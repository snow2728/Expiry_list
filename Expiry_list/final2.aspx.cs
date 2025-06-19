using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Newtonsoft.Json;
using OfficeOpenXml;
using OfficeOpenXml.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.DynamicData;
using System.Web.UI;
using System.Web.UI.WebControls;
using static System.Windows.Forms.LinkLabel;

namespace Expiry_list
{
    public partial class final2 : System.Web.UI.Page
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

                if (!permissions.TryGetValue("ExpiryList", out string perm))
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

                // Month filter
                if (!string.IsNullOrEmpty(selectedMonth) &&
                    DateTime.TryParseExact(selectedMonth, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime monthDate))
                {
                    whereClause.Append(" AND YEAR(regeDate) = @Year AND MONTH(regeDate) = @Month");
                    parameters.Add(new SqlParameter("@Year", monthDate.Year));
                    parameters.Add(new SqlParameter("@Month", monthDate.Month));
                }

                // Permission check
                if (perm == "edit" || perm == "view" || perm == "admin")
                {
                    whereClause.Append(" AND (status NOT IN ('Exchange', 'No Exchange', 'No Action') OR status IS NULL)");

                    if (storeNos.Count > 0)
                    {
                        whereClause.Append($" AND storeNo IN ({string.Join(",", storeNos.Select((s, i) => $"@store{i}"))})");
                        for (int i = 0; i < storeNos.Count; i++)
                        {
                            parameters.Add(new SqlParameter($"@store{i}", storeNos[i]));
                        }
                    }
                }

                // Search
                if (!string.IsNullOrEmpty(searchValue))
                {
                    whereClause.Append(" AND (");
                    string[] searchableColumns = {
                        "no", "itemNo", "description", "barcodeNo", "qty", "uom", "packingInfo", "expiryDate", "storeNo",
                        "staffName", "batchNo", "vendorNo", "vendorName", "regeDate", "action", "status","note", "remark", "completedDate"
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
                    "id", "no", "itemNo", "description", "barcodeNo", "qty", "uom",
                    "packingInfo", "expiryDate", "storeNo", "staffName", "batchNo",
                    "vendorNo", "vendorName", "regeDate", "action", "status", "note",
                    "remark", "completedDate"
                };
                string orderBy = columns.ElementAtOrDefault(orderColumnIndex) ?? "id";

                string dataQuery = $@"
                    SELECT * 
                    FROM itemList 
                    WHERE {whereClause}
                    ORDER BY {orderBy} {orderDir}
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                string countQuery = $"SELECT COUNT(*) FROM itemList WHERE {whereClause}";

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
                    expiryDate = row["expiryDate"],
                    storeNo = row["storeNo"],
                    staffName = row["staffName"],
                    batchNo = row["batchNo"],
                    vendorNo = row["vendorNo"],
                    vendorName = row["vendorName"],
                    regeDate = row["regeDate"],
                    action = row["action"],
                    status = row["status"],
                    note = row["note"],
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
            Response.Redirect("final2.aspx");
        }

        protected string GetActionText(string action)
        {
            switch (action)
            {
                case "0":
                    return "";
                case "1":
                    return "Informed To Supplier";
                case "2":
                    return "Informed To Owner";
                case "3":
                    return "Supplier Sales";
                case "4":
                    return "Owner Sales";
                case "5":
                    return "Store's Responsibility";
                case "6":
                    return "Store Exchange";
                case "7":
                    return "Store Return";
                case "8":
                    return "No Date To Check";
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
                    return "Progress";
                case "2":
                    return "Exchange";
                case "3":
                    return "No Exchange";
                case "4":
                    return "No Action";
                default:
                    return "";
            }
        }

