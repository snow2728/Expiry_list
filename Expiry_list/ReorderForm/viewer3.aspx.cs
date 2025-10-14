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
using System.Web.Script.Services;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using DocumentFormat.OpenXml.Office.Word;
using Newtonsoft.Json;

namespace Expiry_list.ReorderForm
{
    public partial class viewer3 : System.Web.UI.Page
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
                var permissions = Session["formPermissions"] as Dictionary<string, string>;
                string expiryPerm = permissions != null && permissions.ContainsKey("ReorderQuantity") ? permissions["ReorderQuantity"] : "";

                GridView2.Columns[0].Visible = false;
                GridView2.Columns[GridView2.Columns.Count - 1].Visible = expiryPerm == "admin";

                foreach (DataControlField col in GridView2.Columns)
                {
                    if (col is TemplateField && col.HeaderText == "Delete")
                    {
                        col.Visible = expiryPerm == "admin";
                        break;
                    }
                }

                item.Items.Insert(0, new ListItem("", "")); 
                vendor.Items.Insert(0, new ListItem("", ""));

                Panel1.Visible = true;
                BindGrid();
                BindStores();
                PopulateItemsDropdown();
                PopulateVendorDropdown();
            }
            else
            {
                if (ViewState["SelectedItem"] != null)
                {
                    item.SelectedValue = ViewState["SelectedItem"].ToString();
                }
                if (ViewState["SelectedVendor"] != null)
                {
                    vendor.SelectedValue = ViewState["SelectedVendor"].ToString();
                }
            }
        }

        private void RespondWithData()
        {
            using (SqlConnection conn = new SqlConnection(strcon))
            {
                // DataTables request parameters
                int draw = Convert.ToInt32(Request["draw"]);
                int start = Convert.ToInt32(Request["start"]);
                int length = Convert.ToInt32(Request["length"]);
                int orderColumnIndex = Convert.ToInt32(Request["order[0][column]"]);
                string orderDir = Request["order[0][dir]"] ?? "asc";
                string searchValue = Request["search[value]"] ?? "";

                // Filters
                string item = Request["item"] ?? "";
                string vendor = Request["vendor"] ?? "";
                string action = Request["action"] ?? "";
                string status = Request["status"] ?? "";
                string staff = Request["staff"] ?? "";
                string owner = Request["owner"] ?? "";
                string from = Request["fromDate"] ?? "";
                string to = Request["toDate"] ?? "";
                string storeNO = Request["storeNO"] ?? "";
                string division = Request["divisionCode"] ?? "";

                bool hasFrom = DateTime.TryParse(from, out DateTime fromDate);
                bool hasTo = DateTime.TryParse(to, out DateTime toDate);

                if (length == 0) length = 100;

                string[] columns = {
                    "id", "no", "itemNo", "description", "qty", "uom",
                    "packingInfo", "storeNo", "vendorNo", "vendorName",
                    "divisionCode", "regeDate", "approveDate", "approver",
                    "note", "action", "status", "remark", "completedDate", "owner"
                };

                string orderByClause = "ORDER BY no ASC";
                if (orderColumnIndex >= 0 && orderColumnIndex < columns.Length)
                {
                    string orderColumn = columns[orderColumnIndex];
                    orderByClause = $"ORDER BY [{orderColumn}] {orderDir}";
                }

                var where = new StringBuilder();
                where.Append(" (status IN ('Reorder Done', 'No Reordering')) AND (approved = 'approved')");

                // Permission check
                string username = User.Identity.Name;
                var permissions = GetAllowedFormsByUser(username);
                if (!permissions.TryGetValue("ReorderQuantity", out string perm))
                {
                    Response.Write("{\"error\":\"Unauthorized\"}");
                    Response.End();
                    return;
                }

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
                                CONVERT(varchar(10), approveDate, 103) LIKE @search OR owner LIKE @search
                            )");

                List<string> storeNos = GetLoggedInUserStoreNames();
                string selectedMonth = Request.Form["month"];

                //From–To date filter
                if (hasFrom && hasTo)
                    where.Append(" AND regeDate >= @FromDate AND regeDate < DATEADD(day, 1, @ToDate)");
                else if (hasFrom)
                    where.Append(" AND regeDate >= @FromDate");
                else if (hasTo)
                    where.Append(" AND regeDate < DATEADD(day, 1, @ToDate)");
                else if (!string.IsNullOrEmpty(selectedMonth) &&
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

                // Individual filters
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

                if (!string.IsNullOrEmpty(division))
                    where.Append(" AND divisionCode = @division");

                if (!string.IsNullOrEmpty(owner))
                    where.Append(" AND owner = @owner");


                if (!string.IsNullOrEmpty(storeNO))
                    where.Append(" AND storeNo = @storeNO");

                // Main query
                string query = $@"
                    SELECT * FROM itemListR WHERE
                    {where}
                    {orderByClause}
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                DataTable dt = new DataTable();
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    // Base parameters
                    cmd.Parameters.AddWithValue("@Offset", start);
                    cmd.Parameters.AddWithValue("@PageSize", length);
                    cmd.Parameters.AddWithValue("@search", $"%{searchValue}%");

                    // Individual filters
                    if (!string.IsNullOrEmpty(item)) cmd.Parameters.AddWithValue("@item", item);
                    if (!string.IsNullOrEmpty(vendor)) cmd.Parameters.AddWithValue("@vendor", $"%{vendor}%");
                    if (!string.IsNullOrEmpty(action)) cmd.Parameters.AddWithValue("@action", action);
                    if (!string.IsNullOrEmpty(status)) cmd.Parameters.AddWithValue("@status", status);
                    if (!string.IsNullOrEmpty(staff)) cmd.Parameters.AddWithValue("@staff", $"%{staff}%");
                    if (!string.IsNullOrEmpty(owner)) cmd.Parameters.AddWithValue("@owner", owner);
                    if (!string.IsNullOrEmpty(division)) cmd.Parameters.AddWithValue("@division", division);
                    if (!string.IsNullOrEmpty(storeNO)) cmd.Parameters.AddWithValue("@storeNO", storeNO);

                    // Date parameters
                    if (hasFrom) cmd.Parameters.AddWithValue("@FromDate", fromDate);
                    if (hasTo) cmd.Parameters.AddWithValue("@ToDate", toDate);

                    if (!hasHO && storeNos.Any())
                    {
                        for (int i = 0; i < storeNos.Count; i++)
                            cmd.Parameters.AddWithValue($"@store{i}", storeNos[i]);
                    }

                    // Month filter
                    if (!string.IsNullOrEmpty(selectedMonth) &&
                        DateTime.TryParseExact(selectedMonth, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime monthDate1))
                    {
                        cmd.Parameters.AddWithValue("@Year", monthDate1.Year);
                        cmd.Parameters.AddWithValue("@Month", monthDate1.Month);
                    }

                    conn.Open();
                    new SqlDataAdapter(cmd).Fill(dt);
                }

                // Count query
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
                    if (!string.IsNullOrEmpty(owner)) countCmd.Parameters.AddWithValue("@owner", owner);
                    if (!string.IsNullOrEmpty(division)) countCmd.Parameters.AddWithValue("@division", division);
                    if (!string.IsNullOrEmpty(storeNO)) countCmd.Parameters.AddWithValue("@storeNO", storeNO);

                    if (hasFrom) countCmd.Parameters.AddWithValue("@FromDate", fromDate);
                    if (hasTo) countCmd.Parameters.AddWithValue("@ToDate", toDate);

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

                // Response
                var data = dt.AsEnumerable().Select(row => new
                {
                    id = row["id"],
                    no = row["no"],
                    itemNo = row["itemNo"],
                    description = row["description"],
                    qty = row["qty"],
                    uom = row["uom"],
                    packingInfo = row["packingInfo"],
                    storeNo = row["storeNo"],
                    vendorNo = row["vendorNo"],
                    vendorName = row["vendorName"],
                    divisionCode = row["divisionCode"],
                    regeDate = row["regeDate"],
                    approveDate = row["approveDate"],
                    approver = row["approver"],
                    note = row["note"],
                    action = row["action"],
                    status = row["status"],
                    remark = row["remark"],
                    completedDate = row["completedDate"],
                    owner = row["owner"]
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
                        filters = new { item, vendor, storeNO, action, status, staff, owner, division, from, to }
                    }
                };

                Response.ContentType = "application/json";
                Response.Write(JsonConvert.SerializeObject(response));
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
            Response.Redirect("viewer3.aspx");
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
                bool hasAnyFilter =
                    filterStore.Checked || filterItem.Checked || filterVendor.Checked ||
                    filterStatus.Checked || filterAction.Checked ||
                    filterRegistrationDate.Checked || filterStaff.Checked ||
                    filterOwner.Checked || filterDivisionCode.Checked || filterApproveDate.Checked;

                if (!hasAnyFilter)
                {
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "alertNoFilter",
                        "Swal.fire('Warning!', 'Please select at least one filter to apply and ensure it has a value.', 'warning');", true);
                    return;
                }

                // Store filter state
                ViewState["FilterStoreChecked"] = filterStore.Checked;
                ViewState["SelectedStores"] = string.Join(",",
                    lstStoreFilter.Items.Cast<ListItem>()
                        .Where(li => li.Selected)
                        .Select(li => li.Value));

                ViewState["FilterItemChecked"] = filterItem.Checked;
                ViewState["FilterVendorChecked"] = filterVendor.Checked;

                ViewState["FilterStatusChecked"] = filterStatus.Checked;
                ViewState["SelectedStatus"] = ddlStatusFilter.SelectedValue;

                ViewState["FilterActionChecked"] = filterAction.Checked;
                ViewState["SelectedAction"] = ddlActionFilter.SelectedValue;

                ViewState["FilterRegDateChecked"] = filterRegistrationDate.Checked;
                ViewState["FilterFromDate"] = from1.Text;
                ViewState["FilterToDate"] = to1.Text;

                ViewState["FilterApproveDateChecked"] = filterApproveDate.Checked;
                ViewState["SelectedApproveDate"] = txtApproveDateFilter.Text;

                ViewState["FilterStaffChecked"] = filterStaff.Checked;
                ViewState["SelectedStaff"] = txtstaffFilter.Text;

                ViewState["FilterOwnerChecked"] = filterOwner.Checked;
                ViewState["SelectedOwner"] = txtOwner.Text;

                ViewState["FilterDivisionCodeChecked"] = filterDivisionCode.Checked;
                ViewState["SelectedDivisionCode"] = txtDivisionCode.Text;

                DataTable dt = GetFilteredData();
                ViewState["FilteredData"] = dt;

                GridView2.DataSource = dt;
                GridView2.DataBind();

                ScriptManager.RegisterStartupScript(this, this.GetType(), "reinitSelect2",
                    "setTimeout(function() { InitializeItemVendorFilter(); }, 100);", true);
            }
            catch (Exception ex)
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), "alertError",
                    $"Swal.fire('Error!', '{HttpUtility.JavaScriptStringEncode(ex.Message)}', 'error');", true);
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

                            ListItem listItem = new ListItem
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

                            ListItem listItem = new ListItem
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
            // Reset filter
            ddlActionFilter.SelectedIndex = 0;
            ddlStatusFilter.SelectedIndex = 0;
            item.SelectedIndex = 0;
            txtstaffFilter.Text = string.Empty;
            vendor.SelectedIndex = 0;
            txtDivisionCode.Text = string.Empty;
            txtOwner.Text = string.Empty;
            txtApproveDateFilter.Text = string.Empty;

            ScriptManager.RegisterStartupScript(this, GetType(), "ResetDateFilters",
                @"document.getElementById('from1').value = '';
          document.getElementById('to1').value = '';",
                true);

            // Reset filter checkboxes
            filterAction.Checked = false;
            filterStatus.Checked = false;
            filterItem.Checked = false;
            filterStaff.Checked = false;
            filterVendor.Checked = false;
            filterRegistrationDate.Checked = false;
            filterOwner.Checked = false;
            filterDivisionCode.Checked = false;
            filterApproveDate.Checked = false;

            // Clear ViewState 
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
            ViewState["FilterFromDate"] = null; 
            ViewState["FilterToDate"] = null;   

            ViewState["FilterStaffChecked"] = null;
            ViewState["SelectedStaff"] = null;

            ViewState["FilterOwnerChecked"] = null;
            ViewState["SelectedOwner"] = null;

            ViewState["FilterDivisionCodeChecked"] = null;
            ViewState["SelectedDivisionCode"] = null;

            ViewState["FilterApproveDateChecked"] = null;
            ViewState["SelectedApproveDate"] = null;

            // reload page
            ScriptManager.RegisterStartupScript(this, GetType(), "redirect",
                "window.location.href = 'viewer3.aspx';", true);

            // Optional: refresh filter UI visibility
            ScriptManager.RegisterStartupScript(this, GetType(), "ResetFiltersUI",
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
                    ShowAlert("Unauthorized", "You do not have permission to access Reorder Quantity List", "error");
                    return;
                }

                List<string> storeList = Session["storeListRaw"] as List<string>;
                bool isHOUser = storeList?.Contains("HO") == true;

                StringBuilder queryBuilder = new StringBuilder("SELECT * FROM itemListR WHERE 1=1");
                List<SqlParameter> parameters = new List<SqlParameter>();

                if (perm == "edit")
                {
                    queryBuilder.Append(" AND (status IN ('Reorder Done','No Reordering')) AND (approved = 'approved')");
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

            // Permission check
            if (!permissions.TryGetValue("ReorderQuantity", out string perm))
            {
                ShowAlert("Unauthorized", "You do not have permission to access Reorder Quantity List", "error");
                return new DataTable();
            }

            var userStores = Session["storeListRaw"] as List<string> ?? new List<string>();
            bool hasHO = userStores.Any(s => s.Equals("HO", StringComparison.OrdinalIgnoreCase));

            StringBuilder query = new StringBuilder("SELECT * FROM itemListR WHERE 1=1");
            List<SqlParameter> parameters = new List<SqlParameter>();

            // Permission filter
            if (perm == "edit" || perm == "view" || perm == "super" || perm == "admin")
            {
                query.Append(" AND status IN ('Reorder Done','No Reordering') AND approved = 'approved'");
            }

            // Store filter
            if (ViewState["FilterStoreChecked"] is bool isStoreChecked && isStoreChecked)
            {
                string selectedStoresRaw = ViewState["SelectedStores"] as string ?? "";
                var selectedStores = selectedStoresRaw
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();

                var filteredStores = hasHO ? selectedStores : selectedStores.Intersect(userStores).ToList();

                if (filteredStores.Count > 0)
                {
                    string storeFilterCondition = string.Join(",", filteredStores.Select((s, i) => $"@Store{i}"));
                    query.Append($" AND storeNo IN ({storeFilterCondition})");

                    for (int i = 0; i < filteredStores.Count; i++)
                    {
                        parameters.Add(new SqlParameter($"@Store{i}", filteredStores[i]));
                    }
                }
                else
                {
                    query.Append(" AND 1=0"); // no match
                }
            }
            else if (!hasHO && userStores.Count > 0)
            {
                string storeFilterCondition = string.Join(",", userStores.Select((s, i) => $"@UserStore{i}"));
                query.Append($" AND storeNo IN ({storeFilterCondition})");

                for (int i = 0; i < userStores.Count; i++)
                {
                    parameters.Add(new SqlParameter($"@UserStore{i}", userStores[i]));
                }
            }
            else if (!hasHO)
            {
                query.Append(" AND 1=0");
            }

            // Item filter
            if (filterItem.Checked && !string.IsNullOrEmpty(item.SelectedValue))
            {
                query.Append(" AND itemNo = @ItemNo");
                parameters.Add(new SqlParameter("@ItemNo", item.SelectedValue.Trim()));
            }

            // Vendor filter
            if (filterVendor.Checked && !string.IsNullOrEmpty(vendor.SelectedValue))
            {
                query.Append(" AND vendorNo LIKE @VendorNo");
                parameters.Add(new SqlParameter("@VendorNo", vendor.SelectedValue.Trim() + "%"));
            }

            // Status filter
            if (ViewState["FilterStatusChecked"] is bool isStatusChecked && isStatusChecked)
            {
                if (!string.IsNullOrEmpty(ViewState["SelectedStatus"] as string))
                {
                    query.Append(" AND status = @Status");
                    parameters.Add(new SqlParameter("@Status", ViewState["SelectedStatus"]));
                }
            }

            // Action filter
            if (ViewState["FilterActionChecked"] is bool isActionChecked && isActionChecked)
            {
                if (!string.IsNullOrEmpty(ViewState["SelectedAction"] as string))
                {
                    query.Append(" AND action = @Action");
                    parameters.Add(new SqlParameter("@Action", ViewState["SelectedAction"]));
                }
            }

            // Registration Date filter
            if (ViewState["FilterRegDateChecked"] is bool isRegChecked && isRegChecked)
            {
                string fromStr = ViewState["FilterFromDate"] as string;
                string toStr = ViewState["FilterToDate"] as string;

                bool hasFrom = DateTime.TryParse(fromStr, out DateTime fromDate);
                bool hasTo = DateTime.TryParse(toStr, out DateTime toDate);

                if (hasFrom && hasTo)
                {
                    // Between from and to
                    query.Append(" AND regeDate >= @StartDate AND regeDate < @EndDate");
                    parameters.Add(new SqlParameter("@StartDate", fromDate.Date));
                    parameters.Add(new SqlParameter("@EndDate", toDate.Date.AddDays(1)));
                }
                else if (hasFrom)
                {
                    // From date only → show from this date onwards
                    query.Append(" AND regeDate >= @StartDate");
                    parameters.Add(new SqlParameter("@StartDate", fromDate.Date));
                }
                else if (hasTo)
                {
                    // To date only → show up to this date
                    query.Append(" AND regeDate < @EndDate");
                    parameters.Add(new SqlParameter("@EndDate", toDate.Date.AddDays(1)));
                }
            }

            // Approved Date filter
            if (ViewState["FilterApproveDateChecked"] is bool isAppDateChecked && isAppDateChecked)
            {
                string appDateStr = ViewState["SelectedApproveDate"] as string;
                if (DateTime.TryParseExact(appDateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime approveDate))
                {
                    query.Append(" AND approveDate >= @appDate AND approveDate < @NextDay");
                    parameters.Add(new SqlParameter("@appDate", approveDate));
                    parameters.Add(new SqlParameter("@NextDay", approveDate.AddDays(1)));
                }
            }

            // Staff filter
            if (ViewState["FilterStaffChecked"] is bool isStaffChecked && isStaffChecked)
            {
                if (!string.IsNullOrWhiteSpace(ViewState["SelectedStaff"] as string))
                {
                    query.Append(" AND staffName = @StaffNo");
                    parameters.Add(new SqlParameter("@StaffNo", ViewState["SelectedStaff"]));
                }
            }

            // Owner filter
            if (ViewState["FilterOwnerChecked"] is bool isOwnerChecked && isOwnerChecked)
            {
                if (!string.IsNullOrWhiteSpace(ViewState["SelectedOwner"] as string))
                {
                    query.Append(" AND owner = @Owner");
                    parameters.Add(new SqlParameter("@Owner", ViewState["SelectedOwner"]));
                }
            }

            // Division Code filter
            if (ViewState["FilterDivisionCodeChecked"] is bool isDivChecked && isDivChecked)
            {
                if (!string.IsNullOrWhiteSpace(ViewState["SelectedDivisionCode"] as string))
                {
                    query.Append(" AND divisionCode = @Division");
                    parameters.Add(new SqlParameter("@Division", ViewState["SelectedDivisionCode"].ToString().Trim()));
                }
            }

            Debug.WriteLine("Final query: " + query);

            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(strcon))
            using (SqlCommand cmd = new SqlCommand(query.ToString(), conn))
            {
                cmd.Parameters.AddRange(parameters.ToArray());
                conn.Open();
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
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

        private void ShowAlert(string title, string message, string type)
        {
            ScriptManager.RegisterStartupScript(this, GetType(), "alert",
                $"swal('{title}', '{HttpUtility.JavaScriptStringEncode(message)}', '{type}');", true);
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

        // delete
        protected void GridView2_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            try
            {
                string id = GridView2.DataKeys[e.RowIndex].Value.ToString();

                if (DeleteRecord(id))
                {
                    ScriptManager.RegisterStartupScript(this, GetType(), "DeleteSuccess",
                        "Swal.fire('Success!', 'Record deleted successfully.', 'success');", true);

                    BindGrid();
                }
                else
                {
                    ScriptManager.RegisterStartupScript(this, GetType(), "DeleteError",
                        "Swal.fire('Error!', 'Failed to delete record.', 'error');", true);
                }
            }
            catch (Exception ex)
            {
                ScriptManager.RegisterStartupScript(this, GetType(), "DeleteException",
                    $"Swal.fire('Error!', 'An error occurred: {ex.Message}', 'error');", true);
            }
        }

        [WebMethod]
        public static string DeleteRecord1(string id)
        {
            try
            {
                string strcon = ConfigurationManager.ConnectionStrings["con"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(strcon))
                {
                    string query = "DELETE FROM itemListR WHERE id = @id";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        conn.Open();
                        int result = cmd.ExecuteNonQuery();

                        if (result > 0)
                            return "success";
                        else
                            return "notfound";
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                return "sqlerror:" + sqlEx.Number;
            }
            catch (Exception ex)
            {
                return "error:" + ex.Message;
            }
        }

        private bool DeleteRecord(string id)
        {
            using (SqlConnection conn = new SqlConnection(strcon))
            {
                string query = "DELETE FROM itemListR WHERE id = @id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    conn.Open();
                    int result = cmd.ExecuteNonQuery();
                    return result > 0;
                }
            }
        }

        private void BindStores()
        {
            try
            {
                lstStoreFilter.Items.Clear();
                lstStoreFilter.Items.Add(new ListItem("All Stores", "all"));

                var uniqueStores = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                using (SqlConnection con = new SqlConnection(strcon))
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand(@"SELECT DISTINCT LTRIM(RTRIM(storeNo)) AS storeNo 
                                                   FROM stores 
                                                   WHERE storeNo IS NOT NULL 
                                                   AND storeNo <> '' AND storeNo <> 'HO'", con))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string storeNo = reader["storeNo"].ToString().Trim();
                            if (!string.IsNullOrEmpty(storeNo) && uniqueStores.Add(storeNo))
                            {
                                lstStoreFilter.Items.Add(new ListItem(storeNo, storeNo));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ScriptManager.RegisterStartupScript(this, GetType(), "alert",
                    $"alert('Error loading stores: {ex.Message.Replace("'", "''")}');", true);
            }
        }
    }
}