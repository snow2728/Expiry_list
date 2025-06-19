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

                string[] columns = { "id", "no", "itemNo", "description", "qty", "uom", "packingInfo", "storeNo", "approver", "vendorNo", "vendorName", "regeDate", "action", "status", "note", "remark", "completedDate" };

                string[] searchableColumns = {
                   "id", "no", "itemNo", "description", "qty", "uom", "packingInfo", "storeNo", "approver", "vendorNo", "vendorName", "regeDate", "action", "status", "note", "remark", "completedDate"
                };

                string searchValue = Request["search"] ?? "";

                // Build WHERE clause with search
                StringBuilder whereClause = new StringBuilder();
                whereClause.Append("(status NOT IN ('Reorder Added', 'No Reorder Added') OR status IS NULL) AND (approved = 'approved')");
                if (!string.IsNullOrEmpty(searchValue))
                {
                    whereClause.Append(" AND (");
                    foreach (string col in searchableColumns)
                    {
                        whereClause.Append($"{col} LIKE '%' + @SearchValue + '%' OR ");
                    }
                    whereClause.Remove(whereClause.Length - 4, 4);
                    whereClause.Append(")");
                }

                string orderByClause = "";
                int serverColumnIndex = orderColumnIndex - 1;
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
                   FROM itemListR 
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
                   FROM itemListR 
                   WHERE {whereClause}";

                SqlCommand countCmd = new SqlCommand(countQuery, conn);
                countCmd.Parameters.AddWithValue("@SearchValue", searchValue);

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
                    qty = row["qty"],
                    uom = row["uom"],
                    packingInfo = row["packingInfo"],
                    storeNo = row["storeNo"],
                    vendorNo = row["vendorNo"],
                    vendorName = row["vendorName"],
                    regeDate = row["regeDate"],
                    // approved = Convert.ToBoolean(row["approved"]) ? "Approved" : "Not Approved",
                    approver = row["approver"],
                    note = row["note"],
                    action = row["action"],
                    status = row["status"],
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

            using (SqlConnection conn = new SqlConnection(strcon))
            {
                string query = @"UPDATE itemListR 
                         SET Action = @action, 
                             Status = @status, 
                             Remark = @remark, 
                             completedDate = @completedDate 
                         WHERE id = @itemId";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@action", selectedAction);
                cmd.Parameters.AddWithValue("@status", selectedStatus);
                cmd.Parameters.AddWithValue("@remark", remark.Text);
               
                
                if( ddlStatus.SelectedValue != "")
                {
                    cmd.Parameters.AddWithValue("@completedDate", DateTime.Now.Date);
                }
                else
                {
                    ClientScript.RegisterStartupScript(this.GetType(), "success",
                        "swal('Warning!', 'Status is blank!', 'Warning');", true);
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
            Response.Redirect("viewer1.aspx");
        }

        protected void GridView2_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            GridView2.EditIndex = -1;
            BindGrid();
            Response.Redirect("viewer1.aspx");
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
                bool hasAnyFilter = filterStore.Checked || filterItem.Checked || filterVendor.Checked ||
                           filterStatus.Checked || filterAction.Checked ||
                           filterRegistrationDate.Checked || filterStaff.Checked;

                if (!hasAnyFilter)
                {
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "alertNoFilter",
                        "Swal.fire('Warning!', 'Please select at least one filter to apply and ensure it has a value.', 'warning');", true);
                    return;
                }

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
            txtRegDateFilter.Text = string.Empty;

            filterAction.Checked = false;
            filterStatus.Checked = false;
            filterStore.Checked = false;
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
                string orderBy = string.IsNullOrEmpty(hfSelectedIDs.Value)
                    ? "ORDER BY id"
                    : $"ORDER BY CASE WHEN id = '{hfSelectedIDs.Value}' THEN 0 ELSE 1 END, id";

                string query = $@"
                    SELECT * FROM itemListR 
                     WHERE (status NOT IN ('Reorder Added','No Reorder Added') OR status IS NULL) AND (approved = 'approved')
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
                            string updateQuery = "UPDATE itemListR SET Action = @Action  WHERE id = @id";
                            using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                            {
                                cmd.Parameters.AddWithValue("@Action", "");
                                cmd.Parameters.AddWithValue("@id", id);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        else if (!string.IsNullOrEmpty(selectedAction) && selectedAction != "0")
                        {
                            string updateQuery = "UPDATE itemListR SET Action = @Action  WHERE id = @id";
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
                        "swal('Success!', 'Selected records have been updated successfully!', 'success').then(function() { window.location = 'viewer1.aspx'; });", true);

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
                            string updateQuery = "UPDATE itemListR SET Status = @Status, completedDate =@completedDate WHERE id = @id ";
                            using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                            {
                                cmd.Parameters.AddWithValue("@Status", "");
                                cmd.Parameters.AddWithValue("@completedDate", "");
                                cmd.Parameters.AddWithValue("@id", id);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        else if (!string.IsNullOrEmpty(selectedStatus) && selectedStatus != "0")
                        {
                            string updateQuery = "UPDATE itemListR SET Status = @Status, completedDate =@completedDate WHERE id = @id ";
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

        private DataTable GetFilteredData()
        {
            string query = @"SELECT * FROM itemListR
                WHERE (status NOT IN ('Reorder Added','No Reorder Added') 
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

        protected void btnEdit_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(hfSelectedIDs.Value))
            {
                string[] selectedIds = hfSelectedIDs.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                if (selectedIds.Length > 1)
                {
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

    }
}