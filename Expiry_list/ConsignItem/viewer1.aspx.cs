using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json;
using OfficeOpenXml.Style;
using OfficeOpenXml;

namespace Expiry_list.ConsignItem
{
    public partial class viewer1 : System.Web.UI.Page
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

                List<string> storeList = Session["storeListRaw"] as List<string>;
                bool showAllStores = storeList != null && storeList.Contains("HO", StringComparer.OrdinalIgnoreCase);

                string[] columns = { "id", "no", "itemNo", "description", "barcodeNo", "qty", "uom", "packingInfo", "storeNo", "staffName", "vendorNo", "vendorName", "regeDate", "status", "note", "completedDate" };
                string[] searchableColumns = columns.Skip(1).ToArray();
                string searchValue = Request["search"] ?? "";

                // WHERE clause logic
                List<string> filters = new List<string>
                {
                    "(status is Null or status<>'Confirm' or status='')"
                };

                List<SqlParameter> parameters = new List<SqlParameter>();

                // Search clause
                if (!string.IsNullOrEmpty(searchValue))
                {
                    List<string> searchParts = searchableColumns.Select(col => $"{col} LIKE @SearchValue").ToList();
                    filters.Add("(" + string.Join(" OR ", searchParts) + ")");
                    parameters.Add(new SqlParameter("@SearchValue", $"%{searchValue}%"));
                }

                // Store filter
                if (!showAllStores && storeList != null && storeList.Count > 0)
                {
                    List<string> storeFilters = new List<string>();
                    for (int i = 0; i < storeList.Count; i++)
                    {
                        string paramName = $"@store{i}";
                        storeFilters.Add($"storeNo = {paramName}");
                        parameters.Add(new SqlParameter(paramName, storeList[i]));
                    }
                    filters.Add("(" + string.Join(" OR ", storeFilters) + ")");
                }

                string whereClause = filters.Count > 0 ? "WHERE " + string.Join(" AND ", filters) : "";

                int serverColumnIndex = orderColumnIndex - 1;
                string orderByClause = (serverColumnIndex >= 0 && serverColumnIndex < columns.Length)
                    ? $"ORDER BY {columns[serverColumnIndex]} {orderDir}"
                    : "ORDER BY id ASC";

                string query = $@"
                    SELECT *
                    FROM itemListC
                    {whereClause}
                    {orderByClause}
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Offset", start);
                cmd.Parameters.AddWithValue("@PageSize", length);
                parameters.ForEach(p => cmd.Parameters.AddWithValue(p.ParameterName, p.Value));

                conn.Open();
                DataTable dt = new DataTable();
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
                conn.Close();

                // Count query
                string countQuery = $"SELECT COUNT(*) FROM itemListC {whereClause}";
                SqlCommand countCmd = new SqlCommand(countQuery, conn);
                parameters.ForEach(p => countCmd.Parameters.AddWithValue(p.ParameterName, p.Value));

                conn.Open();
                int totalRecords = (int)countCmd.ExecuteScalar();
                conn.Close();

                var data = dt.AsEnumerable().Select(row => new
                {
                    id = row["id"],
                    checkbox = "",
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

            DropDownList ddlStatus = (DropDownList)row.FindControl("ddlStatusEdit");
            string selectedStatus = GetStatusText(ddlStatus.SelectedValue);

            if ( ddlStatus == null )
            {
                ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
                     "swal('Error!', 'Null Update Value!', 'error');", true);
                return;
            }

            using (SqlConnection conn = new SqlConnection(strcon))
            {
                string query = "UPDATE itemListC SET Status = @status, completedDate = @completedDate WHERE id = @itemId";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@status", selectedStatus);

                cmd.Parameters.AddWithValue("@completedDate", DateTime.Now.Date);

                cmd.Parameters.AddWithValue("@itemId", editedId);

                conn.Open();
                cmd.ExecuteNonQuery();
                conn.Close();
            }
            ClientScript.RegisterStartupScript(this.GetType(), "success",
                    "swal('Success!', 'Update completed!', 'success');", true);

            GridView2.EditIndex = -1;
            BindGrid();
            Response.Redirect("viewer1.aspx");
        }

