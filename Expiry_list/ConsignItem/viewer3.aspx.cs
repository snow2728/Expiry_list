using Newtonsoft.Json;
using OfficeOpenXml.Table;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Globalization;
using System.Diagnostics;
using System.Web.Services;

namespace Expiry_list.ConsignItem
{
    public partial class viewer3 : System.Web.UI.Page
    {
        string strcon = ConfigurationManager.ConnectionStrings["con"].ConnectionString;
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Request.QueryString["action"] == "export")
            {
                DataTable filteredData = GetFilteredDataForExport();
                ExportToExcel(filteredData);
                PopulateItemsDropdown();
                PopulateVendorDropdown();
                return;
            }

            if (Request.HttpMethod == "POST" && !string.IsNullOrEmpty(Request["draw"]))
            {
                excel.Visible = false;
                btnExport.Visible = false;
                RespondWithData();
                PopulateItemsDropdown();
                PopulateVendorDropdown();
                return;
            }

            if (!IsPostBack)
            {
                var permissions = Session["formPermissions"] as Dictionary<string, string>;
                string expiryPerm = permissions != null && permissions.ContainsKey("ConsignmentList") ? permissions["ConsignmentList"] : "";

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

                Panel1.Visible = true;
                BindGridView();
                BindStores();
                PopulateItemsDropdown();
                PopulateVendorDropdown();
            }
            else
            {
                // Restore filter states
                if (ViewState["FilterStoreChecked"] != null)
                {
                    filterStore.Checked = (bool)ViewState["FilterStoreChecked"];
                }

                if (ViewState["SelectedStores"] != null)
                {
                    var selectedStores = ViewState["SelectedStores"].ToString().Split(',');
                    foreach (ListItem item in lstStoreFilter.Items)
                    {
                        item.Selected = selectedStores.Contains(item.Value);
                    }
                }

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

        [WebMethod]
        public static string DeleteRecord1(string id)
        {
            try
            {
                string strcon = ConfigurationManager.ConnectionStrings["con"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(strcon))
                {
                    string query = "DELETE FROM itemListC WHERE id = @id";
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

                if (!permissions.TryGetValue("ConsignmentList", out string perm))
                {
                    Response.Write("{\"error\":\"Unauthorized\"}");
                    Response.End();
                    return;
                }

                List<string> storeNos = GetLoggedInUserStoreNames();
                bool hasHO = storeNos.Any(s => s.Equals("HO", StringComparison.OrdinalIgnoreCase));
                string selectedMonth = Request.Form["month"];
                string searchValue = Request["search"] ?? "";

                StringBuilder whereClause = new StringBuilder("1=1");
                List<SqlParameter> parameters = new List<SqlParameter>();

                if (!string.IsNullOrEmpty(selectedMonth) &&
                    DateTime.TryParseExact(selectedMonth, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime monthDate))
                {
                    whereClause.Append(" AND YEAR(regeDate) = @Year AND MONTH(regeDate) = @Month");
                    parameters.Add(new SqlParameter("@Year", monthDate.Year));
                    parameters.Add(new SqlParameter("@Month", monthDate.Month));
                }

                whereClause.Append(" AND (status IS NOT NULL AND status <> '')");

                if (!hasHO && storeNos.Count > 0)
                {
                    string[] storeConditions = storeNos.Select((s, i) => $"@store{i}").ToArray();
                    whereClause.Append($" AND storeNo IN ({string.Join(",", storeConditions)})");

                    for (int i = 0; i < storeNos.Count; i++)
                    {
                        parameters.Add(new SqlParameter($"@store{i}", storeNos[i]));
                    }
                }

                if (!string.IsNullOrEmpty(searchValue))
                {
                    string[] searchableColumns = {
                        "no", "itemNo", "description", "barcodeNo", "qty", "uom", "packingInfo", "storeNo",
                        "staffName", "vendorNo", "vendorName", "regeDate", "status","note", "completedDate"
                    };

                    whereClause.Append(" AND (");
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
                    "id", "no", "itemNo", "description", "barcodeNo", "qty", "uom",
                    "packingInfo", "storeNo", "staffName", "vendorNo", "vendorName", "regeDate", "status", "note", "completedDate"
                };
                string orderBy = columns.ElementAtOrDefault(orderColumnIndex) ?? "id";

                string dataQuery = $@"
                        SELECT * 
                        FROM itemListC 
                        WHERE {whereClause}
                        ORDER BY {orderBy} {orderDir}
                        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                            string countQuery = $"SELECT COUNT(*) FROM itemListC WHERE {whereClause}";

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
                    barcodeNo = row["barcodeNo"],
                    qty = row["qty"],
                    uom = row["uom"],
                    packingInfo = row["packingInfo"],
                    storeNo = row["storeNo"],
                    staffName = row["staffName"],
                    vendorNo = row["vendorNo"],
                    vendorName = row["vendorName"],
                    regeDate = row["regeDate"],
                    status = row["status"],
                    note = row["note"],
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

        protected void GridView1_RowEditing(object sender, GridViewEditEventArgs e)
        {
            GridView2.EditIndex = e.NewEditIndex;
            BindGridView();
        }

        protected void GridView1_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            GridView2.EditIndex = -1;
            BindGridView();
            Response.Redirect("viewer3.aspx");
        }

        protected string GetStatusText(string selectedAction)
        {
            switch (selectedAction)
            {
                case "1":
                    return "Confirm";
                default:
                    return "";
            }
        }

        protected void ApplyFilters_Click(object sender, EventArgs e)
        {
            try
            {
                // Save filter states
                ViewState["FilterStatusChecked"] = filterStatus.Checked;
                ViewState["SelectedStatus"] = ddlStatusFilter.SelectedValue;

                ViewState["FilterStoreChecked"] = filterStore.Checked;
                ViewState["SelectedStores"] = string.Join(",",
                    lstStoreFilter.Items.Cast<ListItem>()
                        .Where(li => li.Selected)
                        .Select(li => li.Value));

                ViewState["FilterItemChecked"] = filterItem.Checked;
                ViewState["SelectedItem"] = item.SelectedValue;

                ViewState["FilterStaffChecked"] = filterStaff.Checked;
                ViewState["SelectedStaff"] = txtstaffFilter.Text;

                ViewState["FilterVendorChecked"] = filterVendor.Checked;
                ViewState["SelectedVendor"] = vendor.SelectedValue;

                ViewState["FilterRegDateChecked"] = filterRegistrationDate.Checked;
                ViewState["SelectedRegDate"] = txtRegDateFilter.Text;

                ViewState["FilterCompletedChecked"] = filterCompletedDate.Checked;
                ViewState["SelectedCompleted"] = txtCompletedFilter.Text;

                // Get filtered data
                DataTable dt = GetFilteredData();

                // Bind to GridView
                GridView2.DataSource = dt;
                GridView2.DataBind();

            }
            catch (Exception ex)
            {
                Debug.WriteLine("Filter error: " + ex.ToString());
                ScriptManager.RegisterStartupScript(this, GetType(), "Error",
                    $"alert('Error applying filters: {ex.Message.Replace("'", "\\'")}');", true);
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
            Panel1.Visible = !Panel1.Visible;
            BindStores();
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
            BindGridView();
        }

        protected void ResetFilters_Click(object sender, EventArgs e)
        {
            ddlStatusFilter.SelectedIndex = 0;
            lstStoreFilter.ClearSelection();
            item.SelectedIndex = 0;
            txtstaffFilter.Text = string.Empty;
            vendor.SelectedIndex = 0;
            txtCompletedFilter.Text = string.Empty;
            txtRegDateFilter.Text = string.Empty;

            filterStatus.Checked = false;
            filterStore.Checked = false;
            filterItem.Checked = false;
            filterStaff.Checked = false;
            filterVendor.Checked = false;
            filterCompletedDate.Checked = false;
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
            ViewState["FilterCompletedChecked"] = null;
            ViewState["SelectedCompleted"] = null;
            ViewState["FilterRegDateChecked"] = null;
            ViewState["SelectedRegDate"] = null;
            ViewState["FilterStaffChecked"] = null;
            ViewState["SelectedStaff"] = null;

            ScriptManager.RegisterStartupScript(this, GetType(), "redirect",
                "window.location.href = 'viewer3.aspx';", true);

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

        private void BindGridView(int pageNumber = 1, int pageSize = 100)
        {
            using (SqlConnection conn = new SqlConnection(strcon))
            {
                string orderBy = string.IsNullOrEmpty(hfSelectedIDs.Value)
                    ? "ORDER BY id"
                    : $"ORDER BY CASE WHEN id = '{hfSelectedIDs.Value}' THEN 0 ELSE 1 END, id";

                string username = User.Identity.Name;
                var permissions = GetAllowedFormsByUser(username);
                Session["formPermissions"] = permissions;
                Session["activeModule"] = "ConsignmentList";

                if (!permissions.TryGetValue("ConsignmentList", out string perm))
                {
                    ShowAlert("Unauthorized", "You do not have permission to access Consignment List", "error");
                    return;
                }

                List<string> storeNos = GetLoggedInUserStoreNames();
                bool hasHO = storeNos.Any(s => s.Equals("HO", StringComparison.OrdinalIgnoreCase));

                StringBuilder query = new StringBuilder("SELECT * FROM itemListC WHERE 1=1");
                var cmd = new SqlCommand();
                cmd.Connection = conn;

                // Permission-specific conditions
                if (perm == "edit" || perm == "view")
                {
                    if (!hasHO && storeNos.Count > 0)
                    {
                        string storeFilter = string.Join(",", storeNos.Select((s, i) => $"@store{i}"));
                        query.Append($" AND storeNo IN ({storeFilter})");
                        for (int i = 0; i < storeNos.Count; i++)
                        {
                            cmd.Parameters.AddWithValue($"@store{i}", storeNos[i]);
                        }
                    }
                }
                else if (perm == "edit" || perm == "view" || perm == "admin")
                {
                    query.Append(" AND (status IS NOT NULL AND status <> '')");
                    if (!hasHO && storeNos.Count > 0)
                    {
                        string storeFilter = string.Join(",", storeNos.Select((s, i) => $"@store{i}"));
                        query.Append($" AND storeNo IN ({storeFilter})");
                        for (int i = 0; i < storeNos.Count; i++)
                        {
                            cmd.Parameters.AddWithValue($"@store{i}", storeNos[i]);
                        }
                    }
                }

                // Add pagination
                query.Append($@" {orderBy} OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY");
                cmd.CommandText = query.ToString();
                cmd.Parameters.AddWithValue("@Offset", (pageNumber - 1) * pageSize);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);

                // Execute and bind
                conn.Open();
                DataTable dt = new DataTable();
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
                conn.Close();

                GridView2.DataSource = dt;
                GridView2.PageIndex = 0;
                GridView2.DataBind();
            }
        }

        private string FormatCellValue2(DataColumn col, object cellValue)
        {
            if (cellValue == DBNull.Value)
                return string.Empty;

            if (col.DataType == typeof(DateTime))
            {
                DateTime dateValue = (DateTime)cellValue;

                if (col.ColumnName.Equals("completedDate", StringComparison.OrdinalIgnoreCase))
                {
                    return dateValue.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
                }
            }

            // Handle Myanmar text columns without HTML encoding
            if (col.ColumnName.Equals("note", StringComparison.OrdinalIgnoreCase) ||
                col.ColumnName.Equals("remark", StringComparison.OrdinalIgnoreCase))
            {
                return cellValue.ToString()
                    .Replace(Environment.NewLine, "<br/>")
                    .Replace("\n", "<br/>");
            }

            // HTML encode other columns
            return HttpUtility.HtmlEncode(cellValue.ToString())
                   .Replace("\n", "<br/>")
                   .Replace(Environment.NewLine, "<br/>");
        }

        protected void btnExport_Click(object sender, EventArgs e)
        {
            try
            {
                DataTable dt = GetFilteredData();

                if (dt != null && dt.Rows.Count > 0)
                {
                    ExportToExcel(dt);
                }
                else
                {
                    ScriptManager.RegisterStartupScript(this, GetType(), "noDataAlert",
                        "alert('No data to export.');", true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Export error: {ex}");
                ScriptManager.RegisterStartupScript(this, GetType(), "exportError",
                    "alert('Error exporting data. Please try again.');", true);
            }
        }

        private DataTable GetFilteredData()
        {
            string username = User.Identity.Name;
            var permissions = GetAllowedFormsByUser(username);
            Session["formPermissions"] = permissions;
            Session["activeModule"] = "ConsignmentList";

            if (!permissions.TryGetValue("ConsignmentList", out string perm))
            {
                ShowAlert("Unauthorized", "You do not have permission to access Consignment List", "error");
                return new DataTable();
            }

            var userStores = Session["storeListRaw"] as List<string> ?? new List<string>();
            bool hasHO = userStores.Any(s => s.Equals("HO", StringComparison.OrdinalIgnoreCase));

            StringBuilder query = new StringBuilder("SELECT * FROM itemListC WHERE 1=1");
            List<SqlParameter> parameters = new List<SqlParameter>();

            // Permission filter
            if (perm == "edit" || perm == "view" || perm == "admin")
            {
                query.Append(" AND (status IS NOT NULL or status <> '')");
            }
            else
            {
                query.Append(" AND 1=0");
            }

            // Store filter
            if (filterStore.Checked)
            {
                var selectedStores = lstStoreFilter.Items.Cast<ListItem>()
                    .Where(li => li.Selected)
                    .Select(li => li.Value)
                    .ToList();

                if (selectedStores.Contains("all"))
                {
                    if (!hasHO)
                    {
                        // For non-HO users, "All Stores" means all their accessible stores
                        if (userStores.Count > 0)
                        {
                            string storeCondition = string.Join(",", userStores.Select((s, i) => $"@Store{i}"));
                            query.Append($" AND storeNo IN ({storeCondition})");
                            for (int i = 0; i < userStores.Count; i++)
                            {
                                parameters.Add(new SqlParameter($"@Store{i}", userStores[i]));
                            }
                        }
                        else
                        {
                            query.Append(" AND 1=0"); // No accessible stores
                        }
                    }
                }
                else
                {
                    // Regular store selection
                    List<string> filteredStores = hasHO ?
                        selectedStores :
                        selectedStores.Intersect(userStores).ToList();

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
                        query.Append(" AND 1=0"); // No valid stores selected
                    }
                }
            }
            else if (!hasHO) // Store filter not checked and non-HO user
            {
                // Apply user's store restrictions
                if (userStores.Count > 0)
                {
                    string storeFilterCondition = string.Join(",", userStores.Select((s, i) => $"@UserStore{i}"));
                    query.Append($" AND storeNo IN ({storeFilterCondition})");
                    for (int i = 0; i < userStores.Count; i++)
                    {
                        parameters.Add(new SqlParameter($"@UserStore{i}", userStores[i]));
                    }
                }
                else
                {
                    query.Append(" AND 1=0");
                }
            }

            // Item filter
            if (filterItem.Checked && !string.IsNullOrEmpty(item.SelectedValue))
            {
                query.Append(" AND itemNo = @ItemNo");
                parameters.Add(new SqlParameter("@ItemNo", item.SelectedValue));
            }

            // Vendor filter
            if (filterVendor.Checked && !string.IsNullOrEmpty(vendor.SelectedValue))
            {
                query.Append(" AND vendorNo = @VendorNo");
                parameters.Add(new SqlParameter("@VendorNo", vendor.SelectedValue));
            }

            // Status filter
            if (filterStatus.Checked && !string.IsNullOrEmpty(ddlStatusFilter.SelectedItem.Text))
            {
                query.Append(" AND status = @Status");
                parameters.Add(new SqlParameter("@Status", ddlStatusFilter.SelectedItem.Text));
            }

            // Completed Date filter
            if (filterCompletedDate.Checked && !string.IsNullOrWhiteSpace(txtCompletedFilter.Text))
            {
                if (DateTime.TryParse(txtCompletedFilter.Text, out DateTime comDate))
                {
                    DateTime startDate = comDate.Date;
                    DateTime endDate = startDate.AddDays(1);
                    query.Append(" AND completedDate >= @StartComDate AND completedDate < @EndComDate");
                    parameters.Add(new SqlParameter("@StartComDate", startDate));
                    parameters.Add(new SqlParameter("@EndComDate", endDate));
                }
            }

            // Registration Date filter
            if (filterRegistrationDate.Checked && !string.IsNullOrWhiteSpace(txtRegDateFilter.Text))
            {
                if (DateTime.TryParse(txtRegDateFilter.Text, out DateTime regDate))
                {
                    DateTime startDate = regDate.Date;
                    DateTime endDate = startDate.AddDays(1);
                    query.Append(" AND regeDate >= @StartRegDate AND regeDate < @EndRegDate");
                    parameters.Add(new SqlParameter("@StartRegDate", startDate));
                    parameters.Add(new SqlParameter("@EndRegDate", endDate));
                }
            }

            // Staff filter
            if (filterStaff.Checked && !string.IsNullOrWhiteSpace(txtstaffFilter.Text))
            {
                query.Append(" AND staffName = @StaffNo");
                parameters.Add(new SqlParameter("@StaffNo", txtstaffFilter.Text));
            }

            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(strcon))
            {
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
                        Debug.WriteLine("Database error: " + ex.ToString());
                        throw;
                    }
                }
            }
            

            return dt;
        }

        private void ExportToExcel(DataTable dt)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            Response.Clear();
            Response.Buffer = true;

            using (ExcelPackage pck = new ExcelPackage())
            {
                ExcelWorksheet ws = pck.Workbook.Worksheets.Add("ConsignmentList");
                ws.Cells["A1"].LoadFromDataTable(dt, true, TableStyles.Medium1);
                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", "attachment; filename=ConsignmentList.xlsx");

                Response.BinaryWrite(pck.GetAsByteArray());
                Response.Flush();
                Response.End();
            }
        }

        private DataTable GetFilteredDataForExport()
        {
            string username = User.Identity.Name;
            var permissions = GetAllowedFormsByUser(username);
            Session["formPermissions"] = permissions;
            Session["activeModule"] = "ConsignmentList";

            if (!permissions.TryGetValue("ConsignmentList", out string perm))
            {
                ShowAlert("Unauthorized", "You do not have permission to access Consignment List", "error");
                return new DataTable(); 
            }

            List<string> userStores = Session["storeListRaw"] as List<string> ?? GetLoggedInUserStoreNames();

            bool hasHO = userStores.Any(s => s.Equals("HO", StringComparison.OrdinalIgnoreCase));

            List<string> selectedStores = new List<string>();

            if (filterStore.Checked)
            {
                selectedStores = lstStoreFilter.Items.Cast<ListItem>()
                    .Where(li => li.Selected && li.Value != "all")
                    .Select(li => li.Value)
                    .ToList();

                if (!hasHO)
                {
                    // Restrict selected stores to user's allowed stores
                    selectedStores = selectedStores.Intersect(userStores).ToList();
                }
            }
            else
            {
                selectedStores = hasHO ? new List<string>() : userStores;
            }

            using (SqlConnection conn = new SqlConnection(strcon))
            {
                conn.Open();
                DataTable dt = new DataTable();

                StringBuilder query = new StringBuilder(@"
                    SELECT 
                        no, itemNo, description, barcodeNo, qty, uom, packingInfo,
                        storeNo, staffName,
                        vendorNo, vendorName,
                        CONVERT(DATE, regeDate) AS regeDate, status, note,
                        CONVERT(DATE, completedDate) AS completedDate
                    FROM itemListC
                    WHERE 1 = 1
                ");

                List<SqlParameter> parameters = new List<SqlParameter>();

                // Permission-based status filter
                if (perm == "edit" || perm == "view" || perm == "admin")
                {
                    query.Append(" AND (status IS NOT NULL or status <> '')");
                }
                else
                {
                    query.Append(" AND 1=0");
                }

                // Store-level restriction
                if (!hasHO)
                {
                    if (selectedStores.Count > 0)
                    {
                        query.Append(" AND storeNo IN (" + string.Join(",", selectedStores.Select((s, i) => $"@Store{i}")) + ")");
                        parameters.AddRange(selectedStores.Select((s, i) => new SqlParameter($"@Store{i}", s)));
                    }
                    else if (!filterStore.Checked && userStores.Count > 0)
                    {
                        query.Append(" AND storeNo IN (" + string.Join(",", userStores.Select((s, i) => $"@UserStore{i}")) + ")");
                        parameters.AddRange(userStores.Select((s, i) => new SqlParameter($"@UserStore{i}", s)));
                    }
                    else
                    {
                        query.Append(" AND 1 = 0"); 
                    }
                }
                else
                {
                    if (selectedStores.Count > 0)
                    {
                        query.Append(" AND storeNo IN (" + string.Join(",", selectedStores.Select((s, i) => $"@Store{i}")) + ")");
                        parameters.AddRange(selectedStores.Select((s, i) => new SqlParameter($"@Store{i}", s)));
                    }
                }

                query.Append(" ORDER BY id");

                using (SqlCommand cmd = new SqlCommand(query.ToString(), conn))
                {
                    cmd.Parameters.AddRange(parameters.ToArray());

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }

                // Ensure column types are preserved
                if (dt.Columns.Contains("completedDate"))
                {
                    dt.Columns["completedDate"].DateTimeMode = DataSetDateTime.Unspecified;
                }

                return dt;
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
                                                   AND storeNo <> ''", con))
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