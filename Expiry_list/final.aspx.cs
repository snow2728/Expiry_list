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

namespace Expiry_list
{
    public partial class final : System.Web.UI.Page
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

                whereClause.Append(" AND status IN ('Exchange', 'No Exchange', 'No Action')");

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
                "no", "itemNo", "description", "barcodeNo", "qty", "uom", "packingInfo", "expiryDate", "storeNo",
                "staffName", "batchNo", "vendorNo", "vendorName", "regeDate", "action", "status","note", "remark", "completedDate"
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

        protected void GridView1_RowEditing(object sender, GridViewEditEventArgs e)
        {
            GridView2.EditIndex = e.NewEditIndex;
            BindGridView();
        }

        protected void GridView1_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            GridView2.EditIndex = -1;
            BindGridView();
            Response.Redirect("final.aspx");
        }

        protected string GetActionText(string action)
        {
            switch (action)
            {
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
                //case "1":
                //    return "Collected Item";
                //case "2":
                //    return "User Error";
                //case "3":
                //    return "Sold Out";
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
                ViewState["FilterStoreChecked"] = filterStore.Checked;
                ViewState["SelectedStores"] = string.Join(",",
                    lstStoreFilter.Items.Cast<ListItem>()
                        .Where(li => li.Selected)
                        .Select(li => li.Value));

                ViewState["FilterItemChecked"] = filterItem.Checked;
                ViewState["SelectedItem"] = item.SelectedValue;

                ViewState["FilterVendorChecked"] = filterVendor.Checked;
                ViewState["SelectedVendor"] = vendor.SelectedValue;

                ViewState["FilterStatusChecked"] = filterStatus.Checked;
                ViewState["SelectedStatus"] = ddlStatusFilter.SelectedItem.Text;

                ViewState["FilterActionChecked"] = filterAction.Checked;
                ViewState["SelectedAction"] = ddlActionFilter.SelectedItem.Text;

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

                //// Debug output
                //Debug.WriteLine("Applied filters:");
                //Debug.WriteLine($"Stores: {ViewState["SelectedStores"]}");
                //Debug.WriteLine($"Item: {ViewState["SelectedItem"]}");
                //Debug.WriteLine($"Vendor: {ViewState["SelectedVendor"]}");
                //Debug.WriteLine($"Status: {ViewState["SelectedStatus"]}");
                //Debug.WriteLine($"Action: {ViewState["SelectedAction"]}");
                //Debug.WriteLine($"Expiry Date: {ViewState["SelectedExpiryDate"]}");
                //Debug.WriteLine($"Reg Date: {ViewState["SelectedRegDate"]}");
                //Debug.WriteLine($"Staff: {ViewState["SelectedStaff"]}");
                //Debug.WriteLine($"Batch: {ViewState["SelectedBatch"]}");
                //Debug.WriteLine($"Row count: {dt.Rows.Count}");
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
            // Reset all filter controls
            ddlActionFilter.SelectedIndex = 0;
            ddlStatusFilter.SelectedIndex = 0;
            lstStoreFilter.ClearSelection();
            item.SelectedIndex = 0;
            txtstaffFilter.Text = string.Empty;
            vendor.SelectedIndex = 0;
            txtExpiryDateFilter.Text = string.Empty;
            txtRegDateFilter.Text = string.Empty;
            txtBatchNoFilter.Text = string.Empty;

            filterAction.Checked = false;
            filterStatus.Checked = false;
            filterStore.Checked = false;
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
     "window.location.href = 'final.aspx';", true);

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
                Session["activeModule"] = "ExpiryList";

                if (!permissions.TryGetValue("ExpiryList", out string perm))
                {
                    ShowAlert("Unauthorized", "You do not have permission to access Expiry List", "error");
                    return;
                }

                List<string> storeNos = GetLoggedInUserStoreNames();
                bool hasHO = storeNos.Any(s => s.Equals("HO", StringComparison.OrdinalIgnoreCase));

                StringBuilder query = new StringBuilder("SELECT * FROM itemList WHERE 1=1");
                var cmd = new SqlCommand();
                cmd.Connection = conn;

                // Permission-specific conditions
                if (perm == "edit" || perm=="view")
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
                else if (perm=="edit" || perm == "view" || perm == "admin")
                {
                    query.Append(" AND status IN ('Exchange', 'No Exchange', 'No Action')");
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

        private void ExportToExcel2(DataTable dt)
        {
            // Force UTF-8 encoding for Unicode characters
            Response.ContentEncoding = Encoding.UTF8;
            Response.HeaderEncoding = Encoding.UTF8;

            Response.Clear();
            Response.Buffer = true;
            Response.AddHeader("content-disposition", "attachment;filename=ExpiryList.xls");
            Response.Charset = "UTF-8";
            Response.ContentType = "application/vnd.ms-excel; charset=utf-8";

            using (StringWriter sw = new StringWriter())
            {
                using (HtmlTextWriter hw = new HtmlTextWriter(sw))
                {
                    // Create HTML table structure with Myanmar font support
                    hw.Write("<table border='1' cellpadding='3' cellspacing='0' style='font-family:Padauk,Myanmar Text,sans-serif;'>");

                    // Write headers
                    hw.Write("<tr style='background-color:#E0E0E0;'>");
                    foreach (DataColumn column in dt.Columns)
                    {
                        hw.Write($"<th>{HttpUtility.HtmlEncode(column.ColumnName)}</th>");
                    }
                    hw.Write("</tr>");

                    // Write data rows
                    foreach (DataRow row in dt.Rows)
                    {
                        hw.Write("<tr>");
                        foreach (DataColumn column in dt.Columns)
                        {
                            object cellValue = row[column];
                            hw.Write("<td style='mso-number-format:\\@;'>");
                            hw.Write(FormatCellValue2(column, cellValue));
                            hw.Write("</td>");
                        }
                        hw.Write("</tr>");
                    }

                    hw.Write("</table>");

                    Response.Output.Write(sw.ToString());
                    Response.Flush();
                    Response.End();
                }
            }
        }

        private string FormatCellValue2(DataColumn col, object cellValue)
        {
            if (cellValue == DBNull.Value)
                return string.Empty;

            if (col.DataType == typeof(DateTime))
            {
                DateTime dateValue = (DateTime)cellValue;

                if (col.ColumnName.Equals("expiryDate", StringComparison.OrdinalIgnoreCase))
                {
                    return dateValue.ToString("MMM-yyyy", CultureInfo.InvariantCulture);
                }
                return dateValue.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
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
                DataTable dt = GetFilteredData2();

                if (dt != null && dt.Rows.Count > 0)
                {
                    ExportToExcel2(dt);
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

        private DataTable GetFilteredData2()
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
                SELECT no, itemNo, description, barcodeNo, qty, uom, packingInfo,
                       CONVERT(DATE, expiryDate) AS expiryDate,
                       storeNo, staffName, batchNo, vendorNo, vendorName,
                       CONVERT(DATE, regeDate) AS regeDate,
                       action, status, note, remark,
                       CONVERT(DATE, completedDate) AS completedDate
                FROM itemList
                WHERE 1=1");

            List<SqlParameter> parameters = new List<SqlParameter>();

            // Base permission filter
            if (perm == "edit" || perm == "view" || perm == "admin")
            {
                query.Append(" AND status IN ('Exchange', 'No Exchange', 'No Action')");
            }

            // UI-based STORE filter
            if (filterStore.Checked)
            {
                var selectedStores = lstStoreFilter.Items.Cast<ListItem>()
                    .Where(li => li.Selected && li.Value != "all")
                    .Select(li => li.Value)
                    .ToList();

                List<string> allowedStores = hasHO
                    ? selectedStores
                    : selectedStores.Intersect(userStores).ToList();

                if (allowedStores.Count > 0)
                {
                    string storeCondition = string.Join(",", allowedStores.Select((s, i) => $"@Store{i}"));
                    query.Append($" AND storeNo IN ({storeCondition})");

                    for (int i = 0; i < allowedStores.Count; i++)
                        parameters.Add(new SqlParameter($"@Store{i}", allowedStores[i]));
                }
                else
                {
                    query.Append(" AND 1=0"); // No access if no allowed stores
                }
            }
            else if (!hasHO)
            {
                // If no UI filter and not HO, limit by user's assigned stores
                if (userStores.Count > 0)
                {
                    string storeCondition = string.Join(",", userStores.Select((s, i) => $"@UserStore{i}"));
                    query.Append($" AND storeNo IN ({storeCondition})");

                    for (int i = 0; i < userStores.Count; i++)
                        parameters.Add(new SqlParameter($"@UserStore{i}", userStores[i]));
                }
                else
                {
                    query.Append(" AND 1=0"); // No assigned stores
                }
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

            // EXPIRY DATE filter
            if (filterExpiryDate.Checked && !string.IsNullOrWhiteSpace(txtExpiryDateFilter.Text))
            {
                var firstDayDates = new List<DateTime>();
                string[] parts = txtExpiryDateFilter.Text.Split('|');

                string[] formats = { "MMMM.yyyy", "MMM.yyyy", "MMMM-yyyy", "MMM-yyyy", "MMMM/yyyy", "MMM/yyyy" };

                foreach (var part in parts)
                {
                    foreach (var fmt in formats)
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
                    string expiryConds = string.Join(" OR ", firstDayDates.Select((d, i) => $"expiryDate = @Exp{i}"));
                    query.Append($" AND ({expiryConds})");

                    for (int i = 0; i < firstDayDates.Count; i++)
                        parameters.Add(new SqlParameter($"@Exp{i}", firstDayDates[i]));
                }
            }

            // REGISTRATION DATE filter
            if (filterRegistrationDate.Checked && !string.IsNullOrWhiteSpace(txtRegDateFilter.Text))
            {
                if (DateTime.TryParseExact(txtRegDateFilter.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime regDate))
                {
                    DateTime nextDay = regDate.AddDays(1);
                    query.Append(" AND regeDate >= @RegDate AND regeDate < @NextDay");
                    parameters.Add(new SqlParameter("@RegDate", regDate));
                    parameters.Add(new SqlParameter("@NextDay", nextDay));
                }
            }

            // STAFF filter
            if (filterStaff.Checked && !string.IsNullOrWhiteSpace(txtstaffFilter.Text))
            {
                query.Append(" AND staffName = @StaffNo");
                parameters.Add(new SqlParameter("@StaffNo", txtstaffFilter.Text));
            }

            // BATCH filter
            if (filterBatch.Checked && !string.IsNullOrWhiteSpace(txtBatchNoFilter.Text))
            {
                query.Append(" AND batchNo = @BatchNo");
                parameters.Add(new SqlParameter("@BatchNo", txtBatchNoFilter.Text));
            }

            Debug.WriteLine("Final Export Query: " + query.ToString());

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
                    Debug.WriteLine("Database error: " + ex.ToString());
                    throw;
                }
            }

            return dt;
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

            StringBuilder query = new StringBuilder("SELECT * FROM itemList WHERE 1=1");
            List<SqlParameter> parameters = new List<SqlParameter>();

            // Permission filter
            if (perm == "edit" || perm == "view" || perm == "admin")
            {
                query.Append(" AND status IN ('Exchange', 'No Exchange', 'No Action')");
            }

            // Store filter
            if (filterStore.Checked)
            {
                var selectedStores = lstStoreFilter.Items.Cast<ListItem>()
                    .Where(li => li.Selected && li.Value != "all")
                    .Select(li => li.Value)
                    .ToList();

                List<string> filteredStores;

                if (hasHO)
                {
                    filteredStores = selectedStores;
                }
                else
                {
                    filteredStores = selectedStores.Intersect(userStores).ToList();
                }

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
                    // No matching allowed stores, return no data
                    query.Append(" AND 1=0");
                }
            }
            else if (!hasHO)
            {
                // Filter by assigned stores even if UI filter not checked
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

            // Action filter
            if (filterAction.Checked && !string.IsNullOrEmpty(ddlActionFilter.SelectedItem.Text))
            {
                query.Append(" AND action = @Action");
                parameters.Add(new SqlParameter("@Action", ddlActionFilter.SelectedItem.Text));
            }

            // Expiry date filter
            if (filterExpiryDate.Checked && !string.IsNullOrWhiteSpace(txtExpiryDateFilter.Text))
            {
                var firstDayDates = new List<DateTime>();
                string[] parts = txtExpiryDateFilter.Text.Split('|');

                string[] formats = new[] { "MMMM.yyyy", "MMM.yyyy", "MMMM-yyyy", "MMM-yyyy", "MMMM/yyyy", "MMM/yyyy" };

                foreach (var part in parts)
                {
                    foreach (var fmt in formats)
                    {
                        if (DateTime.TryParseExact(part.Trim(), fmt, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
                        {
                            firstDayDates.Add(new DateTime(parsedDate.Year, parsedDate.Month, 1));
                            break;
                        }
                    }
                }

                if (firstDayDates.Count > 0)
                {
                    string conditions = string.Join(" OR ", firstDayDates.Select((d, i) => $"expiryDate = @Exp{i}"));
                    query.Append($" AND ({conditions})");

                    for (int i = 0; i < firstDayDates.Count; i++)
                    {
                        parameters.Add(new SqlParameter($"@Exp{i}", firstDayDates[i]));
                    }
                }
            }

            // Registration Date filter
            if (filterRegistrationDate.Checked && !string.IsNullOrWhiteSpace(txtRegDateFilter.Text))
            {
                if (DateTime.TryParseExact(txtRegDateFilter.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime regDate))
                {
                    DateTime nextDay = regDate.AddDays(1);
                    query.Append(" AND regeDate >= @RegDate AND regeDate < @NextDay");
                    parameters.Add(new SqlParameter("@RegDate", regDate));
                    parameters.Add(new SqlParameter("@NextDay", nextDay));
                }
            }

            // Staff filter
            if (filterStaff.Checked && !string.IsNullOrWhiteSpace(txtstaffFilter.Text))
            {
                query.Append(" AND staffName = @StaffNo");
                parameters.Add(new SqlParameter("@StaffNo", txtstaffFilter.Text));
            }

            // Batch filter
            if (filterBatch.Checked && !string.IsNullOrWhiteSpace(txtBatchNoFilter.Text))
            {
                query.Append(" AND batchNo = @BatchNo");
                parameters.Add(new SqlParameter("@BatchNo", txtBatchNoFilter.Text));
            }

            // Debug
            Debug.WriteLine("Final query: " + query);

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
                    Debug.WriteLine("Database error: " + ex.ToString());
                    throw;
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
                ExcelWorksheet ws = pck.Workbook.Worksheets.Add("ExpiryList");
                ws.Cells["A1"].LoadFromDataTable(dt, true, TableStyles.Medium1);
                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", "attachment; filename=ExpiryList.xlsx");

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
            Session["activeModule"] = "ExpiryList";

            if (!permissions.TryGetValue("ExpiryList", out string perm))
            {
                ShowAlert("Unauthorized", "You do not have permission to access Expiry List", "error");
                return new DataTable(); // Return empty
            }

            // Get assigned stores from session or DB
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
                        CONVERT(DATE, expiryDate) AS expiryDate,
                        storeNo, staffName, batchNo,
                        vendorNo, vendorName,
                        CONVERT(DATE, regeDate) AS regeDate,
                        action, status, note, remark,
                        CONVERT(DATE, completedDate) AS completedDate
                    FROM itemList
                    WHERE 1 = 1
                ");

                List<SqlParameter> parameters = new List<SqlParameter>();

                // Permission-based status filter
                if (perm == "edit" || perm == "view" || perm == "admin")
                {
                    query.Append(" AND status IN ('Exchange', 'No Exchange', 'No Action') ");
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
                        query.Append(" AND 1 = 0"); // No store access
                    }
                }
                else
                {
                    // HO users: use selectedStores if any
                    if (selectedStores.Count > 0)
                    {
                        query.Append(" AND storeNo IN (" + string.Join(",", selectedStores.Select((s, i) => $"@Store{i}")) + ")");
                        parameters.AddRange(selectedStores.Select((s, i) => new SqlParameter($"@Store{i}", s)));
                    }
                    // else no filter needed (all stores)
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
                if (dt.Columns.Contains("expiryDate"))
                {
                    dt.Columns["expiryDate"].DateTimeMode = DataSetDateTime.Unspecified;
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