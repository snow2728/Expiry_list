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

                string userRole = Session["role"] as string;
                string storeNo = GetLoggedInUserStoreName();

                string selectedMonth = Request.Form["month"];
                string searchValue = Request["search"] ?? "";

                StringBuilder whereClause = new StringBuilder("1=1");
                var parameters = new List<SqlParameter>();

                if (!string.IsNullOrEmpty(selectedMonth))
                {
                    if (DateTime.TryParseExact(selectedMonth, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime monthDate))
                    {
                        whereClause.Append(" AND YEAR(regeDate) = @Year AND MONTH(regeDate) = @Month");
                        parameters.Add(new SqlParameter("@Year", monthDate.Year));
                        parameters.Add(new SqlParameter("@Month", monthDate.Month));
                    }
                }

                if (userRole == "user")
                {
                    whereClause.Append("  AND status IN ('Exchange', 'No Exchange', 'No Action') AND storeNo = @StoreNo");
                    parameters.Add(new SqlParameter("@StoreNo", storeNo));
                }
                else if (userRole == "viewer" || userRole == "admin")
                {
                    whereClause.Append(" AND status IN ('Exchange', 'No Exchange', 'No Action')");
                }

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

                string orderBy = "id";
                if (orderColumnIndex >= 0 && orderColumnIndex < columns.Length)
                {
                    orderBy = columns[orderColumnIndex];
                }

                string query = $@"
                        SELECT * 
                        FROM itemList 
                        WHERE {whereClause}
                        ORDER BY {orderBy} {orderDir}
                        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    // Clone parameters for this command
                    foreach (var param in parameters)
                    {
                        cmd.Parameters.Add(new SqlParameter(param.ParameterName, param.Value));
                    }
                    cmd.Parameters.AddWithValue("@Offset", start);
                    cmd.Parameters.AddWithValue("@PageSize", length);

                    conn.Open();
                    DataTable dt = new DataTable();
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                    conn.Close();

                    string countQuery = $@"
                SELECT COUNT(*) 
                FROM itemList 
                WHERE {whereClause}";

                    using (SqlCommand countCmd = new SqlCommand(countQuery, conn))
                    {
                        // Clone parameters for the count command
                        foreach (var param in parameters)
                        {
                            countCmd.Parameters.Add(new SqlParameter(param.ParameterName, param.Value));
                        }

                        conn.Open();
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
                        }).ToList();

                        var response = new
                        {
                            draw = draw,
                            recordsTotal = totalRecords,
                            recordsFiltered = totalRecords,
                            data = data,
                            orderColumn = orderColumnIndex,
                            orderDir = orderDir
                        };

                        string jsonResponse = JsonConvert.SerializeObject(response);
                        Response.ContentType = "application/json";
                        Response.Write(jsonResponse);
                        Response.End();
                    }
                }
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

        private string GetLoggedInUserStoreName()
        {
            int storeId = Convert.ToInt32(Session["storeNo"] ?? 0);

            if (storeId == 0 || Session["storeNo"] == null)
            {
                Response.Redirect("login.aspx");
                return null;
            }

            string storeName = null;
            string query = "SELECT StoreNo FROM Stores WHERE id = @StoreId";

            using (SqlConnection conn = new SqlConnection(strcon))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@StoreId", storeId);
                conn.Open();

                storeName = cmd.ExecuteScalar()?.ToString();
            }

            if (string.IsNullOrEmpty(storeName))
            {
                Response.Write("Error: StoreName not found.<br>");
            }

            return storeName;
        }

        private void BindGridView(int pageNumber = 1, int pageSize = 100)
        {
            using (SqlConnection conn = new SqlConnection(strcon))
            {
                string orderBy = string.IsNullOrEmpty(hfSelectedIDs.Value)
                    ? "ORDER BY id"
                    : $"ORDER BY CASE WHEN id = '{hfSelectedIDs.Value}' THEN 0 ELSE 1 END, id";
                string userRole = Session["role"] as string;
                string storeNo = GetLoggedInUserStoreName();

                string query;

                if (userRole == "user")
                {
                    query = $@"SELECT * FROM itemList 
                 WHERE storeNo = @storeNo
                {orderBy}
                 OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
                }
                else if (userRole == "viewer")
                {
                    query = $@"SELECT * FROM itemList 
                 WHERE status IN ('Exchange', 'No Exchange', 'No Action')
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
                if (userRole == "user" )
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
                GridView2.PageIndex = 0; // Force first page after edit
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

            string userRole = Session["role"] as string;
            string storeNo = GetLoggedInUserStoreName();

            string query = "SELECT no, itemNo, description, barcodeNo,qty, uom,packingInfo,CONVERT(DATE, expiryDate) AS expiryDate, storeNo, staffName,batchNo, vendorNo,vendorName, CONVERT(DATE, regeDate) AS regeDate,action,status,note,remark, CONVERT(DATE, completedDate) AS completedDate FROM itemList WHERE ";
            List<SqlParameter> parameters = new List<SqlParameter>();

            // Base filter based on role
            if (userRole == "user")
            {
                query += "status IN ('Exchange', 'No Exchange', 'No Action') and storeNo = @StoreNo";
                parameters.Add(new SqlParameter("@StoreNo", storeNo));
            }
            else if (userRole == "viewer" || userRole == "admin")
            {
                query += "status IN ('Exchange', 'No Exchange', 'No Action')";
            }
            else
            {
                query += "1=1";
            }

            // STORE filter
            if (filterStore.Checked)
            {
                var selectedStores = lstStoreFilter.Items.Cast<ListItem>()
                    .Where(li => li.Selected && li.Value != "all")
                    .Select(li => li.Value)
                    .ToList();

                Debug.WriteLine($"Selected stores: {string.Join(",", selectedStores)}");

                if (selectedStores.Count > 0)
                {
                    query += " AND storeNo IN (" + string.Join(",", selectedStores.Select((s, i) => $"@Store{i}")) + ")";
                    parameters.AddRange(selectedStores.Select((s, i) => new SqlParameter($"@Store{i}", s)));
                }
            }

            // ITEM filter
            if (filterItem.Checked && !string.IsNullOrEmpty(item.SelectedValue))
            {
                Debug.WriteLine($"Selected item: {item.SelectedValue}");
                query += " AND itemNo = @ItemNo";
                parameters.Add(new SqlParameter("@ItemNo", item.SelectedValue));
            }

            // VENDOR filter
            if (filterVendor.Checked && !string.IsNullOrEmpty(vendor.SelectedValue))
            {
                Debug.WriteLine($"Selected vendor: {vendor.SelectedValue}");
                query += " AND vendorNo = @VendorNo";
                parameters.Add(new SqlParameter("@VendorNo", vendor.SelectedValue));
            }

            // STATUS filter
            if (filterStatus.Checked && !string.IsNullOrEmpty(ddlStatusFilter.SelectedItem.Text))
            {
                Debug.WriteLine($"Selected status: {ddlStatusFilter.SelectedItem.Text}");
                query += " AND status = @Status";
                parameters.Add(new SqlParameter("@Status", ddlStatusFilter.SelectedItem.Text));
            }

            // ACTION filter
            if (filterAction.Checked && !string.IsNullOrEmpty(ddlActionFilter.SelectedItem.Text))
            {
                Debug.WriteLine($"Selected action: {ddlActionFilter.SelectedItem.Text}");
                query += " AND action = @Action";
                parameters.Add(new SqlParameter("@Action", ddlActionFilter.SelectedItem.Text));
            }

            // EXPIRY DATE filter
            if (filterExpiryDate.Checked && !string.IsNullOrWhiteSpace(txtExpiryDateFilter.Text))
            {
                DateTime expiryDate;
                if (DateTime.TryParse(txtExpiryDateFilter.Text, out expiryDate))
                {
                    Debug.WriteLine($"Selected expiry date: {expiryDate:yyyy-MM-dd}");
                    query += " AND expiryDate = @ExpiryDate";
                    parameters.Add(new SqlParameter("@ExpiryDate", expiryDate));
                }
            }

            // REGISTRATION DATE filter
            if (filterRegistrationDate.Checked && !string.IsNullOrWhiteSpace(txtRegDateFilter.Text))
            {
                DateTime regDate;
                if (DateTime.TryParseExact(
                    txtRegDateFilter.Text,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out regDate))
                {
                    Debug.WriteLine($"Selected registration date: {regDate:yyyy-MM-dd}");

                    DateTime nextDay = regDate.AddDays(1);
                    query += " AND regeDate >= @RegDate AND regeDate < @NextDay";
                    parameters.Add(new SqlParameter("@RegDate", regDate));
                    parameters.Add(new SqlParameter("@NextDay", nextDay));
                }
            }

            // STAFF filter
            if (filterStaff.Checked && !string.IsNullOrWhiteSpace(txtstaffFilter.Text))
            {
                Debug.WriteLine($"Selected staff: {txtstaffFilter.Text}");
                query += " AND staffName = @StaffNo";
                parameters.Add(new SqlParameter("@StaffNo", txtstaffFilter.Text));
            }

            // BATCH filter
            if (filterBatch.Checked && !string.IsNullOrWhiteSpace(txtBatchNoFilter.Text))
            {
                Debug.WriteLine($"Selected batch: {txtBatchNoFilter.Text}");
                query += " AND batchNo = @BatchNo";
                parameters.Add(new SqlParameter("@BatchNo", txtBatchNoFilter.Text));
            }


            DataTable dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(strcon))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
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

        private DataTable GetFilteredData()
        {
            string userRole = Session["role"] as string;
            string storeNo = GetLoggedInUserStoreName();

            string query = "SELECT * FROM itemList WHERE ";
            List<SqlParameter> parameters = new List<SqlParameter>();

            // Base filter based on role
            if (userRole == "user")
            {
                query += "status IN ('Exchange', 'No Exchange', 'No Action') and storeNo = @StoreNo";
                parameters.Add(new SqlParameter("@StoreNo", storeNo));
            }
            else if (userRole == "viewer" || userRole == "admin")
            {
                query += "status IN ('Exchange', 'No Exchange', 'No Action')";
            }
            else
            {
                query += "1=1";
            }


            // STORE filter
            if (filterStore.Checked)
            {
                var selectedStores = lstStoreFilter.Items.Cast<ListItem>()
                    .Where(li => li.Selected && li.Value != "all")
                    .Select(li => li.Value)
                    .ToList();

                Debug.WriteLine($"Selected stores: {string.Join(",", selectedStores)}");

                if (selectedStores.Count > 0)
                {
                    query += " AND storeNo IN (" + string.Join(",", selectedStores.Select((s, i) => $"@Store{i}")) + ")";
                    parameters.AddRange(selectedStores.Select((s, i) => new SqlParameter($"@Store{i}", s)));
                }
            }

            // ITEM filter
            if (filterItem.Checked && !string.IsNullOrEmpty(item.SelectedValue))
            {
                Debug.WriteLine($"Selected item: {item.SelectedValue}");
                query += " AND itemNo = @ItemNo";
                parameters.Add(new SqlParameter("@ItemNo", item.SelectedValue));
            }

            // VENDOR filter
            if (filterVendor.Checked && !string.IsNullOrEmpty(vendor.SelectedValue))
            {
                Debug.WriteLine($"Selected vendor: {vendor.SelectedValue}");
                query += " AND vendorNo = @VendorNo";
                parameters.Add(new SqlParameter("@VendorNo", vendor.SelectedValue));
            }

            // STATUS filter
            if (filterStatus.Checked && !string.IsNullOrEmpty(ddlStatusFilter.SelectedItem.Text))
            {
                Debug.WriteLine($"Selected status: {ddlStatusFilter.SelectedItem.Text}");
                query += " AND status = @Status";
                parameters.Add(new SqlParameter("@Status", ddlStatusFilter.SelectedItem.Text));
            }

            // ACTION filter
            if (filterAction.Checked && !string.IsNullOrEmpty(ddlActionFilter.SelectedItem.Text))
            {
                Debug.WriteLine($"Selected action: {ddlActionFilter.SelectedItem.Text}");
                query += " AND action = @Action";
                parameters.Add(new SqlParameter("@Action", ddlActionFilter.SelectedItem.Text));
            }

            // EXPIRY DATE filter
            if (filterExpiryDate.Checked && !string.IsNullOrWhiteSpace(txtExpiryDateFilter.Text))
            {
                DateTime expiryDate;
                if (DateTime.TryParse(txtExpiryDateFilter.Text, out expiryDate))
                {
                    Debug.WriteLine($"Selected expiry date: {expiryDate:yyyy-MM-dd}");
                    query += " AND expiryDate = @ExpiryDate";
                    parameters.Add(new SqlParameter("@ExpiryDate", expiryDate));
                }
            }

            /// REGISTRATION DATE filter
            if (filterRegistrationDate.Checked && !string.IsNullOrWhiteSpace(txtRegDateFilter.Text))
            {
                DateTime regDate;
                if (DateTime.TryParseExact(
                    txtRegDateFilter.Text,
                    "yyyy-MM-dd", // Matches TextMode="Date" format
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out regDate))
                {
                    Debug.WriteLine($"Selected registration date: {regDate:yyyy-MM-dd}");

                    // Filter by date range (ignores time)
                    DateTime nextDay = regDate.AddDays(1);
                    query += " AND regeDate >= @RegDate AND regeDate < @NextDay";
                    parameters.Add(new SqlParameter("@RegDate", regDate));
                    parameters.Add(new SqlParameter("@NextDay", nextDay));
                }
            }

            // STAFF filter
            if (filterStaff.Checked && !string.IsNullOrWhiteSpace(txtstaffFilter.Text))
            {
                Debug.WriteLine($"Selected staff: {txtstaffFilter.Text}");
                query += " AND staffName = @StaffNo";
                parameters.Add(new SqlParameter("@StaffNo", txtstaffFilter.Text));
            }

            // BATCH filter
            if (filterBatch.Checked && !string.IsNullOrWhiteSpace(txtBatchNoFilter.Text))
            {
                Debug.WriteLine($"Selected batch: {txtBatchNoFilter.Text}");
                query += " AND batchNo = @BatchNo";
                parameters.Add(new SqlParameter("@BatchNo", txtBatchNoFilter.Text));
            }

            // Debug final query
            Debug.WriteLine("Final query: " + query);

            DataTable dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(strcon))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
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

        private (string query, List<SqlParameter> parameters) GetFilteredQuery()
        {
            string userRole = Session["role"] as string;
            string storeNo = GetLoggedInUserStoreName();

            string query = "SELECT * FROM itemList WHERE ";
            List<SqlParameter> parameters = new List<SqlParameter>();

            // Base filter based on role
            if (userRole == "user")
            {
                query += "status IN ('Exchange', 'No Exchange', 'No Action') and storeNo = @StoreNo";
                parameters.Add(new SqlParameter("@StoreNo", storeNo));
            }
            else if (userRole == "viewer" || userRole == "admin")
            {
                query += "status IN ('Exchange', 'No Exchange', 'No Action')";
            }
            else
            {
                query += "1=1";
            }

            string action = GetActionText(ddlActionFilter.SelectedValue);
            string status = GetStatusText(ddlStatusFilter.SelectedValue);
            string store = string.Join("|", lstStoreFilter.Items.Cast<ListItem>()
                        .Where(li => li.Selected)
                        .Select(li => li.Value));
            string itemNo = item.SelectedValue;
            string expiryDate = txtExpiryDateFilter.Text;
            string regDate = txtRegDateFilter.Text;
            string staffName = txtstaffFilter.Text;
            string batchNo = txtBatchNoFilter.Text.Trim();
            string vendorNo = vendor.SelectedValue;

            if (!string.IsNullOrEmpty(action) && ddlActionFilter.SelectedValue != "0")
            {
                query += " AND Action = @Action";
                parameters.Add(new SqlParameter("@Action", action));
            }

            if (!string.IsNullOrEmpty(status) && ddlStatusFilter.SelectedValue != "0")
            {
                query += " AND Status = @Status";
                parameters.Add(new SqlParameter("@Status", status));
            }

            if (!string.IsNullOrEmpty(store) && !store.Contains("all"))
            {
                var stores = store.Split('|'); // Split by pipe
                query += " AND storeNo IN (";
                for (int i = 0; i < stores.Length; i++)
                {
                    if (i > 0) query += ",";
                    query += $"@Store{i}";
                    parameters.Add(new SqlParameter($"@Store{i}", stores[i]));
                }
                query += ")";
            }

            if (!string.IsNullOrEmpty(itemNo) && itemNo != "0")
            {
                query += " AND itemNo = @ItemNo";
                parameters.Add(new SqlParameter("@ItemNo", itemNo));
            }

            if (!string.IsNullOrEmpty(expiryDate))
            {
                query += " AND CONVERT(VARCHAR(10), expiryDate, 120) = @ExpiryDate";
                parameters.Add(new SqlParameter("@ExpiryDate", DateTime.ParseExact(expiryDate, "yyyy-MM", null).ToString("yyyy-MM-dd")));
            }

            if (!string.IsNullOrEmpty(staffName) && staffName != "0")
            {
                query += " AND staffName = @StaffName";
                parameters.Add(new SqlParameter("@StaffName", staffName));
            }

            if (!string.IsNullOrEmpty(batchNo))
            {
                query += " AND batchNo LIKE '%' + @BatchNo + '%'";
                parameters.Add(new SqlParameter("@BatchNo", batchNo));
            }

            if (!string.IsNullOrEmpty(vendorNo) && vendorNo != "0")
            {
                query += " AND vendorNo = @VendorNo";
                parameters.Add(new SqlParameter("@VendorNo", vendorNo));
            }

            if (!string.IsNullOrEmpty(regDate))
            {
                DateTime date = DateTime.ParseExact(regDate, "yyyy-MM-dd", null);
                query += " AND CAST(regeDate AS DATE) = @RegDate";
                parameters.Add(new SqlParameter("@RegDate", date.Date));
            }

            return (query, parameters);
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
            using (SqlConnection conn = new SqlConnection(strcon))
            {
                string userRole = Session["role"] as string;

                StringBuilder whereClause = new StringBuilder();

                if (userRole == "viewer")
                {
                    whereClause.Append("status IN ('Exchange', 'No Exchange', 'No Action')");
                }
                else
                {
                    whereClause.Append("1=1");
                }

                string searchValue = Request["search"] ?? "";

                // Define the searchable columns
                string[] searchableColumns = {
                   "no", "itemNo", "description", "barcodeNo", "qty", "uom", "packingInfo", "expiryDate", "storeNo",
                   "staffName", "batchNo", "vendorNo", "vendorName", "regeDate", "action", "status","note", "remark", "completedDate"
                };

                // Append search filter if search value is provided
                if (!string.IsNullOrEmpty(searchValue))
                {
                    whereClause.Append(" AND (");
                    foreach (string col in searchableColumns)
                    {
                        whereClause.Append($"{col} LIKE '%' + @SearchValue + '%' OR ");
                    }
                    whereClause.Remove(whereClause.Length - 4, 4);  // Remove the trailing 'OR'
                    whereClause.Append(")");
                }

                // Construct the query without pagination
                string query = $@"
            SELECT * 
            FROM itemList 
            WHERE {whereClause}
            ORDER BY id;";

                SqlCommand cmd = new SqlCommand(query, conn);

                // If a search value is provided, add it as a parameter
                if (!string.IsNullOrEmpty(searchValue))
                {
                    cmd.Parameters.AddWithValue("@SearchValue", searchValue);
                }

                conn.Open();
                DataTable dt = new DataTable();
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
                conn.Close();

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
    }
}