        protected void ApplyFilters_Click(object sender, EventArgs e)
        {
            try
            {
                // Save filter states
                //ViewState["FilterStoreChecked"] = filterStore.Checked;
                //ViewState["SelectedStores"] = string.Join(",",
                //    lstStoreFilter.Items.Cast<ListItem>()
                //        .Where(li => li.Selected)
                //        .Select(li => li.Value));

                ViewState["FilterItemChecked"] = filterItem.Checked;
                ViewState["SelectedItem"] = item.SelectedValue;

                ViewState["FilterVendorChecked"] = filterVendor.Checked;
                ViewState["SelectedVendor"] = vendor.SelectedValue;

                ViewState["FilterStatusChecked"] = filterStatus.Checked;
                ViewState["SelectedStatus"] = ddlStatusFilter.SelectedValue;

                ViewState["FilterActionChecked"] = filterAction.Checked;
                ViewState["SelectedAction"] = ddlActionFilter.SelectedValue;

                ViewState["FilterExpiryDateChecked"] = filterExpiryDate.Checked;
                ViewState["SelectedExpiryDate"] = txtExpiryDateFilter.Text;

                ViewState["FilterRegDateChecked"] = filterRegistrationDate.Checked;
                ViewState["SelectedRegDate"] = txtRegDateFilter.Text;

                ViewState["FilterStaffChecked"] = filterStaff.Checked;
                ViewState["SelectedStaff"] = txtstaffFilter.Text;

                ViewState["FilterBatchChecked"] = filterBatch.Checked;
                ViewState["SelectedBatch"] = txtBatchNoFilter.Text;

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
            txtExpiryDateFilter.Text = string.Empty;
            txtRegDateFilter.Text = string.Empty;
            txtBatchNoFilter.Text = string.Empty;

            filterAction.Checked = false;
            filterStatus.Checked = false;
            //filterStore.Checked = false;
            filterItem.Checked = false;
            filterStaff.Checked = false;
            filterVendor.Checked = false;
            filterExpiryDate.Checked = false;
            filterRegistrationDate.Checked = false;
            filterBatch.Checked = false;

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
            ViewState["FilterExpiryDateChecked"] = null;
            ViewState["SelectedExpiryDate"] = null;
            ViewState["FilterRegDateChecked"] = null;
            ViewState["SelectedRegDate"] = null;
            ViewState["FilterStaffChecked"] = null;
            ViewState["SelectedStaff"] = null;
            ViewState["FilterBatchChecked"] = null;
            ViewState["SelectedBatch"] = null;

            ScriptManager.RegisterStartupScript(this, GetType(), "redirect",
                "window.location.href = 'final2.aspx';", true);

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
                Session["activeModule"] = "ExpiryList";

                if (!permissions.TryGetValue("ExpiryList", out string perm))
                {
                    ShowAlert("Unauthorized", "You do not have permission to access Expiry List", "error");
                    return;
                }

                string storeNo = string.Join(",", GetLoggedInUserStoreNames());

                string query;

                if (perm == "edit")
                {
                    query = $@"SELECT * FROM itemList 
                         WHERE  (status NOT IN ('Exchange', 'No Exchange', 'No Action') OR status IS NULL) and storeNo = @storeNo
                        {orderBy}
                         OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
                }
                else
                {
                    query = $@"SELECT * FROM itemList 
                          {orderBy} 
                         OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
                }

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Offset", (pageNumber - 1) * pageSize);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);
                if (perm == "edit")
                {
                    cmd.Parameters.AddWithValue("@storeNo", storeNo);
                }

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

        private DataTable GetFilteredData()
        {
            string username = User.Identity.Name;
            var permissions = GetAllowedFormsByUser(username);
            Session["formPermissions"] = permissions;
            Session["activeModule"] = "ExpiryList";

            if (!permissions.TryGetValue("ExpiryList", out string perm))
            {
                ShowAlert("Unauthorized", "You do not have permission to access Expiry List", "error");
                return new DataTable();
            }

            var userStores = Session["storeListRaw"] as List<string> ?? new List<string>();
            bool hasHO = userStores.Any(s => s.Equals("HO", StringComparison.OrdinalIgnoreCase));

            StringBuilder query = new StringBuilder(@"
                SELECT * FROM itemList
                WHERE (status NOT IN ('Exchange', 'No Exchange', 'No Action') OR status IS NULL)");

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

            // EXPIRY DATE (Month-Year) filter
            if (filterExpiryDate.Checked && !string.IsNullOrWhiteSpace(txtExpiryDateFilter.Text))
            {
                List<DateTime> firstDayDates = new List<DateTime>();
                string[] parts = txtExpiryDateFilter.Text.Split('|');
                string[] formats = { "MMMM.yyyy", "MMM.yyyy", "MMMM-yyyy", "MMM-yyyy", "MMMM/yyyy", "MMM/yyyy" };

                foreach (string part in parts)
                {
                    foreach (string fmt in formats)
                    {
                        if (DateTime.TryParseExact(part.Trim(), fmt, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed))
                        {
                            firstDayDates.Add(new DateTime(parsed.Year, parsed.Month, 1));
                            break;
                        }
                    }
                }

                if (firstDayDates.Count > 0)
                {
                    string dateFilter = string.Join(" OR ", firstDayDates.Select((d, i) => $"expiryDate = @Exp{i}"));
                    query.Append($" AND ({dateFilter})");

                    for (int i = 0; i < firstDayDates.Count; i++)
                    {
                        parameters.Add(new SqlParameter($"@Exp{i}", firstDayDates[i]));
                    }
                }
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
                query.Append(" AND staffName = @StaffNo");
                parameters.Add(new SqlParameter("@StaffNo", txtstaffFilter.Text.Trim()));
            }

            // BATCH filter
            if (filterBatch.Checked && !string.IsNullOrWhiteSpace(txtBatchNoFilter.Text))
            {
                query.Append(" AND batchNo = @BatchNo");
                parameters.Add(new SqlParameter("@BatchNo", txtBatchNoFilter.Text.Trim()));
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

        private void ShowAlert(string title, string message, string type)
        {
            ScriptManager.RegisterStartupScript(this, GetType(), "alert",
                $"swal('{title}', '{HttpUtility.JavaScriptStringEncode(message)}', '{type}');", true);
        }
    }
}

