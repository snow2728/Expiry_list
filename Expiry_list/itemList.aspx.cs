using ClosedXML.Excel;
using DocumentFormat.OpenXml.Presentation;
using Newtonsoft.Json;
using OfficeOpenXml;
using OfficeOpenXml.Style;
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
    public partial class itemList : System.Web.UI.Page
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
                BindGrid();
                BindStores();
                PopulateItemsDropdown();
                PopulateVendorDropdown();
            }
            else
            {
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

        protected void GridView2_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            GridView2.PageIndex = e.NewPageIndex;
            BindGrid();
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

                if (length == 0) length = 100;

                string[] columns = { "id", "no", "itemNo", "description", "barcodeNo", "qty", "uom", "packingInfo", "expiryDate", "storeNo", "staffName", "batchNo", "vendorNo", "vendorName", "regeDate", "action", "status", "note", "remark", "completedDate" };

                string[] searchableColumns = {
                   "no", "itemNo", "description", "barcodeNo", "qty", "uom", "packingInfo", "expiryDate", "storeNo",
                   "staffName", "batchNo", "vendorNo", "vendorName", "regeDate", "action", "status","note", "remark", "completedDate"
                };

                string searchValue = Request["search"] ?? "";

                // Build WHERE clause with search
                StringBuilder whereClause = new StringBuilder();
                whereClause.Append("(status NOT IN ('Exchange', 'No Exchange','No Action') OR status IS NULL)");
                if (!string.IsNullOrEmpty(searchValue))
                {
                    whereClause.Append(" AND (");
                    foreach (string col in searchableColumns)
                    {
                        whereClause.Append($"{col} LIKE '%' + @SearchValue + '%' OR ");
                    }
                    whereClause.Remove(whereClause.Length - 4, 4); // Remove the last " OR "
                    whereClause.Append(")");
                }

                string orderByClause = "";
                // Add this adjustment after getting orderColumnIndex
                int serverColumnIndex = orderColumnIndex - 1; // Subtract client-only columns (checkbox + Num)
                if (serverColumnIndex >= 0 && serverColumnIndex < columns.Length)
                {
                    string orderBy = columns[serverColumnIndex];
                    orderByClause = $" ORDER BY {orderBy} {orderDir}";
                }
                else
                {
                    orderByClause = " ORDER BY id ASC";
                }

                string query = $@"
           SELECT * 
           FROM itemList 
           WHERE {whereClause}
           {orderByClause}
           OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Offset", start);
                cmd.Parameters.AddWithValue("@SearchValue", searchValue);
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

                SqlCommand countCmd = new SqlCommand(countQuery, conn);
                countCmd.Parameters.AddWithValue("@SearchValue", searchValue);

                conn.Open();
                int totalRecords = (int)countCmd.ExecuteScalar();
                conn.Close();

                var data = dt.AsEnumerable().Select(row => new
                {
                    id = row["id"],
                    checkbox = "", // Empty for checkbox column
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
                    completedDate = row["completedDate"],
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

                // Return JSON response
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

        protected void GridView2_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            string editedId = GridView2.DataKeys[e.RowIndex].Value.ToString();
            hfSelectedIDs.Value = editedId;

            GridViewRow row = GridView2.Rows[e.RowIndex];
            string itemId = Convert.ToString(GridView2.DataKeys[e.RowIndex].Value);

            DropDownList ddlAction = (DropDownList)row.FindControl("ddlActionEdit");
            string selectedAction = GetActionText(ddlAction.SelectedValue);
            DropDownList ddlStatus = (DropDownList)row.FindControl("ddlStatusEdit");
            string selectedStatus = GetStatusText(ddlStatus.SelectedValue);
            TextBox remark = (TextBox)row.FindControl("txtRemark");

            TextBox completed = (TextBox)row.FindControl("txtCompleted");

            if (ddlAction == null || ddlStatus == null || remark == null || completed == null)
            {
                ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
                     "swal('Error!', 'Null Update Value!', 'error');", true);
                return;
            }

            System.Diagnostics.Debug.WriteLine($"Action: {ddlAction.SelectedValue}, Status: {ddlStatus.SelectedValue}, Remark: {remark.Text}");

           

            using (SqlConnection conn = new SqlConnection(strcon))
            {
                string query = "UPDATE itemList SET Action = @action, Status = @status, Remark = @remark, completedDate = @completedDate WHERE id = @itemId";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@action", selectedAction);
                cmd.Parameters.AddWithValue("@status", selectedStatus);
                cmd.Parameters.AddWithValue("@remark", remark.Text);

                if (!string.IsNullOrEmpty(completed.Text))
                {
                    cmd.Parameters.AddWithValue("@completedDate", completed.Text);
                }
                else
                {
                    cmd.Parameters.AddWithValue("@completedDate", DBNull.Value);
                }

                cmd.Parameters.AddWithValue("@itemId", editedId);

                conn.Open();
                cmd.ExecuteNonQuery();
                conn.Close();
            }
            ClientScript.RegisterStartupScript(this.GetType(), "success",
                    "swal('Success!', 'Update completed!', 'success');", true);

            GridView2.EditIndex = -1;
            BindGrid();
            Response.Redirect("itemList.aspx");
        }

        protected void GridView2_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            GridView2.EditIndex = -1;
            BindGrid();
            Response.Redirect("itemList.aspx");
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

                DataTable dt = GetFilteredData();

                ViewState["FilteredData"] = dt;

                // Bind GridView
                GridView2.DataSource = dt;
                GridView2.DataBind();

                //BindGrid();

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

        protected void GridView2_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow && GridView2.EditIndex == e.Row.RowIndex)
            {
                DataRowView rowView = (DataRowView)e.Row.DataItem;

                // Handle Action dropdown
                DropDownList ddlAction = (DropDownList)e.Row.FindControl("ddlActionEdit");
                if (ddlAction != null)
                {
                    string currentAction = rowView["action"].ToString();
                    ddlAction.SelectedValue = GetActionText(currentAction);
                }

                // Handle Status dropdown
                DropDownList ddlStatus = (DropDownList)e.Row.FindControl("ddlStatusEdit");
                if (ddlStatus != null)
                {
                    string currentStatus = rowView["status"].ToString();
                    ddlStatus.SelectedValue = GetStatusText(currentStatus);
                }
            }
        }

        protected void btnFilter_Click1(object sender, EventArgs e)
        {
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
     "window.location.href = 'itemList.aspx';", true);

            // Optional (if you want to refresh filter UI visibility)
            ScriptManager.RegisterStartupScript(this, GetType(), "ResetFilters",
                @"if (typeof(updateFilterVisibility) === 'function') { 
            updateFilterVisibility(); 
            toggleFilter(false); 
        }", true);
        }

        private void BindGrid(int pageNumber = 1, int pageSize = 100)
        {
            using (SqlConnection conn = new SqlConnection(strcon))
            {
                string orderBy = string.IsNullOrEmpty(hfSelectedIDs.Value)
                    ? "ORDER BY id"
                    : $"ORDER BY CASE WHEN id = '{hfSelectedIDs.Value}' THEN 0 ELSE 1 END, id";

                string query = $@"
            SELECT * 
            FROM itemList 
            WHERE (status IN ('Exchange', 'No Exchange', 'No Action') OR status IS NULL)
            {orderBy}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Offset", (pageNumber - 1) * pageSize);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);

                conn.Open();
                DataTable dt = new DataTable();
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
                conn.Close();

                GridView2.DataSource = dt;
                GridView2.PageIndex = pageNumber - 1; 
                GridView2.DataBind();
            }
        }

        protected void btnUpdateSelected_Click(object sender, EventArgs e)

        {
            try
            {
                string selectedAction = GetActionText(ddlAction.SelectedValue);

                string selectedIDs = hfSelectedIDs.Value;
                if (string.IsNullOrEmpty(selectedIDs))
                {
                    ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
                        "swal('Error!', 'Please select at least one record!', 'error');", true);
                    return;
                }

                string[] ids = selectedIDs.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                using (SqlConnection conn = new SqlConnection(strcon))
                {
                    conn.Open();
                    foreach (string idStr in ids)
                    {
                        int id = Convert.ToInt32(idStr);
                        ;

                        if ((string.IsNullOrEmpty(selectedAction) || selectedAction == "0"))
                        {
                            string updateQuery = "UPDATE itemList SET Action = @Action  WHERE id = @id";
                            using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                            {
                                cmd.Parameters.AddWithValue("@Action", "");
                                cmd.Parameters.AddWithValue("@id", id);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        else if (!string.IsNullOrEmpty(selectedAction) && selectedAction != "0")
                        {
                            string updateQuery = "UPDATE itemList SET Action = @Action  WHERE id = @id";
                            using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                            {
                                cmd.Parameters.AddWithValue("@Action", selectedAction);
                                cmd.Parameters.AddWithValue("@id", id);
                                cmd.ExecuteNonQuery();
                            }
                        }

                    }
                    conn.Close();
                    hfSelectedIDs.Value = string.Empty;

                    BindGrid();
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "alertRedirect",
                        "swal('Success!', 'Selected records have been updated successfully!', 'success').then(function() { window.location = 'itemList.aspx'; });", true);

                }

            }
            catch (Exception ex)
            {
                ScriptManager.RegisterStartupScript(this, GetType(), "alert", $"alert('Error: {ex.Message}');", true);
                
            }

        }

        protected void btnStatusSelected_Click(object sender, EventArgs e)
        {
            try
            {
                string selectedStatus = GetStatusText(ddlStatus.SelectedValue);

                string selectedIDs = hfSelectedIDs.Value;
                if (string.IsNullOrEmpty(selectedIDs))
                {
                    ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
                        "swal('Error!', 'Please select at least one record!', 'error');", true);
                    return;
                }

                string[] ids = selectedIDs.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                using (SqlConnection conn = new SqlConnection(strcon))
                {
                    conn.Open();
                    foreach (string idStr in ids)
                    {
                        int id = Convert.ToInt32(idStr);
                        ;

                        if ((string.IsNullOrEmpty(selectedStatus) || selectedStatus == "0"))
                        {
                            string updateQuery = "UPDATE itemList SET Status = @Status WHERE id = @id ";
                            using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                            {
                                cmd.Parameters.AddWithValue("@Status", "");
                                cmd.Parameters.AddWithValue("@id", id);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        else if (!string.IsNullOrEmpty(selectedStatus) && selectedStatus != "0")
                        {
                            string updateQuery = "UPDATE itemList SET Status = @Status WHERE id = @id ";
                            using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                            {
                                cmd.Parameters.AddWithValue("@Status", selectedStatus);
                                cmd.Parameters.AddWithValue("@id", id);
                                cmd.ExecuteNonQuery();
                            }
                        }

                    }
                    conn.Close();
                    hfSelectedIDs.Value = string.Empty;

                    BindGrid();
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "alertRedirect",
                        "swal('Success!', 'Selected records have been updated successfully!', 'success').then(function() { window.location = 'itemList.aspx'; });", true);

                }

            }
            catch (Exception ex)
            {
                ScriptManager.RegisterStartupScript(this, GetType(), "alert", $"alert('Error: {ex.Message}');", true);

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

        //protected void btnExport_Click(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        DataTable dt = GetFilteredData2();

        //        if (dt != null && dt.Rows.Count > 0)
        //        {
        //            ExportToExcel2(dt);
        //        }
        //        else
        //        {
        //            ScriptManager.RegisterStartupScript(this, GetType(), "noDataAlert",
        //                "alert('No data to export.');", true);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine($"Export error: {ex}");
        //        ScriptManager.RegisterStartupScript(this, GetType(), "exportError",
        //            "alert('Error exporting data. Please try again.');", true);
        //    }
        //}

        //private DataTable GetFilteredData2()
        //{
        //    string query = @" SELECT 
        //        no, itemNo, description, barcodeNo,qty, uom, packingInfo, CONVERT(DATE, expiryDate) AS expiryDate,
        //        storeNo, staffName, batchNo,vendorNo, vendorName, CONVERT(DATE, regeDate) AS regeDate,
        //        action, status,note,remark, CONVERT(DATE, completedDate) AS completedDate FROM itemList 
        //        WHERE (status NOT IN ('Exchange', 'No Exchange', 'No Action') OR status IS NULL)";

        //    List<SqlParameter> parameters = new List<SqlParameter>();

        //    // STORE filter
        //    if (filterStore.Checked)
        //    {
        //        var selectedStores = lstStoreFilter.Items.Cast<ListItem>()
        //            .Where(li => li.Selected && li.Value != "all")
        //            .Select(li => li.Value)
        //            .ToList();

        //        Debug.WriteLine($"Selected stores: {string.Join(",", selectedStores)}");

        //        if (selectedStores.Count > 0)
        //        {
        //            query += " AND storeNo IN (" + string.Join(",", selectedStores.Select((s, i) => $"@Store{i}")) + ")";
        //            parameters.AddRange(selectedStores.Select((s, i) => new SqlParameter($"@Store{i}", s)));
        //        }
        //    }

        //    // ITEM filter
        //    if (filterItem.Checked && !string.IsNullOrEmpty(item.SelectedValue))
        //    {
        //        Debug.WriteLine($"Selected item: {item.SelectedValue}");
        //        query += " AND itemNo = @ItemNo";
        //        parameters.Add(new SqlParameter("@ItemNo", item.SelectedValue));
        //    }

        //    // VENDOR filter
        //    if (filterVendor.Checked && !string.IsNullOrEmpty(vendor.SelectedValue))
        //    {
        //        Debug.WriteLine($"Selected vendor: {vendor.SelectedValue}");
        //        query += " AND vendorNo = @VendorNo";
        //        parameters.Add(new SqlParameter("@VendorNo", vendor.SelectedValue));
        //    }

        //    // STATUS filter
        //    if (filterStatus.Checked && !string.IsNullOrEmpty(ddlStatusFilter.SelectedValue))
        //    {
        //        Debug.WriteLine($"Selected status: {ddlStatusFilter.SelectedValue}");
        //        query += " AND status = @Status";
        //        parameters.Add(new SqlParameter("@Status", ddlStatusFilter.SelectedValue));
        //    }

        //    // ACTION filter
        //    if (filterAction.Checked && !string.IsNullOrEmpty(ddlActionFilter.SelectedValue))
        //    {
        //        Debug.WriteLine($"Selected action: {ddlActionFilter.SelectedValue}");
        //        query += " AND action = @Action";
        //        parameters.Add(new SqlParameter("@Action", ddlActionFilter.SelectedValue));
        //    }

        //    // EXPIRY DATE filter
        //    if (filterExpiryDate.Checked && !string.IsNullOrWhiteSpace(txtExpiryDateFilter.Text))
        //    {
        //        DateTime expiryDate;
        //        if (DateTime.TryParse(txtExpiryDateFilter.Text, out expiryDate))
        //        {
        //            Debug.WriteLine($"Selected expiry date: {expiryDate:yyyy-MM-dd}");
        //            query += " AND expiryDate = @ExpiryDate";
        //            parameters.Add(new SqlParameter("@ExpiryDate", expiryDate));
        //        }
        //    }

        //    // REGISTRATION DATE filter
        //    if (filterRegistrationDate.Checked && !string.IsNullOrWhiteSpace(txtRegDateFilter.Text))
        //    {
        //        DateTime regDate;
        //        if (DateTime.TryParseExact(
        //            txtRegDateFilter.Text,
        //            "yyyy-MM-dd",
        //            CultureInfo.InvariantCulture,
        //            DateTimeStyles.None,
        //            out regDate))
        //        {
        //            Debug.WriteLine($"Selected registration date: {regDate:yyyy-MM-dd}");

        //            // Filter by date range (ignores time)
        //            DateTime nextDay = regDate.AddDays(1);
        //            query += " AND regeDate >= @RegDate AND regeDate < @NextDay";
        //            parameters.Add(new SqlParameter("@RegDate", regDate));
        //            parameters.Add(new SqlParameter("@NextDay", nextDay));
        //        }
        //    }

        //    // STAFF filter
        //    if (filterStaff.Checked && !string.IsNullOrWhiteSpace(txtstaffFilter.Text))
        //    {
        //        Debug.WriteLine($"Selected staff: {txtstaffFilter.Text}");
        //        query += " AND staffName = @StaffNo";
        //        parameters.Add(new SqlParameter("@StaffNo", txtstaffFilter.Text));
        //    }

        //    // BATCH filter
        //    if (filterBatch.Checked && !string.IsNullOrWhiteSpace(txtBatchNoFilter.Text))
        //    {
        //        Debug.WriteLine($"Selected batch: {txtBatchNoFilter.Text}");
        //        query += " AND batchNo = @BatchNo";
        //        parameters.Add(new SqlParameter("@BatchNo", txtBatchNoFilter.Text));
        //    }


        //    DataTable dt = new DataTable();

        //    using (SqlConnection conn = new SqlConnection(strcon))
        //    {
        //        using (SqlCommand cmd = new SqlCommand(query, conn))
        //        {
        //            cmd.Parameters.AddRange(parameters.ToArray());

        //            try
        //            {
        //                conn.Open();
        //                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
        //                {
        //                    da.Fill(dt);
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                Debug.WriteLine("Database error: " + ex.ToString());
        //                throw;
        //            }
        //        }
        //    }

        //    return dt;
        //}

        private DataTable GetFilteredData()
        {
            string query = @"SELECT * FROM itemList 
                WHERE (status NOT IN ('Exchange', 'No Exchange', 'No Action') 
                OR status IS NULL)";
            List<SqlParameter> parameters = new List<SqlParameter>();

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

            if (filterExpiryDate.Checked && !string.IsNullOrWhiteSpace(txtExpiryDateFilter.Text))
            {
                List<string> validMonthList = new List<string>();
                string[] parts = txtExpiryDateFilter.Text.Split('|');

                foreach (var part in parts)
                {
                    // Convert "May.2025" => DateTime => "yyyy-MM"
                    if (DateTime.TryParseExact(part.Trim(), "MMM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
                    {
                        validMonthList.Add(parsedDate.ToString("yyyy-MM"));
                    }
                }

                if (validMonthList.Count > 0)
                {
                    string monthConditions = string.Join(" OR ", validMonthList.Select((m, i) => $"FORMAT(expiryDate, 'yyyy-MM') = @ExpiryMonth{i}"));
                    query += $" AND ({monthConditions})";

                    for (int i = 0; i < validMonthList.Count; i++)
                    {
                        parameters.Add(new SqlParameter($"@ExpiryMonth{i}", validMonthList[i]));
                    }
                }
            }

            // REGISTRATION DATE filter
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

        private void ExportToExcel(DataTable dt)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            Response.Clear();
            Response.Buffer = true;

            using (ExcelPackage pck = new ExcelPackage())
            {
                ExcelWorksheet ws = pck.Workbook.Worksheets.Add("ExpiryList");

                // Load data without table style to maintain formatting control
                ws.Cells["A1"].LoadFromDataTable(dt, true);

                // Format date columns (adjust "expiryDate" to your column name)
                if (dt.Columns.Contains("expiryDate"))
                {
                    int dateColIndex = dt.Columns["expiryDate"].Ordinal + 1; // +1 for Excel's 1-based index
                    using (ExcelRange dateColumn = ws.Cells[2, dateColIndex, dt.Rows.Count + 1, dateColIndex])
                    {
                        dateColumn.Style.Numberformat.Format = "mmm_yyyy";
                        dateColumn.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }
                }

                // Auto-fit columns after formatting
                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                using (ExcelRange header = ws.Cells[1, 1, 1, dt.Columns.Count])
                {
                    header.Style.Font.Bold = true;
                    header.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    header.Style.Fill.BackgroundColor.SetColor(color:System.Drawing.Color.LightGray);
                }

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
                conn.Open();
                DataTable dt = new DataTable();

                string query = $@"
                    SELECT 
                        no,
                        itemNo,
                        description,
                        barcodeNo,qty, uom,	packingInfo,  CONVERT(DATE, expiryDate) AS expiryDate,
                        storeNo,
                        staffName,
                        batchNo,
                        vendorNo,
                        vendorName, CONVERT(DATE, regeDate) AS regeDate,
                        action,
                        status,note,remark, CONVERT(DATE, completedDate) AS completedDate
                    FROM itemList 
                    WHERE {(BuildWhereClause())}
                    ORDER BY id";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddRange(GetParameters().ToArray());

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }

                // Ensure date column is properly typed
                if (dt.Columns.Contains("expiryDate"))
                {
                    dt.Columns["expiryDate"].DateTimeMode = DataSetDateTime.Unspecified;
                }

                return dt;
            }
        }

        private string BuildWhereClause()
        {
            StringBuilder whereClause = new StringBuilder();
            whereClause.Append("(status NOT IN ('Exchange', 'No Exchange','No Action') OR status IS NULL)");

            if (!string.IsNullOrEmpty(Request["search"]))
            {
                whereClause.Append(" AND (");
                string[] searchableColumns = {"no", "itemNo", "description", "barcodeNo", "qty", "uom", "packingInfo", "expiryDate", "storeNo",
                   "staffName", "batchNo", "vendorNo", "vendorName", "regeDate", "action", "status","note", "remark", "completedDate"};

                foreach (string col in searchableColumns)
                {
                    whereClause.Append($"{col} LIKE '%' + @SearchValue + '%' OR ");
                }
                whereClause.Length -= 4;
                whereClause.Append(")");
            }

            return whereClause.ToString();
        }

        private List<SqlParameter> GetParameters()
        {
            List<SqlParameter> parameters = new List<SqlParameter>();

            if (!string.IsNullOrEmpty(Request["search"]))
            {
                parameters.Add(new SqlParameter("@SearchValue", SqlDbType.NVarChar)
                {
                    Value = $"%{Request["search"]}%"
                });
            }

            return parameters;
        }

        protected void btnEdit_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(hfSelectedIDs.Value))
            {
                string[] selectedIds = hfSelectedIDs.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                if (selectedIds.Length > 1)
                {
                    // Show alert if more than one row is selected
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "alertMultiple", "swal('Warning!', 'Please select only one row to edit.', 'warning');", true);
                    return;
                }

                string selectedId = selectedIds.FirstOrDefault();
                BindGrid();

                for (int i = 0; i < GridView2.Rows.Count; i++)
                {
                    var rowId = GridView2.DataKeys[i].Value.ToString();
                    if (rowId == selectedId)
                    {
                        GridView2.EditIndex = i;
                        break;
                    }
                }
                BindGrid();
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

        protected void btnExport_Click(object sender, EventArgs e)
        {
            try
            {
                // Configure response for streaming large files
                Response.ContentEncoding = Encoding.UTF8;
                Response.HeaderEncoding = Encoding.UTF8;
                Response.Clear();
                Response.Buffer = false;
                Response.AddHeader("content-disposition", "attachment;filename=ExpiryList.xls");
                Response.Charset = "UTF-8";
                Response.ContentType = "application/vnd.ms-excel; charset=utf-8";

                // Start HTML structure
                Response.Write("<html xmlns:o=\"urn:schemas-microsoft-com:office:office\" xmlns:x=\"urn:schemas-microsoft-com:office:excel\">");
                Response.Write("<head><meta charset=\"UTF-8\"></head><body>");
                Response.Write("<table border='1' cellpadding='3' cellspacing='0' style='font-family:Padauk,Myanmar Text,sans-serif;'>");

                // Write headers
                Response.Write("<tr style='background-color:#E0E0E0;'>");
                string[] headers = { "no", "itemNo", "description", "barcodeNo", "qty", "uom", "packingInfo",
                           "expiryDate", "storeNo", "staffName", "batchNo", "vendorNo", "vendorName",
                           "regeDate", "action", "status", "note", "remark", "completedDate" };
                foreach (string header in headers)
                {
                    Response.Write($"<th>{HttpUtility.HtmlEncode(header)}</th>");
                }
                Response.Write("</tr>");
                Response.Flush();

                // Build query and parameters
                string query = BuildBaseQuery(out List<SqlParameter> parameters);
                bool hasData = false;

                using (SqlConnection conn = new SqlConnection(strcon))
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandTimeout = 600; // Increase timeout for large exports
                    cmd.Parameters.AddRange(parameters.ToArray());
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        int rowCounter = 0; // Track rows for flushing

                        // Stream data row by row
                        while (reader.Read())
                        {
                            hasData = true;
                            rowCounter++;

                            Response.Write("<tr>");

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                Response.Write("<td style='mso-number-format:\\@;'>");

                                if (reader.IsDBNull(i))
                                {
                                    Response.Write("");
                                }
                                else
                                {
                                    object value = reader.GetValue(i);

                                    // Format dates specifically
                                    if (value is DateTime)
                                    {
                                        Response.Write(((DateTime)value).ToString("yyyy-MM-dd"));
                                    }
                                    else
                                    {
                                        Response.Write(HttpUtility.HtmlEncode(value.ToString()));
                                    }
                                }

                                Response.Write("</td>");
                            }
                            Response.Write("</tr>");

                            // Flush every 100 rows to manage memory
                            if (rowCounter % 100 == 0)
                            {
                                Response.Flush();
                            }
                        }
                    }
                }

                if (!hasData)
                {
                    Response.End();
                    ScriptManager.RegisterStartupScript(this, GetType(), "noDataAlert",
                        "alert('No data to export.');", true);
                    return;
                }

                // Finalize HTML
                Response.Write("</table></body></html>");
                Response.Flush();
                Response.End();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Export error: {ex}");
                ScriptManager.RegisterStartupScript(this, GetType(), "exportError",
                    "alert('Error exporting data. Please try again.');", true);
            }
        }

        private string BuildBaseQuery(out List<SqlParameter> parameters)
        {
            parameters = new List<SqlParameter>();
            string query = @"SELECT 
        no, itemNo, description, barcodeNo, qty, uom, packingInfo, 
        CONVERT(DATE, expiryDate) AS expiryDate,
        storeNo, staffName, batchNo, vendorNo, vendorName, 
        CONVERT(DATE, regeDate) AS regeDate,
        action, status, note, remark, 
        CONVERT(DATE, completedDate) AS completedDate 
        FROM itemList 
        WHERE (status NOT IN ('Exchange', 'No Exchange', 'No Action') OR status IS NULL)";

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
                    // Create parameterized IN clause
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
                Debug.WriteLine($"Selected status: {ddlStatusFilter.SelectedValue}");
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

            return query;
        }

    }
}

