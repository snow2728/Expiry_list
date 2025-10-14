using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using DocumentFormat.OpenXml.Office.Word;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Newtonsoft.Json;

namespace Expiry_list.ReorderForm
{
    public partial class viewer2 : System.Web.UI.Page
    {
        string strcon = ConfigurationManager.ConnectionStrings["con"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Request.HttpMethod == "POST" && !string.IsNullOrEmpty(Request["draw"]))
            {
                RespondWithData();
                //PopulateItemsDropdown();
                //PopulateVendorDropdown();
                return;
            }

            if (!IsPostBack)
            {
                Panel1.Visible = true;
                BindGrid();
                PopulateItemsDropdown();
                PopulateVendorDropdown();
            }
            else
            {
                // Restore ViewState for filter checkboxes
                if (ViewState["FilterItemChecked"] != null)
                {
                    filterItem.Checked = (bool)ViewState["FilterItemChecked"];
                }

                if (ViewState["SelectedItem"] != null)
                {
                    item.SelectedValue = ViewState["SelectedItem"].ToString();
                }
            }
        }

        private void RespondWithData()
        {
            using (SqlConnection conn = new SqlConnection(strcon))
            {
                int draw = Convert.ToInt32(Request["draw"]);
                int start = Convert.ToInt32(Request["start"]);
                int length = Convert.ToInt32(Request["length"]);
                int orderColumnIndex = Convert.ToInt32(Request["order[0][column]"]);
                string orderDir = Request["order[0][dir]"] ?? "asc";
                string searchValue = Request["search[value]"] ?? "";

                string item = Request["item"] ?? "";
                string vendor = Request["vendor"] ?? "";
                string action = Request["action"] ?? "";
                string status = Request["status"] ?? "";
                string staff = Request["staff"] ?? "";
                string regDate = Request["regDate"] ?? "";


                if (length == 0) length = 100;

                string[] columns = {
                        "id", "no", "itemNo", "description", "qty", "uom",
                    "packingInfo", "divisionCode", "storeNo", "vendorNo", "vendorName", "regeDate", "approveDate", "approver", "note", "action", "status", "remark", "completedDate"
                };

                string orderByClause = "ORDER BY no ASC";
                if (orderColumnIndex >= 0 && orderColumnIndex < columns.Length)
                {
                    string orderColumn = columns[orderColumnIndex];
                    orderByClause = $"ORDER BY [{orderColumn}] {orderDir}";
                }

                string username = User.Identity.Name;
                var permissions = GetAllowedFormsByUser(username);

                if (!permissions.TryGetValue("ReorderQuantity", out string perm))
                {
                    Response.Write("{\"error\":\"Unauthorized\"}");
                    Response.End();
                    return;
                }

                var where = new StringBuilder();
                where.Append("(status NOT IN ('Reorder Done', 'No Reordering') OR status IS NULL) AND approved = 'approved'");

                if (!string.IsNullOrEmpty(searchValue))
                    where.Append(@"
                            AND (
                                no LIKE @search OR 
                                storeNo LIKE @search OR 
                                divisionCode LIKE @search OR 
                                itemNo LIKE @search OR 
                                description LIKE @search OR 
                                packingInfo LIKE @search OR 
                                qty LIKE @search OR 
                                uom LIKE @search OR 
                                action LIKE @search OR 
                                status LIKE @search OR 
                                remark LIKE @search OR 
                                approver LIKE @search OR 
                                vendorNo LIKE @search OR 
                                vendorName LIKE @search OR 
                                CONVERT(varchar(10), regeDate, 103) LIKE @search OR 
                                CONVERT(varchar(10), approveDate, 103) LIKE @search
                            )");

                List<string> storeNos = GetLoggedInUserStoreNames();
                string selectedMonth = Request.Form["month"];

                // Month filter
                if (!string.IsNullOrEmpty(selectedMonth) &&
                    DateTime.TryParseExact(selectedMonth, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime monthDate))
                {
                    where.Append(" AND YEAR(regeDate) = @Year AND MONTH(regeDate) = @Month");
                }

                storeNos = storeNos.Select(s => s.Trim().ToUpper()).ToList();

                bool hasHO = storeNos.Contains("HO");
                if (!hasHO && storeNos.Any())
                {
                    var storeParams = storeNos.Select((s, i) => $"@store{i}").ToArray();
                    where.Append($" AND UPPER(storeNo) IN ({string.Join(",", storeParams)})");
                }


                if (!string.IsNullOrEmpty(item))
                    where.Append(" AND itemNo = @item");

                if (!string.IsNullOrEmpty(vendor))
                    where.Append(" AND vendorNo LIKE @vendor");

                if (!string.IsNullOrEmpty(action) && action != "0")
                    where.Append(" AND action = @action");

                if (!string.IsNullOrEmpty(status) && status != "0")
                    where.Append(" AND status = @status");

                if (!string.IsNullOrEmpty(staff))
                    where.Append(" AND approver LIKE @staff");

                if (!string.IsNullOrEmpty(regDate))
                    where.Append(" AND CAST(regeDate AS DATE) = @regDate");


                //Main query
                string query = $@"
                        SELECT * FROM itemListR
                        WHERE {where}
                        {orderByClause}
                        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                //Execute Query
                DataTable dt = new DataTable();
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Offset", start);
                    cmd.Parameters.AddWithValue("@PageSize", length);
                    cmd.Parameters.AddWithValue("@search", $"%{searchValue}%");

                    if (!string.IsNullOrEmpty(item)) cmd.Parameters.AddWithValue("@item", item);
                    if (!string.IsNullOrEmpty(vendor)) cmd.Parameters.AddWithValue("@vendor", $"%{vendor}%");
                    if (!string.IsNullOrEmpty(action)) cmd.Parameters.AddWithValue("@action", action);
                    if (!string.IsNullOrEmpty(status)) cmd.Parameters.AddWithValue("@status", status);
                    if (!string.IsNullOrEmpty(staff)) cmd.Parameters.AddWithValue("@staff", $"%{staff}%");
                    if (!string.IsNullOrEmpty(regDate)) cmd.Parameters.AddWithValue("@regDate", regDate);

                    if (!hasHO && storeNos.Any())
                    {
                        for (int i = 0; i < storeNos.Count; i++)
                            cmd.Parameters.AddWithValue($"@store{i}", storeNos[i]);
                    }

                    if (!string.IsNullOrEmpty(selectedMonth) &&
                    DateTime.TryParseExact(selectedMonth, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime monthDate1))
                    {
                        cmd.Parameters.AddWithValue("@Year", monthDate1.Year);
                        cmd.Parameters.AddWithValue("@Month", monthDate1.Month);
                    }

                    conn.Open();
                    new SqlDataAdapter(cmd).Fill(dt);
                }

                //Count query
                string countQuery = $"SELECT COUNT(*) FROM itemListR WHERE {where}";
                int totalRecords = 0;
                using (SqlCommand countCmd = new SqlCommand(countQuery, conn))
                {
                    countCmd.Parameters.AddWithValue("@search", $"%{searchValue}%");
                    if (!string.IsNullOrEmpty(item)) countCmd.Parameters.AddWithValue("@item", item);
                    if (!string.IsNullOrEmpty(vendor)) countCmd.Parameters.AddWithValue("@vendor", $"%{vendor}%");
                    if (!string.IsNullOrEmpty(action)) countCmd.Parameters.AddWithValue("@action", action);
                    if (!string.IsNullOrEmpty(status)) countCmd.Parameters.AddWithValue("@status", status);
                    if (!string.IsNullOrEmpty(staff)) countCmd.Parameters.AddWithValue("@staff", $"%{staff}%");
                    if (!string.IsNullOrEmpty(regDate)) countCmd.Parameters.AddWithValue("@regDate", regDate);

                    if (!hasHO && storeNos.Any())
                    {
                        for (int i = 0; i < storeNos.Count; i++)
                            countCmd.Parameters.AddWithValue($"@store{i}", storeNos[i]);
                    }

                    if (!string.IsNullOrEmpty(selectedMonth) &&
                   DateTime.TryParseExact(selectedMonth, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime monthDate1))
                    {
                        countCmd.Parameters.AddWithValue("@Year", monthDate1.Year);
                        countCmd.Parameters.AddWithValue("@Month", monthDate1.Month);
                    }

                    if (conn.State != ConnectionState.Open) conn.Open();
                    totalRecords = (int)countCmd.ExecuteScalar();
                }

                var data = dt.AsEnumerable().Select(row => new
                {
                    id = row["id"],
                    no = row["no"],
                    itemNo = row["itemNo"],
                    description = row["description"],
                    qty = row["qty"],
                    uom = row["uom"],
                    packingInfo = row["packingInfo"],
                    divisionCode = row["divisionCode"],
                    storeNo = row["storeNo"],
                    vendorNo = row["vendorNo"],
                    vendorName = row["vendorName"],
                    regeDate = row["regeDate"],
                    approveDate = row["approveDate"],
                    approver = row["approver"],
                    note = row["note"],
                    action = row["action"],
                    status = row["status"],
                    remark = row["remark"],
                    completedDate = row["completedDate"]
                });

                var response = new
                {
                    draw = draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = totalRecords,
                    data = data,
                    debug = new
                    {
                        query,
                        filters = new { item, vendor, action, status, staff, regDate }
                    }
                };

                string jsonResponse = JsonConvert.SerializeObject(response);
                Response.ContentType = "application/json";
                Response.Write(jsonResponse);
                Response.End();
            }
        }

