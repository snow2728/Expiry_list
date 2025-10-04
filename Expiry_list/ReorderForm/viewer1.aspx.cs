using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json;
using OfficeOpenXml.Style;
using OfficeOpenXml;

namespace Expiry_list.ReorderForm
{
    public partial class viewer1 : System.Web.UI.Page
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

            if (!IsPostBack && !string.IsNullOrEmpty(hfLastSearch.Value))
            {
                ScriptManager.RegisterStartupScript(this, GetType(), "HighlightSearched",
                    $"applyManualSearchHighlighting('{hfLastSearch.Value}');", true);
            }

            if (!IsPostBack)
            {
                BindGrid();
                BindStores();
                PopulateItemsDropdown();
                PopulateVendorDropdown();
                hfLastSearch.Value = "";
            }
            else
            {
                if (ViewState["FilterStoreChecked"] != null)
                    filterStore.Checked = (bool)ViewState["FilterStoreChecked"];

                if (ViewState["SelectedStores"] != null)
                {
                    var selectedStores = ViewState["SelectedStores"].ToString().Split(',');
                    foreach (ListItem item in lstStoreFilter.Items)
                        item.Selected = selectedStores.Contains(item.Value);
                }

                if (ViewState["FilterItemChecked"] != null)
                    filterItem.Checked = (bool)ViewState["FilterItemChecked"];

                if (ViewState["SelectedItem"] != null)
                    item.SelectedValue = ViewState["SelectedItem"].ToString();

                if (ViewState["IsFiltered"] != null)
                    hfIsFiltered.Value = ViewState["IsFiltered"].ToString();

                if (!string.IsNullOrEmpty(Request.Form[hfLastSearch.UniqueID]))
                {
                    hfLastSearch.Value = Request.Form[hfLastSearch.UniqueID];
                }

                ViewState["LastSearch"] = hfLastSearch.Value;
            }
        }

        [System.Web.Services.WebMethod]
        public static string UpdateCell(int id, string column, string value)
        {
            var allowedColumns = new HashSet<string> { "action", "status", "remark" };
            if (!allowedColumns.Contains(column))
                throw new Exception("Invalid column name");

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["con"].ConnectionString))
            {
                conn.Open();

                string sql;
                SqlCommand cmd;

                if (column == "action" || column == "status")
                {
                    sql = $"UPDATE itemListR SET {column} = @value, completedDate = @completedDate, owner = @owner WHERE ID = @id";
                    cmd = new SqlCommand(sql, conn);

                    cmd.Parameters.AddWithValue("@value", string.IsNullOrEmpty(value) ? (object)DBNull.Value : value);
                    cmd.Parameters.AddWithValue("@id", id);

                    string username = HttpContext.Current.User.Identity.Name;
                    if (!string.IsNullOrEmpty(value))
                    {
                        cmd.Parameters.AddWithValue("@completedDate", DateTime.Now.Date);
                        cmd.Parameters.AddWithValue("@owner", username);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@completedDate", DBNull.Value);
                        cmd.Parameters.AddWithValue("@owner", DBNull.Value);
                    }
                }
                else
                {
                    sql = $"UPDATE itemListR SET {column} = @value WHERE ID = @id";
                    cmd = new SqlCommand(sql, conn);

                    cmd.Parameters.AddWithValue("@value", string.IsNullOrEmpty(value) ? (object)DBNull.Value : value);
                    cmd.Parameters.AddWithValue("@id", id);
                }

                cmd.ExecuteNonQuery();
            }

            return "OK";
        }

        protected void GridView2_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            GridView2.PageIndex = e.NewPageIndex;
            BindGrid();
        }

        private void RespondWithData()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(strcon))
                {
                    // Get DataTables parameters
                    int draw = Convert.ToInt32(Request["draw"]);
                    int start = Convert.ToInt32(Request["start"]);
                    int length = Convert.ToInt32(Request["length"]);
                    int orderColumnIndex = Convert.ToInt32(Request["order[0][column]"]);
                    string orderDir = Request["order[0][dir]"] ?? "asc";

                    if (length == 0) length = 100;

                    string[] columns = {
                        "id","no","storeNo", "divisionCode","approveDate", "itemNo", "description", "packingInfo", "qty", "uom",
                         "action", "status", "remark", "approver", "note", "vendorNo", "vendorName", "regeDate"
                    };

                    string[] searchableColumns = {
                        "no","storeNo", "divisionCode","approveDate", "itemNo", "description", "packingInfo", "qty", "uom",
                         "action", "status", "remark", "approver", "note", "vendorNo", "vendorName", "regeDate"
                    };


                    string searchValue = Request["search[value]"] ?? "";
                    string[] dateColumns = { "regeDate", "approveDate", "completedDate" };

                    // Build WHERE clause
                    StringBuilder whereClause = new StringBuilder();
                    whereClause.Append("(status NOT IN ('Reorder Done', 'No Reordering') OR status IS NULL) AND (approved = 'approved')");

                    if (!string.IsNullOrEmpty(searchValue))
                    {
                        whereClause.Append(" AND (");
                        foreach (string col in searchableColumns)
                        {
                            if (dateColumns.Contains(col))
                            {
                                whereClause.Append($"(CONVERT(VARCHAR, {col}, 103) LIKE '%' + @SearchValue + '%' OR ");
                                whereClause.Append($"CONVERT(VARCHAR, {col}, 126) LIKE '%' + @SearchValue + '%') OR ");
                            }
                            else
                            {
                                whereClause.Append($"{col} LIKE '%' + @SearchValue + '%' OR ");
                            }
                        }
                        whereClause.Remove(whereClause.Length - 4, 4);
                        whereClause.Append(")");
                    }

                    string orderByClause = " ORDER BY id ASC";

                    // Validate the index from DataTables
                    if (orderColumnIndex >= 0 && orderColumnIndex < columns.Length)
                    {
                        string orderColumn = columns[orderColumnIndex];

                        // Do not allow sorting on the checkbox column
                        if (orderColumn != "id")
                        {
                            orderDir = (orderDir == "asc" || orderDir == "desc") ? orderDir : "asc";
                            orderByClause = $" ORDER BY [{orderColumn}] {orderDir}";
                        }
                    }
                    else
                    {
                        orderByClause = " ORDER BY no ASC";
                    }

                    // Get paginated data
                    string query = $@"
                        SELECT * 
                        FROM itemListR 
                        WHERE {whereClause}
                        {orderByClause}
                        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                    System.Data.DataTable dt = new System.Data.DataTable();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Offset", start);
                        cmd.Parameters.AddWithValue("@SearchValue", searchValue);
                        cmd.Parameters.AddWithValue("@PageSize", length);

                        conn.Open();
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }
                    }

                    // Get total records count
                    string countQuery = $@"SELECT COUNT(*) FROM itemListR WHERE {whereClause}";
                    int totalRecords = 0;
                    using (SqlCommand countCmd = new SqlCommand(countQuery, conn))
                    {
                        countCmd.Parameters.AddWithValue("@SearchValue", searchValue);
                        if (conn.State != ConnectionState.Open) conn.Open();
                        totalRecords = (int)countCmd.ExecuteScalar();
                    }

                    var data = dt.AsEnumerable().Select(row => {
                        // Create search text by joining all values
                        string searchText = string.Join(" ", row.ItemArray
                            .Where(x => !DBNull.Value.Equals(x))
                            .Select(x => x.ToString().ToLower()));

                        return new
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
                            divisionCode = row["divisionCode"],
                            storeNo = row["storeNo"],
                            vendorNo = row["vendorNo"],
                            vendorName = row["vendorName"],
                            regeDate = row["regeDate"] is DBNull ? "" : Convert.ToDateTime(row["regeDate"]).ToString("yyyy-MM-dd"),
                            approveDate = row["approveDate"] is DBNull ? "" : Convert.ToDateTime(row["approveDate"]).ToString("yyyy-MM-dd"),
                            approver = row["approver"],
                            note = row["note"],
                            action = row["action"],
                            status = row["status"],
                            remark = row["remark"],
                        };
                    }).ToList();

                    // Create response object
                    var response = new
                    {
                        draw = draw,
                        recordsTotal = totalRecords,
                        recordsFiltered = totalRecords,
                        data = data
                    };

                    // Return JSON
                    Response.ContentType = "application/json";
                    Response.Write(JsonConvert.SerializeObject(response));
                }
            }
            catch (Exception ex)
            {
                Response.ContentType = "application/json";
                Response.Write(JsonConvert.SerializeObject(new
                {
                    draw = Request["draw"],
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new List<object>(),
                    error = ex.Message
                }));
            }
            finally
            {
                Response.End();
            }
        }

        protected void GridView2_RowEditing(object sender, GridViewEditEventArgs e)
        {
            GridView2.EditIndex = e.NewEditIndex;
            string searchTerm = hfLastSearch.Value;

            if (Request["search[value]"] != null)
            {
                ViewState["LastSearch"] = Request["search[value]"];
            }

            if (hfIsFiltered.Value == "true")
            {
                if (ViewState["FilteredData"] != null)
                {
                    GridView2.DataSource = ViewState["FilteredData"] as System.Data.DataTable;
                    GridView2.DataBind();

                    if (GridView2.HeaderRow != null)
                    {
                        GridView2.HeaderRow.TableSection = TableRowSection.TableHeader;
                        GridView2.HeaderRow.CssClass = "static-header";
                    }

                    ScriptManager.RegisterStartupScript(this, GetType(), "RestoreHeader_" + GridView2.ClientID,
                        "$('[id=\\\"" + GridView2.ClientID + "\\\"] thead').addClass('static-header').css('display','table-header-group').show();",
                        true);

                }
            }
            else
            {
                BindGrid();
            }

            searchValue.Text = searchTerm;
            searchValue.Style["display"] = "inline";
            searchLabel.Style["display"] = "inline";

            if (GridView2.DataKeys != null && e.NewEditIndex < GridView2.DataKeys.Count)
            {
                string rowId = GridView2.DataKeys[e.NewEditIndex].Value.ToString();
                hfEditedRowId.Value = rowId;
                ViewState["EditedRowID"] = rowId;
            }
        }

        protected void GridView2_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            string editedId = GridView2.DataKeys[e.RowIndex].Value.ToString();
            hfSelectedIDs.Value = editedId;
            string username = User.Identity.Name;

            GridViewRow row = GridView2.Rows[e.RowIndex];
            string itemId = Convert.ToString(GridView2.DataKeys[e.RowIndex].Value);

            DropDownList ddlAction = (DropDownList)row.FindControl("ddlActionEdit");
            DropDownList ddlStatus = (DropDownList)row.FindControl("ddlStatusEdit");
            TextBox remark = (TextBox)row.FindControl("txtRemark");

            if (ddlAction == null || ddlStatus == null || remark == null)
            {
                ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
                     "swal('Error!', 'Null Update Value!', 'error');", true);
                return;
            }

            string selectedAction = GetActionText(ddlAction.SelectedValue);
            string selectedStatus = GetStatusText(ddlStatus.SelectedValue);

            bool editInitiatedByBtn = hfEditInitiatedByButton.Value == "true";

            using (SqlConnection conn = new SqlConnection(strcon))
            {
                string query = @"UPDATE itemListR 
            SET Action = @action, 
                Status = @status, 
                Remark = @remark, 
                completedDate = @completedDate,
                owner=@owner
            WHERE id = @itemId";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@action", selectedAction);
                cmd.Parameters.AddWithValue("@status", selectedStatus);
                cmd.Parameters.AddWithValue("@remark", remark.Text);

                if (!string.IsNullOrEmpty(ddlStatus.SelectedValue))
                {
                    cmd.Parameters.AddWithValue("@completedDate", DateTime.Now.Date);
                    cmd.Parameters.AddWithValue("@owner", username);
                }
                else
                {
                    cmd.Parameters.AddWithValue("@completedDate", DBNull.Value);
                    cmd.Parameters.AddWithValue("@owner", DBNull.Value);
                }

                cmd.Parameters.AddWithValue("@itemId", editedId);

                conn.Open();
                cmd.ExecuteNonQuery();
            }

            GridView2.EditIndex = -1;

            if (hfIsFiltered.Value == "true")
            {
                System.Data.DataTable dt = GetFilteredData();
                ViewState["FilteredData"] = dt;
                GridView2.DataSource = dt;
                GridView2.DataBind();

                if (GridView2.HeaderRow != null)
                {
                    GridView2.HeaderRow.TableSection = TableRowSection.TableHeader;
                    GridView2.HeaderRow.CssClass = "static-header";
                }

                ScriptManager.RegisterStartupScript(this, GetType(), "RestoreHeader_" + GridView2.ClientID,
                    "$('[id=\\\"" + GridView2.ClientID + "\\\"] thead').addClass('static-header').css('display','table-header-group').show();",
                    true);

            }
            else
            {
                BindGrid();
                Response.Redirect("viewer1.aspx", false);
            }

            hfEditInitiatedByButton.Value = "false";

            ClientScript.RegisterStartupScript(this.GetType(), "success",
                "swal('Success!', 'Update completed!', 'success');", true);
        }

        protected void GridView2_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            GridView2.EditIndex = -1;

            bool editInitiatedByBtn = hfEditInitiatedByButton.Value == "true";

            if (hfIsFiltered.Value == "true")
            {
                RebindGridWithFilter();
            }
            else
            {
                BindGrid();
                RefreshDataTable();
            }

            hfEditInitiatedByButton.Value = "false";

            if (editInitiatedByBtn)
            {
                ScriptManager.RegisterStartupScript(this, GetType(), "RedirectAfterCancel",
                    "window.location = 'viewer1.aspx';", true);
            }

            if (ViewState["LastSearch"] != null)
            {
                ScriptManager.RegisterStartupScript(this, GetType(), "PreserveSearch",
                    "$('#<%= hfLastSearch.ClientID %>').val('" + ViewState["LastSearch"] + "');", true);
            }
        }

        protected string GetActionText(string action)
        {
            switch (action)
            {
                case "0":
                    return "0";
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
                    return "0";
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
                // Check if at least one filter is selected and has a value
                bool hasAnyFilter =
                    (filterStore.Checked && lstStoreFilter.SelectedIndex >= 0) ||
                    (filterItem.Checked && !string.IsNullOrEmpty(item.SelectedValue)) ||
                    (filterVendor.Checked && !string.IsNullOrEmpty(vendor.SelectedValue)) ||
                    (filterStatus.Checked && !string.IsNullOrEmpty(ddlStatusFilter.SelectedValue)) ||
                    (filterAction.Checked && !string.IsNullOrEmpty(ddlActionFilter.SelectedValue)) ||
                    (filterRegistrationDate.Checked && !string.IsNullOrEmpty(txtRegDateFilter.Text)) ||
                    (filterStaff.Checked && !string.IsNullOrEmpty(txtstaffFilter.Text)) ||
                    (filterDivisionCode.Checked && !string.IsNullOrEmpty(txtDivisionCodeFilter.Text)) ||
                    (filterApproveDate.Checked && !string.IsNullOrEmpty(txtApproveDateFilter.Text));

                if (!hasAnyFilter)
                {
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "alertNoFilter",
                        "Swal.fire('Warning!', 'Please select at least one filter with a value.', 'warning');", true);
                    return;
                }

                // Save selected filter values in ViewState (optional)
                ViewState["SelectedStores"] = string.Join(",", lstStoreFilter.Items.Cast<ListItem>()
                                                    .Where(li => li.Selected).Select(li => li.Value));
                ViewState["SelectedItem"] = item.SelectedValue;
                ViewState["SelectedVendor"] = vendor.SelectedValue;
                ViewState["SelectedStatus"] = ddlStatusFilter.SelectedValue;
                ViewState["SelectedAction"] = ddlActionFilter.SelectedValue;
                ViewState["SelectedRegDate"] = txtRegDateFilter.Text;
                ViewState["SelectedStaff"] = txtstaffFilter.Text;
                ViewState["SelectedDivisionCode"] = txtDivisionCodeFilter.Text;
                ViewState["SelectedApproveDate"] = txtApproveDateFilter.Text;

                // Get filtered data from your data source
                System.Data.DataTable dtFiltered = GetFilteredData();
                ViewState["FilteredData"] = dtFiltered;

                // Bind GridView (optional for server-side fallback)
                GridView2.DataSource = dtFiltered;
                GridView2.DataBind();

                // Ensure header and body are set correctly
                if (GridView2.HeaderRow != null)
                {
                    GridView2.HeaderRow.TableSection = TableRowSection.TableHeader;
                }
                foreach (GridViewRow row in GridView2.Rows)
                {
                    row.TableSection = TableRowSection.TableBody;
                }

                // Trigger DataTable AJAX reload OR rebind editing
                ScriptManager.RegisterStartupScript(this, this.GetType(), "ReloadTableAndResize", @"
                    if (typeof dataTable !== 'undefined' && $.fn.DataTable.isDataTable('#" + GridView2.ClientID + @"')) { 
                        // Draw new data and then reinitialize resizable columns
                        dataTable.draw(false); 
                        initializeResizableColumns();
                    } else { 
                        // Fallback for non-DataTable cases
                        enableGridViewInlineEditing(); 
                        initializeResizableColumns();
                    }", true);

            }
            catch (Exception ex)
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), "alertError",
                    "Swal.fire('Error!', 'An error occurred while applying filters.', 'error');", true);
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
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                string id = GridView2.DataKeys[e.Row.RowIndex].Value.ToString();
                e.Row.Attributes["data-id"] = id;

                if (GridView2.EditIndex == e.Row.RowIndex)
                {
                    DataRowView rowView = (DataRowView)e.Row.DataItem;

                    DropDownList ddlAction = (DropDownList)e.Row.FindControl("ddlActionEdit");
                    if (ddlAction != null)
                        ddlAction.SelectedValue = GetActionText(rowView["action"].ToString());

                    DropDownList ddlStatus = (DropDownList)e.Row.FindControl("ddlStatusEdit");
                    if (ddlStatus != null)
                        ddlStatus.SelectedValue = GetStatusText(rowView["status"].ToString());

                    ViewState["EditedRowID"] = id;
                }

                string searchTerm = hfLastSearch.Value;
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    // Apply highlighting to each cell
                    foreach (TableCell cell in e.Row.Cells)
                    {
                        TextBox txt = cell.Controls.OfType<TextBox>().FirstOrDefault();
                        if (txt != null && txt.Text.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            cell.BackColor = System.Drawing.Color.Yellow;
                            continue;
                        }

                        if (!string.IsNullOrEmpty(cell.Text)
                            && cell.Text.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            cell.Text = System.Text.RegularExpressions.Regex.Replace(
                                cell.Text,
                                $"({System.Text.RegularExpressions.Regex.Escape(searchTerm)})",
                                "<span class='highlight'>$1</span>",
                                System.Text.RegularExpressions.RegexOptions.IgnoreCase
                            );
                        }
                    }
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
            txtRegDateFilter.Text = string.Empty;
            txtDivisionCodeFilter.Text = string.Empty;
            txtApproveDateFilter.Text = string.Empty;

            filterAction.Checked = false;
            filterStatus.Checked = false;
            filterStore.Checked = false;
            filterItem.Checked = false;
            filterStaff.Checked = false;
            filterVendor.Checked = false;
            filterRegistrationDate.Checked = false;
            filterDivisionCode.Checked = false;
            filterApproveDate.Checked = false;

            hfIsFiltered.Value = "false";
            ViewState["IsFiltered"] = "false";
            ViewState["FilteredData"] = null;

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
            ViewState["FilterDivisionCodeChecked"] = null;
            ViewState["SelectedDivisionCode"] = null;
            ViewState["FilterApproveDateChecked"] = null;
            ViewState["SelectedApproveDate"] = null;

            BindGrid();

            ScriptManager.RegisterStartupScript(this, GetType(), "ResetFilters",
                @"if (typeof(updateFilterVisibility) === 'function') { 
                    updateFilterVisibility(); 
                    toggleFilter(false); 
                }", true);

            ScriptManager.RegisterStartupScript(this, GetType(), "redirect",
                "window.location.href = 'viewer1.aspx';", true);
        }

        private void BindGrid(int pageNumber = 1, int pageSize = 100)
        {
            using (SqlConnection conn = new SqlConnection(strcon))
            {
                string query = @"
                    SELECT * FROM itemListR 
                    WHERE (status NOT IN ('Reorder Done','No Reordering') OR status IS NULL) 
                    AND (approved = 'approved')
                    ORDER BY id
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Offset", (pageNumber - 1) * pageSize);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);

                conn.Open();
                System.Data.DataTable dt = new System.Data.DataTable();
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
                conn.Close();

                GridView2.DataSource = dt;
                GridView2.PageIndex = pageNumber - 1;
                GridView2.DataBind();

                if (GridView2.HeaderRow != null)
                {
                    GridView2.HeaderRow.TableSection = TableRowSection.TableHeader;
                    GridView2.HeaderRow.CssClass = "static-header";
                }

                ScriptManager.RegisterStartupScript(this, GetType(), "RestoreHeader_" + GridView2.ClientID,
                    "$('[id=\\\"" + GridView2.ClientID + "\\\"] thead').addClass('static-header').css('display','table-header-group').show();",
                    true);


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
                    ScriptManager.RegisterStartupScript(this, GetType(), "alert",
                        "swal('Error!', 'Please select at least one record!', 'error');", true);
                    Response.Redirect("viewer1.aspx");
                }
                else
                {
                    if (selectedAction == "0" || ddlAction.SelectedValue == "0")
                    {
                        ScriptManager.RegisterStartupScript(this, GetType(), "alert",
                            "swal('Error!', 'Please select at least one reason!', 'error');", true);
                        RebindGridWithFilter();
                    }
                    else
                    {


                        string[] ids = selectedIDs.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                        using (SqlConnection conn = new SqlConnection(strcon))
                        {
                            conn.Open();
                            foreach (string idStr in ids)
                            {
                                if (int.TryParse(idStr, out int id))
                                {
                                    string updateQuery = "UPDATE itemListR SET Action = @Action WHERE id = @id";
                                    using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                                    {
                                        cmd.Parameters.AddWithValue("@Action", selectedAction);
                                        cmd.Parameters.AddWithValue("@id", id);
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }
                        }

                        var dt = ViewState["FilteredData"] as System.Data.DataTable;
                        if (dt != null && dt.Rows.Count > 0)
                        {
                            string actionCol = dt.Columns.Contains("action") ? "action"
                                             : dt.Columns.Contains("Action") ? "Action"
                                             : null;

                            if (actionCol != null)
                            {
                                foreach (string idStr in ids)
                                {
                                    if (int.TryParse(idStr, out int id))
                                    {
                                        DataRow[] rows = dt.Select("id = " + id);
                                        foreach (DataRow r in rows)
                                        {
                                            r[actionCol] = selectedAction;
                                        }
                                    }
                                }
                                ViewState["FilteredData"] = dt;
                            }
                            else
                            {
                                ViewState["FilteredData"] = null;
                            }
                        }

                        if (hfIsFiltered.Value == "true")
                        {
                            RebindGridWithFilter();
                        }
                        else
                        {
                            Response.Redirect("viewer1.aspx");
                        }

                        var updatedRows = ids.Select(id => new { id = id, action = selectedAction }).ToList();
                        string jsArray = Newtonsoft.Json.JsonConvert.SerializeObject(updatedRows);

                        string ddlClientId = ddlAction.ClientID;
                        string clearDropdownScript =
                            $"document.getElementById('{ddlClientId}').selectedIndex = 0;" +
                            $"$('#{ddlClientId}').trigger('change');";

                        ScriptManager.RegisterStartupScript(this, this.GetType(), "clearDropdown",
                            clearDropdownScript, true);

                        ScriptManager.RegisterStartupScript(this, this.GetType(), "highlightRows",
                            $@"setTimeout(function() {{ 
                            updateSelectedRows({jsArray}); 
                            swal('Success!', 'Selected records have been updated successfully!', 'success');
                        }}, 200);", true);

                        // Reset server-side state
                        hfSelectedIDs.Value = string.Empty;
                        ddlAction.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                ddlAction.SelectedIndex = 0;
                ScriptManager.RegisterStartupScript(this, GetType(), "alert",
                    $"swal('Error!', 'Error: {ex.Message}', 'error');", true);
            }
        }

        protected void btnStatusSelected_Click(object sender, EventArgs e)
        {
            try
            {

                string selectedStatus = GetStatusText(ddlStatus.SelectedValue);
                string selectedIDs = hfSelectedIDs.Value;

                // Handle no selected records
                if (string.IsNullOrEmpty(selectedIDs))
                {
                    ScriptManager.RegisterStartupScript(this, GetType(), "no-records-alert",
                        "swal('Error!', 'Please select at least one record!', 'error');",
                        true);
                    Response.Redirect("viewer1.aspx");
                }
                else
                {
                    // Handle no selected status
                    if (selectedStatus == "0" || ddlStatus.SelectedValue == "0")
                    {
                        ScriptManager.RegisterStartupScript(this, GetType(), "no-records-alert",
                       "swal('Error!', 'Please select at least one status!', 'error');",
                       true);
                        RebindGridWithFilter();
                    }
                    else
                    {
                        string[] ids = selectedIDs.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        string username = User.Identity.Name;

                        using (SqlConnection conn = new SqlConnection(strcon))
                        {
                            conn.Open();
                            foreach (string idStr in ids)
                            {
                                if (int.TryParse(idStr, out int id))
                                {
                                    string updateQuery;
                                    using (SqlCommand cmd = new SqlCommand())
                                    {
                                        cmd.Connection = conn;

                                        if (string.IsNullOrEmpty(selectedStatus) || selectedStatus == "0")
                                        {
                                            updateQuery = "UPDATE itemListR SET Status = @Status, completedDate = @completedDate WHERE id = @id";
                                            cmd.CommandText = updateQuery;
                                            cmd.Parameters.AddWithValue("@Status", "");
                                            cmd.Parameters.AddWithValue("@completedDate", DBNull.Value);
                                            cmd.Parameters.AddWithValue("@id", id);
                                        }
                                        else
                                        {
                                            updateQuery = "UPDATE itemListR SET Status = @Status, completedDate = @completedDate, owner = @owner WHERE id = @id";
                                            cmd.CommandText = updateQuery;
                                            cmd.Parameters.AddWithValue("@Status", selectedStatus);
                                            cmd.Parameters.AddWithValue("@completedDate", DateTime.Now.Date);
                                            cmd.Parameters.AddWithValue("@owner", username);
                                            cmd.Parameters.AddWithValue("@id", id);
                                        }

                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }
                        }

                        // Update ViewState["FilteredData"]
                        var dt = ViewState["FilteredData"] as System.Data.DataTable;
                        if (dt != null && dt.Rows.Count > 0)
                        {
                            string statusCol = dt.Columns.Contains("Status") ? "Status" : null;
                            string completedDateCol = dt.Columns.Contains("completedDate") ? "completedDate" : null;
                            string ownerCol = dt.Columns.Contains("owner") ? "owner" : null;

                            if (statusCol != null)
                            {
                                foreach (string idStr in ids)
                                {
                                    if (int.TryParse(idStr, out int id))
                                    {
                                        DataRow[] rows = dt.Select("id = " + id);
                                        foreach (DataRow r in rows)
                                        {
                                            if (string.IsNullOrEmpty(selectedStatus) || selectedStatus == "0")
                                            {
                                                r[statusCol] = "";
                                                if (completedDateCol != null) r[completedDateCol] = DBNull.Value;
                                                if (ownerCol != null) r[ownerCol] = DBNull.Value;
                                            }
                                            else
                                            {
                                                r[statusCol] = selectedStatus;
                                                if (completedDateCol != null) r[completedDateCol] = DateTime.Now.Date;
                                                if (ownerCol != null) r[ownerCol] = username;
                                            }
                                        }
                                    }
                                }
                                ViewState["FilteredData"] = dt;
                            }
                            else
                            {
                                ViewState["FilteredData"] = null;
                            }
                        }

                        // Rebind grid
                        if (hfIsFiltered.Value == "true")
                        {
                            RebindGridWithFilter();
                        }
                        else
                        {
                            BindGrid();
                        }

                        // Build JS array for updated rows
                        var updatedRows = ids.Select(id => new
                        {
                            id = id,
                            status = selectedStatus,
                            completedDate = DateTime.Now.ToString("yyyy-MM-dd"),
                            owner = User.Identity.Name
                        }).ToList();

                        string jsArray = Newtonsoft.Json.JsonConvert.SerializeObject(updatedRows);

                        // Clear dropdown using combined JS/jQuery
                        string clearScript = $@"
                            document.getElementById('{ddlStatus.ClientID}').selectedIndex = 0;
                            $('#{ddlStatus.ClientID}').trigger('change');
                            setTimeout(function() {{ 
                                updateSelectedStatusRows({jsArray}); 
                                swal('Success!', 'Selected records updated successfully!', 'success');
                            }}, 200);";

                        ScriptManager.RegisterStartupScript(this, GetType(), "update-ui", clearScript, true);

                        // Reset server-side state
                        hfSelectedIDs.Value = string.Empty;
                        ddlStatus.SelectedIndex = 0;
                    }
                }
                
            }
            catch (Exception ex)
            {
                ddlStatus.SelectedIndex = 0;
                ScriptManager.RegisterStartupScript(this, GetType(), "alert",
                    $"swal('Error!', 'Error: {ex.Message}', 'error');", true);
            }
        }

        private void RebindGridWithFilter()
        {
            System.Data.DataTable dt = null;

            if (ViewState["FilteredData"] != null)
            {
                dt = ViewState["FilteredData"] as System.Data.DataTable;
            }

            if (dt == null)
            {
                dt = GetFilteredData();
                ViewState["FilteredData"] = dt;
            }

            if (GridView2.PageIndex > 0 && dt.Rows.Count <= GridView2.PageSize * GridView2.PageIndex)
            {
                GridView2.PageIndex = 0;
            }

            GridView2.DataSource = dt;
            GridView2.DataBind();

            if (GridView2.HeaderRow != null)
            {
                GridView2.HeaderRow.TableSection = TableRowSection.TableHeader;
                GridView2.HeaderRow.CssClass = "static-header";
            }

            ScriptManager.RegisterStartupScript(this, GetType(), "RestoreHeader_" + GridView2.ClientID,
                "$('[id=\\\"" + GridView2.ClientID + "\\\"] thead').addClass('static-header').css('display','table-header-group').show();",
                true);

        }

        private void RefreshDataTable()
        {
            if (hfIsFiltered.Value != "true")
            {
                ScriptManager.RegisterStartupScript(this, GetType(), "RefreshDataTable",
                    "if (typeof initializeComponents === 'function') { initializeComponents(); }",
                    true);
            }
        }

        private System.Data.DataTable GetFilteredData()
        {
            string query = @"SELECT * FROM itemListR
                WHERE (status NOT IN ('Reorder Done','No Reordering') 
                OR status IS NULL) AND (approved = 'approved')";
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
                query += " AND vendorNo LIKE @VendorNo";
                parameters.Add(new SqlParameter("@VendorNo", vendor.SelectedValue.Trim() + "%"));
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
                query += " AND approver = @StaffNo";
                parameters.Add(new SqlParameter("@StaffNo", txtstaffFilter.Text));
            }

            // Approved Date filter
            if (filterApproveDate.Checked && !string.IsNullOrWhiteSpace(txtApproveDateFilter.Text))
            {
                Debug.WriteLine($"Selected staff: {txtApproveDateFilter.Text}");
                query += " AND approveDate = @approved";
                parameters.Add(new SqlParameter("@approved", txtApproveDateFilter.Text));
            }

            if (filterDivisionCode.Checked && !string.IsNullOrWhiteSpace(txtDivisionCodeFilter.Text))
            {
                query += " AND divisionCode = @division";
                parameters.Add(new SqlParameter("@division", txtDivisionCodeFilter.Text.Trim()));
            }

            System.Data.DataTable dt = new System.Data.DataTable();

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

        protected string TruncateWords(string text, int maxWords)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            string[] words = text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length <= maxWords)
                return text;

            return string.Join(" ", words.Take(maxWords)) + " ...";
        }

        protected void btnEdit_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(hfSelectedIDs.Value))
            {
                string[] selectedIds = hfSelectedIDs.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                if (selectedIds.Length > 1)
                {
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "alertMultiple",
                        "swal('Warning!', 'Please select only one row to edit.', 'warning');", true);
                    Response.Redirect("viewer1.aspx", false);
                }

                string selectedId = selectedIds.FirstOrDefault();
                GridView2.HeaderRow.TableSection = TableRowSection.TableHeader;
                hfEditInitiatedByButton.Value = "true";

                for (int i = 0; i < GridView2.Rows.Count; i++)
                {
                    var rowId = GridView2.DataKeys[i].Value.ToString();
                    if (rowId == selectedId)
                    {
                        GridView2.EditIndex = i;
                        break;
                    }
                }

                RebindGridWithFilter();
                hfIsSearchEdit.Value = "true";
                hfEditedRowId.Value = selectedId;

                if (hfIsFiltered.Value != "true")
                {
                    RefreshDataTable();
                }

                ScriptManager.RegisterStartupScript(this, GetType(), "ScrollToRow",
                    $"setTimeout(function() {{ scrollToEditedRow('{selectedId}'); }}, 300);", true);
            }
            else
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), "alertMultiple",
                    "swal('Warning!', 'Please select only one row to edit.', 'warning');", true);
                Response.Redirect("viewer1.aspx", false);
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

        protected void btnDelete_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(hfSelectedIDs.Value))
            {
                string[] selectedIds = hfSelectedIDs.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                using (SqlConnection conn = new SqlConnection(strcon))
                {
                    conn.Open();
                    foreach (string idStr in selectedIds)
                    {
                        if (int.TryParse(idStr, out int id))
                        {
                            string deleteQuery = "DELETE FROM itemListR WHERE id = @id";
                            using (SqlCommand cmd = new SqlCommand(deleteQuery, conn))
                            {
                                cmd.Parameters.AddWithValue("@id", id);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                }

                ScriptManager.RegisterStartupScript(this, this.GetType(), "alertSuccess",
                     "swal('Success!', 'Item is successfully deleted!', 'success').then(function(){ window.location='viewer1.aspx'; });",
                  true);
                return; 
            }
            else
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(),
                       "alertWarning", "swal('Warning!', 'Please select only one row to edit.', 'warning').then(function(){ window.location='viewer1.aspx'; });", true);
                return;
            }
        }
    }
}