        protected void GridView2_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            GridView2.EditIndex = -1;
            BindGrid();
            Response.Redirect("viewer1.aspx");
        }

        protected string GetStatusText(string selectedAction)
        {
            switch (selectedAction)
            {
                case "0":
                    return "";
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

                ViewState["FilterCompletedDateChecked"] = filterCompletedDate.Checked;
                ViewState["SelectedCompletedDate"] = txtComDateFilter.Text;

                ViewState["FilterRegDateChecked"] = filterRegistrationDate.Checked;
                ViewState["SelectedRegDate"] = txtRegDateFilter.Text;

                ViewState["FilterStaffChecked"] = filterStaff.Checked;
                ViewState["SelectedStaff"] = txtstaffFilter.Text;

                DataTable dt = GetFilteredData();

                ViewState["FilteredData"] = dt;

                // Bind GridView
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

        protected void GridView2_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow && GridView2.EditIndex == e.Row.RowIndex)
            {
                DataRowView rowView = (DataRowView)e.Row.DataItem;

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
            ddlStatusFilter.SelectedIndex = 0;
            lstStoreFilter.ClearSelection();
            item.SelectedIndex = 0;
            txtstaffFilter.Text = string.Empty;
            vendor.SelectedIndex = 0;
            txtComDateFilter.Text = string.Empty;
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
            ViewState["FilterRegDateChecked"] = null;
            ViewState["SelectedRegDate"] = null;
            ViewState["FilterStaffChecked"] = null;
            ViewState["SelectedStaff"] = null;

            ScriptManager.RegisterStartupScript(this, GetType(), "redirect",
                "window.location.href = 'viewer1.aspx';", true);

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
                List<string> storeList = Session["storeListRaw"] as List<string>;

                string orderBy = string.IsNullOrEmpty(hfSelectedIDs.Value)
                    ? "ORDER BY id"
                    : $"ORDER BY CASE WHEN id = '{hfSelectedIDs.Value}' THEN 0 ELSE 1 END, id";

                StringBuilder whereClause = new StringBuilder();
                whereClause.Append("(status IS NULL or status<>'Confirm' or status='')");

                bool showAllStores = storeList != null &&
                                     storeList.Contains("HO", StringComparer.OrdinalIgnoreCase);

                List<string> storeConditions = new List<string>();

                if (!showAllStores && storeList != null && storeList.Count > 0)
                {
                    for (int i = 0; i < storeList.Count; i++)
                    {
                        storeConditions.Add($"storeNo = @store{i}");
                    }

                    whereClause.Append(" AND (" + string.Join(" OR ", storeConditions) + ")");
                }

                string query = $@"
                    SELECT * 
                    FROM itemListC 
                    WHERE {whereClause}
                    {orderBy}
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Offset", (pageNumber - 1) * pageSize);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);

                if (!showAllStores && storeList != null)
                {
                    for (int i = 0; i < storeList.Count; i++)
                    {
                        cmd.Parameters.AddWithValue($"@store{i}", storeList[i]);
                    }
                }

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
                            string updateQuery = "UPDATE itemListC SET Status = @Status WHERE id = @id ";
                            using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                            {
                                cmd.Parameters.AddWithValue("@Status", "");
                                cmd.Parameters.AddWithValue("@id", id);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        else if (!string.IsNullOrEmpty(selectedStatus) && selectedStatus != "0")
                        {
                            string updateQuery = "UPDATE itemListC SET Status = @Status, completedDate = @completedDate WHERE id = @id ";
                            using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                            {
                                cmd.Parameters.AddWithValue("@Status", selectedStatus);
                                cmd.Parameters.AddWithValue("@completedDate", DateTime.Now.Date);
                                cmd.Parameters.AddWithValue("@id", id);
                                cmd.ExecuteNonQuery();
                            }
                        }

                    }
                    conn.Close();
                    hfSelectedIDs.Value = string.Empty;

                    BindGrid();
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "alertRedirect",
                        "swal('Success!', 'Selected records have been updated successfully!', 'success').then(function() { window.location = 'viewer1.aspx'; });", true);

                }

            }
            catch (Exception ex)
            {
                ScriptManager.RegisterStartupScript(this, GetType(), "alert", $"alert('Error: {ex.Message}');", true);

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
                query.Append(" AND (status is Null or status<>'Confirm' or status='')");
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
                    query.Append(" AND 1=0");
                }
            }
            else if (!hasHO)
            {
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
            if (filterCompletedDate.Checked && !string.IsNullOrWhiteSpace(txtComDateFilter.Text))
            {
                if (DateTime.TryParseExact(txtComDateFilter.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime comDate))
                {
                    DateTime nextDay = comDate.AddDays(1);
                    query.Append(" AND completedDate >= @RegDate AND completedDate < @NextDay");
                    parameters.Add(new SqlParameter("@RegDate", comDate));
                    parameters.Add(new SqlParameter("@NextDay", nextDay));
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
                ExcelWorksheet ws = pck.Workbook.Worksheets.Add("ConsignmentItem");

                ws.Cells["A1"].LoadFromDataTable(dt, true);

                // Auto-fit columns after formatting
                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                using (ExcelRange header = ws.Cells[1, 1, 1, dt.Columns.Count])
                {
                    header.Style.Font.Bold = true;
                    header.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    header.Style.Fill.BackgroundColor.SetColor(color: System.Drawing.Color.LightGray);
                }

                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", "attachment; filename=ConsignmentItemList.xlsx");
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
                    query.Append(" AND status is Null or status<>'Confirm' or status='' ");
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

                return dt;
            }
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
            else
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), "alertMultiple", "swal('Warning!', 'Please select only one row to edit.', 'warning');", true);
                return;
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

        private void ShowAlert(string title, string message, string type)
        {
            ScriptManager.RegisterStartupScript(this, GetType(), "alert",
                $"swal('{title}', '{HttpUtility.JavaScriptStringEncode(message)}', '{type}');", true);
        }

    }
}