        protected void GridView2_RowEditing(object sender, GridViewEditEventArgs e)
        {
            GridView2.EditIndex = e.NewEditIndex;
            BindGrid();
        }

        protected void GridView2_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            GridView2.EditIndex = -1;
            BindGrid();
            Response.Redirect("viewer2.aspx");
        }

        protected string GetActionText(string action)
        {
            switch (action)
            {
                case "0":
                    return "";
                case "1":
                    return "None";
                case "2":
                    return "Overstock and Redistribute";
                case "3":
                    return "Redistribute";
                case "4":
                    return "Allocation Item";
                case "5":
                    return "Shortage Item";
                case "6":
                    return "System Enough";
                case "7":
                    return "Tail Off Item";
                case "8":
                    return "Purchase Blocked";
                case "9":
                    return "Already Added Reorder";
                case "10":
                    return "Customer Requested Item";
                case "11":
                    return "No Hierarchy";
                case "12":
                    return "Near Expiry Item";
                case "13":
                    return "Reorder Qty is large, Need to adjust Qty";
                case "14":
                    return "Discon Item";
                case "15":
                    return "Supplier Discon";
                default:
                    return "";
            }
            
        }

        protected string GetStatusText(string selectedAction)
        {
            switch (selectedAction)
            {
                case "0":
                    return "";
                case "1":
                    return "Reorder Done";
                case "2":
                    return "No Reordering";
                default:
                    return "";
            }
        }

        protected void ApplyFilters_Click(object sender, EventArgs e)
        {
            try
            {
                bool hasAnyFilter = filterItem.Checked || filterVendor.Checked ||
                           filterStatus.Checked || filterAction.Checked ||
                           filterRegistrationDate.Checked || filterStaff.Checked || filterApproveDate.Checked;

                if (!hasAnyFilter)
                {
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "alertNoFilter",
                        "Swal.fire('Warning!', 'Please select at least one filter to apply and ensure it has a value.', 'warning');", true);
                    return;
                }

                ViewState["FilterItemChecked"] = filterItem.Checked;
                ViewState["SelectedItem"] = item.SelectedValue;

                ViewState["FilterVendorChecked"] = filterVendor.Checked;
                ViewState["SelectedVendor"] = vendor.SelectedValue;

                ViewState["FilterStatusChecked"] = filterStatus.Checked;
                ViewState["SelectedStatus"] = ddlStatusFilter.SelectedValue;

                ViewState["FilterActionChecked"] = filterAction.Checked;
                ViewState["SelectedAction"] = ddlActionFilter.SelectedValue;

                ViewState["FilterRegDateChecked"] = filterRegistrationDate.Checked;
                ViewState["SelectedRegDate"] = txtRegDateFilter.Text;

                ViewState["FilterStaffChecked"] = filterStaff.Checked;
                ViewState["SelectedStaff"] = txtstaffFilter.Text;

                ViewState["FilterApproveDateChecked"] = filterApproveDate.Checked;
                ViewState["SelectedApproveDate"] = txtApproveDateFilter.Text;

                // Get filtered data
                DataTable dt = GetFilteredData();

                // Bind to GridView
                GridView2.DataSource = dt;
                GridView2.DataBind();

            }
            catch (Exception ex)
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), "alertMultiple", "swal('Warning!', 'Please select only one filter type to apply filter.', 'warning');", true);
            }
        }

        protected void GridView1_Sorting(object sender, GridViewSortEventArgs e)
        {
            string sortExpression = e.SortExpression;
            string sortDirection = GetSortDirection(sortExpression);

            ViewState["SortExpression"] = sortExpression;
            ViewState["SortDirection"] = sortDirection;

            ApplyFilters_Click(sender, e);
        }

        protected void GridView1_RowCreated(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.Header)
            {
                if (ViewState["SortExpression"] != null && ViewState["SortDirection"] != null)
                {
                    string sortExpression = ViewState["SortExpression"].ToString();
                    string sortDirection = ViewState["SortDirection"].ToString();

                    for (int i = 0; i < GridView2.Columns.Count; i++)
                    {
                        if (GridView2.Columns[i].SortExpression == sortExpression)
                        {
                            LinkButton lb = (LinkButton)e.Row.Cells[i].Controls[0];
                            string iconHtml = sortDirection == "ASC" ? " ▲" : " ▼";
                            lb.Text += iconHtml;

                        }
                    }
                }
            }
        }

        protected void btnFilter_Click1(object sender, EventArgs e)
        {
            string script = @"toggleFilter();";
            ScriptManager.RegisterStartupScript(this, GetType(), "toggleFilter", script, true);
        }

        private string GetSortDirection(string sortExpression)
        {
            if (ViewState["SortExpression"] != null && ViewState["SortExpression"].ToString() == sortExpression)
            {
                if (ViewState["SortDirection"] != null && ViewState["SortDirection"].ToString() == "ASC")
                {
                    return "DESC";
                }
                else
                {
                    return "ASC";
                }
            }
            else
            {
                return "ASC";
            }
        }

        private void PopulateItemsDropdown()
        {
            string query = "SELECT ItemNo, Description FROM items";
            using (SqlConnection con = new SqlConnection(strcon))
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    try
                    {
                        con.Open();
                        SqlDataReader reader = cmd.ExecuteReader();

                        while (reader.Read())
                        {
                            string itemNo = reader["ItemNo"].ToString();
                            string description = reader["Description"].ToString();

                            System.Web.UI.WebControls.ListItem listItem = new System.Web.UI.WebControls.ListItem
                            {
                                Text = itemNo + " - " + description,
                                Value = itemNo
                            };
                            item.Items.Add(listItem);
                        }

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: " + ex.Message);
                    }
                    finally
                    {
                        con.Close();
                    }
                }
            }
        }

        private void PopulateVendorDropdown()
        {
            string query = "SELECT VendorNo, VendorName FROM vendors";
            using (SqlConnection con = new SqlConnection(strcon))
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    try
                    {
                        con.Open();
                        SqlDataReader reader = cmd.ExecuteReader();

                        while (reader.Read())
                        {
                            string vendorNo = reader["VendorNo"].ToString();
                            string vendorName = reader["VendorName"].ToString();

                            System.Web.UI.WebControls.ListItem listItem = new System.Web.UI.WebControls.ListItem
                            {
                                Text = vendorNo + " - " + vendorName,
                                Value = vendorNo
                            };
                            vendor.Items.Add(listItem);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: " + ex.Message);
                    }
                    finally
                    {
                        con.Close();
                    }
                }
            }
        }

        protected void GridView1_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            GridView2.PageIndex = e.NewPageIndex;
            BindGrid();
        }

        protected void ResetFilters_Click(object sender, EventArgs e)
        {
            // Reset all filter controls
            ddlActionFilter.SelectedIndex = 0;
            ddlStatusFilter.SelectedIndex = 0;
            item.SelectedIndex = 0;
            txtstaffFilter.Text = string.Empty;
            vendor.SelectedIndex = 0;
            txtRegDateFilter.Text = string.Empty;
            txtApproveDateFilter.Text = string.Empty;

            filterAction.Checked = false;
            filterStatus.Checked = false;
            //filterStore.Checked = false;
            filterItem.Checked = false;
            filterStaff.Checked = false;
            filterVendor.Checked = false;
            filterRegistrationDate.Checked = false;
            filterApproveDate.Checked = false;

            // Clear ViewState filters too
            ViewState["FilterStoreChecked"] = null;
            ViewState["SelectedStores"] = null;
            ViewState["FilterItemChecked"] = null;
            ViewState["SelectedItem"] = null;
            ViewState["FilterVendorChecked"] = null;
            ViewState["SelectedVendor"] = null;
            ViewState["FilterStatusChecked"] = null;
            ViewState["SelectedStatus"] = null;
            ViewState["FilterActionChecked"] = null;
            ViewState["SelectedAction"] = null;
            ViewState["FilterRegDateChecked"] = null;
            ViewState["SelectedRegDate"] = null;
            ViewState["FilterStaffChecked"] = null;
            ViewState["SelectedStaff"] = null;
            ViewState["FilterSApproveDateChecked"] = null;
            ViewState["SelectedApproveDate"] = null;

            ScriptManager.RegisterStartupScript(this, GetType(), "redirect",
                "window.location.href = 'viewer2.aspx';", true);

            // Optional (if you want to refresh filter UI visibility)
            ScriptManager.RegisterStartupScript(this, GetType(), "ResetFilters",
                @"if (typeof(updateFilterVisibility) === 'function') { 
                    updateFilterVisibility(); 
                    toggleFilter(false); 
                }", true);
        }

        private List<string> GetLoggedInUserStoreNames()
        {
            List<string> storeNos = Session["storeListRaw"] as List<string>;
            List<string> storeNames = new List<string>();

            if (storeNos == null || storeNos.Count == 0)
                return storeNames;

            string query = $"SELECT storeNo FROM Stores WHERE storeNo IN ({string.Join(",", storeNos.Select((s, i) => $"@store{i}"))})";

            using (SqlConnection conn = new SqlConnection(strcon))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                for (int i = 0; i < storeNos.Count; i++)
                {
                    cmd.Parameters.AddWithValue($"@store{i}", storeNos[i]);
                }

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        storeNames.Add(reader["storeNo"].ToString());
                    }
                }
            }

            return storeNames;
        }

        private void BindGrid(int pageNumber = 1, int pageSize = 100)
        {
            using (SqlConnection conn = new SqlConnection(strcon))
            {
                string orderBy = string.IsNullOrEmpty(hfSelectedIDs.Value)
                    ? "ORDER BY id"
                    : $"ORDER BY CASE WHEN id = '{hfSelectedIDs.Value}' THEN 0 ELSE 1 END, id";

                string username = User.Identity.Name;
                var permissions = GetAllowedFormsByUser(username);
                Session["formPermissions"] = permissions;
                Session["activeModule"] = "ReorderQuantity";

                if (!permissions.TryGetValue("ReorderQuantity", out string perm))
                {
                    ShowAlert("Unauthorized", "You do not have permission to access Reorder", "error");
                    return;
                }

                List<string> storeList = Session["storeListRaw"] as List<string>;
                bool isHOUser = storeList?.Contains("HO") == true;

                StringBuilder queryBuilder = new StringBuilder("SELECT * FROM itemListR WHERE 1=1");
                List<SqlParameter> parameters = new List<SqlParameter>();

                if (perm == "edit" || perm == "view" || perm == "super" || perm == "admin")
                {
                    queryBuilder.Append(" AND (status NOT IN ('Reorder Done','No Reordering')) AND (approved = 'approved')");
                    if (!isHOUser)
                    {
                        queryBuilder.Append(" AND storeNo IN (" + string.Join(",", storeList.Select((s, i) => $"@store{i}")) + ")");
                        for (int i = 0; i < storeList.Count; i++)
                        {
                            parameters.Add(new SqlParameter($"@store{i}", storeList[i]));
                        }
                    }
                }


                queryBuilder.Append($" {orderBy} OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY");
                parameters.Add(new SqlParameter("@Offset", (pageNumber - 1) * pageSize));
                parameters.Add(new SqlParameter("@PageSize", pageSize));

                SqlCommand cmd = new SqlCommand(queryBuilder.ToString(), conn);
                cmd.Parameters.AddRange(parameters.ToArray());

                DataTable dt = new DataTable();
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    conn.Open();
                    da.Fill(dt);
                }

                GridView2.DataSource = dt;
                GridView2.PageIndex = 0;
                GridView2.DataBind();
            }
        }

        private DataTable GetFilteredData()
        {
            string username = User.Identity.Name;
            var permissions = GetAllowedFormsByUser(username);
            Session["formPermissions"] = permissions;
            Session["activeModule"] = "ReorderQuantity";

            if (!permissions.TryGetValue("ReorderQuantity", out string perm))
            {
                ShowAlert("Unauthorized", "You do not have permission to access Reorder Quantity", "error");
                return new DataTable();
            }

            var userStores = Session["storeListRaw"] as List<string> ?? new List<string>();
            bool hasHO = userStores.Any(s => s.Equals("HO", StringComparison.OrdinalIgnoreCase));

            StringBuilder query = new StringBuilder(@"
                SELECT * FROM itemListR
                WHERE (status NOT IN ('Reorder Done', 'No Reordering') OR status IS NULL) and (approved = 'approved')");

            List<SqlParameter> parameters = new List<SqlParameter>();

            // Backend filtering by user's store list if not HO
            if (!hasHO && userStores.Count > 0)
            {
                string storeFilter = string.Join(",", userStores.Select((s, i) => $"@Store{i}"));
                query.Append($" AND storeNo IN ({storeFilter})");
                for (int i = 0; i < userStores.Count; i++)
                {
                    parameters.Add(new SqlParameter($"@Store{i}", userStores[i]));
                }
            }
            else if (!hasHO && userStores.Count == 0)
            {
                query.Append(" AND 1 = 0");
            }

            // ITEM filter
            if (filterItem.Checked && !string.IsNullOrEmpty(item.SelectedValue))
            {
                query.Append(" AND itemNo = @ItemNo");
                parameters.Add(new SqlParameter("@ItemNo", item.SelectedValue));
            }

            // VENDOR filter
            if (filterVendor.Checked && !string.IsNullOrEmpty(vendor.SelectedValue))
            {
                query.Append(" AND vendorNo = @VendorNo");
                parameters.Add(new SqlParameter("@VendorNo", vendor.SelectedValue));
            }

            // STATUS filter
            if (filterStatus.Checked && !string.IsNullOrEmpty(ddlStatusFilter.SelectedItem.Text))
            {
                query.Append(" AND status = @Status");
                parameters.Add(new SqlParameter("@Status", ddlStatusFilter.SelectedItem.Text));
            }

            // ACTION filter
            if (filterAction.Checked && !string.IsNullOrEmpty(ddlActionFilter.SelectedItem.Text))
            {
                query.Append(" AND action = @Action");
                parameters.Add(new SqlParameter("@Action", ddlActionFilter.SelectedItem.Text));
            }

            // REGISTRATION DATE filter
            if (filterRegistrationDate.Checked && DateTime.TryParseExact(
                txtRegDateFilter.Text,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime regDate))
            {
                DateTime nextDay = regDate.AddDays(1);
                query.Append(" AND regeDate >= @RegDate AND regeDate < @NextDay");
                parameters.Add(new SqlParameter("@RegDate", regDate));
                parameters.Add(new SqlParameter("@NextDay", nextDay));
            }

            // Approved DATE filter
            if (filterApproveDate.Checked && DateTime.TryParseExact(
                txtApproveDateFilter.Text,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime approveDate))
            {
                DateTime nextDay = approveDate.AddDays(1);
                query.Append(" AND approveDate >= @AppDate AND approveDate < @NextDay");
                parameters.Add(new SqlParameter("@AppDate", approveDate));
                parameters.Add(new SqlParameter("@NextDay", nextDay));
            }

            // STAFF filter
            if (filterStaff.Checked && !string.IsNullOrWhiteSpace(txtstaffFilter.Text))
            {
                query.Append(" AND approver=@StaffNo");
                parameters.Add(new SqlParameter("@StaffNo", txtstaffFilter.Text.Trim()));
            }

            // Log the final query
            Debug.WriteLine("Final Query: " + query.ToString());

            // Execute query
            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(strcon))
            using (SqlCommand cmd = new SqlCommand(query.ToString(), conn))
            {
                cmd.Parameters.AddRange(parameters.ToArray());
                try
                {
                    conn.Open();
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Database error: " + ex);
                    throw;
                }
            }

            return dt;
        }

        private Dictionary<string, string> GetAllowedFormsByUser(string username)
        {
            Dictionary<string, string> forms = new Dictionary<string, string>();

            using (SqlConnection conn = new SqlConnection(strcon))
            {
                string query = @"
            SELECT f.name AS FormName,
                   CASE up.permission_level
                        WHEN 1 THEN 'view'
                        WHEN 2 THEN 'edit'
                        WHEN 3 THEN 'admin'
                        WHEN 4 THEN 'super'
                        WHEN 5 THEN 'super1'
                        ELSE 'none'
                   END AS Permission
                    FROM UserPermissions up
                    INNER JOIN Forms f ON up.form_id = f.id
                    INNER JOIN Users u ON up.user_id = u.id
                    WHERE u.username = @Username";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Username", username);
                conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string form = reader["FormName"].ToString();
                        string permission = reader["Permission"].ToString();
                        forms[form] = permission;
                    }
                }
            }

            return forms;
        }

        protected string TruncateWords(string text, int maxWords)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            string[] words = text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length <= maxWords)
                return text;

            return string.Join(" ", words.Take(maxWords)) + " ...";
        }

        private void ShowAlert(string title, string message, string type)
        {
            ScriptManager.RegisterStartupScript(this, GetType(), "alert",
                $"swal('{title}', '{HttpUtility.JavaScriptStringEncode(message)}', '{type}');", true);
        }
    }
}