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
                PopulateItemsDropdown();
                PopulateVendorDropdown();
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

                string username = User.Identity.Name;
                var permissions = GetAllowedFormsByUser(username);

                if (!permissions.TryGetValue("ReorderQuantity", out string perm))
                {
                    Response.Write("{\"error\":\"Unauthorized\"}");
                    Response.End();
                    return;
                }

                List<string> storeNos = GetLoggedInUserStoreNames();
                string selectedMonth = Request.Form["month"];
                string searchValue = Request["search"] ?? "";

                StringBuilder whereClause = new StringBuilder("1=1");
                List<SqlParameter> parameters = new List<SqlParameter>();

                whereClause.Append(" AND (status NOT IN ('Reorder Added', 'No Reorder Added') OR status IS NULL) AND (approved = 'approved')");

                // Month filter
                if (!string.IsNullOrEmpty(selectedMonth) &&
                    DateTime.TryParseExact(selectedMonth, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime monthDate))
                {
                    whereClause.Append(" AND YEAR(regeDate) = @Year AND MONTH(regeDate) = @Month");
                    parameters.Add(new SqlParameter("@Year", monthDate.Year));
                    parameters.Add(new SqlParameter("@Month", monthDate.Month));
                }

                bool hasHO = storeNos.Any(s => s.Equals("HO", StringComparison.OrdinalIgnoreCase));

                // Store filter
                if ((perm == "edit" || perm == "super" || perm == "view") && !hasHO && storeNos.Count > 0)
                {
                    whereClause.Append($" AND storeNo IN ({string.Join(",", storeNos.Select((s, i) => $"@store{i}"))})");
                    for (int i = 0; i < storeNos.Count; i++)
                    {
                        parameters.Add(new SqlParameter($"@store{i}", storeNos[i]));
                    }
                }

                // Search
                if (!string.IsNullOrEmpty(searchValue))
                {
                    whereClause.Append(" AND (");
                    string[] searchableColumns = {
                        "no", "itemNo", "description","qty", "uom", "packingInfo","storeNo",
                        "approver","vendorNo", "vendorName", "regeDate", "action", "status","note", "remark", "completedDate"
                    };

                    for (int i = 0; i < searchableColumns.Length; i++)
                    {
                        whereClause.Append($"{searchableColumns[i]} LIKE @SearchValue");
                        if (i < searchableColumns.Length - 1)
                            whereClause.Append(" OR ");
                    }

                    whereClause.Append(")");
                    parameters.Add(new SqlParameter("@SearchValue", $"%{searchValue}%"));
                }

                string[] columns = {
                    "id", "no", "itemNo", "description", "qty", "uom",
                    "packingInfo","storeNo", "approver",
                    "vendorNo", "vendorName", "regeDate", "action", "status", "note",
                    "remark", "completedDate"
                };
                string orderBy = columns.ElementAtOrDefault(orderColumnIndex) ?? "id";

                string dataQuery = $@"
                    SELECT * 
                    FROM itemListR 
                    WHERE {whereClause}
                    ORDER BY {orderBy} {orderDir}
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                string countQuery = $"SELECT COUNT(*) FROM itemListR WHERE {whereClause}";

                SqlCommand dataCmd = new SqlCommand(dataQuery, conn);
                SqlCommand countCmd = new SqlCommand(countQuery, conn);

                parameters.ForEach(p =>
                {
                    dataCmd.Parameters.AddWithValue(p.ParameterName, p.Value);
                    countCmd.Parameters.AddWithValue(p.ParameterName, p.Value);
                });

                dataCmd.Parameters.AddWithValue("@Offset", start);
                dataCmd.Parameters.AddWithValue("@PageSize", length);

                conn.Open();
                DataTable dt = new DataTable();
                new SqlDataAdapter(dataCmd).Fill(dt);

                int totalRecords = (int)countCmd.ExecuteScalar();
                conn.Close();

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
                    regeDate = row["regeDate"],
                    approver = row["approver"],
                    note = row["note"],
                    action = row["action"],
                    status = row["status"],
                    remark = row["remark"],
                    completedDate = row["completedDate"]
                });

                var response = new
                {
                    draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = totalRecords,
                    data,
                    orderColumn = orderColumnIndex,
                    orderDir
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
                    return "Reorder Added";
                case "2":
                    return "No Reorder Added";
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
                           filterRegistrationDate.Checked || filterStaff.Checked;

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

                // Get filtered data
                DataTable dt = GetFilteredData();

                // Bind to GridView
                GridView2.DataSource = dt;
                GridView2.DataBind();

            }
            catch (Exception ex)
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), "alertMultiple", "swal('Warning!', 'Please select only one filter type to apply filter.', 'warning');", true);
                //return;
                //Debug.WriteLine("Filter error: " + ex.ToString());
                //ScriptManager.RegisterStartupScript(this, GetType(), "Error",
                //    $"alert('Error applying filters: {ex.Message.Replace("'", "\\'")}');", true);
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
            // Reset all filter controls
            ddlActionFilter.SelectedIndex = 0;
            ddlStatusFilter.SelectedIndex = 0;
            item.SelectedIndex = 0;
            txtstaffFilter.Text = string.Empty;
            vendor.SelectedIndex = 0;
            txtRegDateFilter.Text = string.Empty;

            filterAction.Checked = false;
            filterStatus.Checked = false;
            //filterStore.Checked = false;
            filterItem.Checked = false;
            filterStaff.Checked = false;
            filterVendor.Checked = false;
            filterRegistrationDate.Checked = false;

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
                    queryBuilder.Append(" AND (status NOT IN ('Reorder Added','No Reorder Added')) AND (approved = 'approved')");
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
                WHERE (status NOT IN ('Reorder Added', 'No Reorder Added') OR status IS NULL) and (approved = 'approved')");

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
                // No assigned stores and not HO → show no data
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
    }
}