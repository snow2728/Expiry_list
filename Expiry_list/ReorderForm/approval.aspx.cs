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
    public partial class approval : System.Web.UI.Page
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
                string searchValue = Request["search[value]"] ?? "";

                if (length == 0) length = 100;

                string[] columns = {
                    "id", "no", "itemNo", "description", "qty", "uom", "packingInfo",
                    "storeNo", "vendorNo", "vendorName", "regeDate", "note", "approved"
                };
                string[] searchableColumns = columns.Skip(1).ToArray();

                var whereClause = new StringBuilder("(status NOT IN ('Reorder Added', 'No Reorder Added') OR status IS NULL) AND  (approved != 'approved')");
                var cmd = new SqlCommand();
                var countCmd = new SqlCommand();
                var parameters = new List<SqlParameter>();

                // Store filter
                var storeList = Session["storeListRaw"] as List<string>;
                bool showAllStores = storeList != null && storeList.Contains("HO", StringComparer.OrdinalIgnoreCase);

                if (!showAllStores && storeList != null && storeList.Count > 0)
                {
                    var storeConditions = new List<string>();
                    for (int i = 0; i < storeList.Count; i++)
                    {
                        string paramName = $"@store{i}";
                        storeConditions.Add($"storeNo = {paramName}");
                        parameters.Add(new SqlParameter(paramName, storeList[i]));
                    }
                    whereClause.Append(" AND (" + string.Join(" OR ", storeConditions) + ")");
                }

                // Search
                if (!string.IsNullOrEmpty(searchValue))
                {
                    var searchParts = searchableColumns.Select(c => $"{c} LIKE @SearchValue");
                    whereClause.Append(" AND (" + string.Join(" OR ", searchParts) + ")");
                    parameters.Add(new SqlParameter("@SearchValue", $"%{searchValue}%"));
                }

                // OrderBy
                string orderBy = columns.ElementAtOrDefault(orderColumnIndex) ?? "id";
                string orderByClause = $"ORDER BY {orderBy} {orderDir}";

                // Final queries
                string query = $@"
                    SELECT * FROM itemListR 
                    WHERE {whereClause}
                    {orderByClause}
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                string countQuery = $@"SELECT COUNT(*) FROM itemListR WHERE {whereClause}";

                // Setup commands
                cmd.CommandText = query;
                countCmd.CommandText = countQuery;
                cmd.Connection = conn;
                countCmd.Connection = conn;

                parameters.ForEach(p =>
                {
                    cmd.Parameters.AddWithValue(p.ParameterName, p.Value);
                    countCmd.Parameters.AddWithValue(p.ParameterName, p.Value);
                });

                cmd.Parameters.AddWithValue("@Offset", start);
                cmd.Parameters.AddWithValue("@PageSize", length);

                // Execute
                DataTable dt = new DataTable();
                conn.Open();
                new SqlDataAdapter(cmd).Fill(dt);
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
                    note = row["note"],
                    approved = row["approved"]
                }).ToList();

                var response = new
                {
                    draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = totalRecords,
                    data,
                    orderColumn = orderColumnIndex,
                    orderDir
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

        protected void GridView2_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            string editedId = GridView2.DataKeys[e.RowIndex].Value.ToString();
            hfSelectedIDs.Value = editedId;

            string userName = Session["username"].ToString();

            GridViewRow row = GridView2.Rows[e.RowIndex];
            string itemId = Convert.ToString(GridView2.DataKeys[e.RowIndex].Value);

            DropDownList ddlApprove = (DropDownList)row.FindControl("ddlApprovalEdit");

            if ( ddlApprove == null)
            {
                ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
                     "swal('Error!', 'Null Update Value!', 'error');", true);
                return;
            }

            using (SqlConnection conn = new SqlConnection(strcon))
            {
                string query = "UPDATE itemListR SET approved = @approve, approver=@approver, completedDate=@completedDate WHERE id = @itemId";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@approve", ddlApprove.SelectedValue);
                cmd.Parameters.AddWithValue("@approver", userName);
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
            Response.Redirect("approval.aspx");
        }

        protected void GridView2_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            GridView2.EditIndex = -1;
            BindGrid();
            Response.Redirect("approval.aspx");
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


                ViewState["FilterRegDateChecked"] = filterRegistrationDate.Checked;
                ViewState["SelectedRegDate"] = txtRegDateFilter.Text;

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
            ddlApproveFilter.SelectedIndex = 0;
            lstStoreFilter.ClearSelection();
            item.SelectedIndex = 0;
            vendor.SelectedIndex = 0;
            txtRegDateFilter.Text = string.Empty;

            filterStore.Checked = false;
            filterItem.Checked = false;
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
            ViewState["FilterRegDateChecked"] = null;
            ViewState["SelectedRegDate"] = null;
            ViewState["FilterStaffChecked"] = null;
            ViewState["SelectedStaff"] = null;

            ScriptManager.RegisterStartupScript(this, GetType(), "redirect",
                "window.location.href = 'approval.aspx';", true);

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
                     WHERE (status NOT IN ('Reorder Added', 'No Reorder Added') OR status IS NULL) AND (approved != 'approved')
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

        protected void btnApproveSelected_Click(object sender, EventArgs e)
        {
            try
            {
                string selectedIDs = hfSelectedIDs.Value;
                string userName = Session["username"].ToString();

                if (string.IsNullOrEmpty(selectedIDs))
                {
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "alert",
                        "Swal.fire('Error!', 'Please select at least one record!', 'error');", true);
                    return;
                }

                string[] ids = selectedIDs.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                using (SqlConnection conn = new SqlConnection(strcon))
                {
                    conn.Open();
                    foreach (string idStr in ids)
                    {
                        int id = Convert.ToInt32(idStr);
                        string updateQuery = @"
                            UPDATE itemListR 
                            SET approved = @approve, 
                            approver = @approver, 
                            completedDate = @completedDate 
                            WHERE id = @id";

                        using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@approve", "approved");
                            cmd.Parameters.AddWithValue("@approver", userName);
                            cmd.Parameters.AddWithValue("@completedDate", DateTime.Now.Date);
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    conn.Close();
                }

                hfSelectedIDs.Value = string.Empty;
                BindGrid();

                ScriptManager.RegisterStartupScript(this, this.GetType(), "alertRedirect",
                    "Swal.fire('Success!', 'Selected records have been updated successfully!', 'success')" +
                    ".then(function() { window.location = 'approval.aspx'; });", true);
            }
            catch (Exception ex)
            {
                string safeMessage = HttpUtility.JavaScriptStringEncode("Error: " + ex.Message);
                ScriptManager.RegisterStartupScript(this, GetType(), "alert",
                    $"Swal.fire('Error!', '{safeMessage}', 'error');", true);
            }
        }

        protected void btnDeclineSelected_Click(object sender, EventArgs e)
        {
            try
            {
                string selectedIDs = hfSelectedIDs.Value;
                string userName = Session["username"].ToString();

                if (string.IsNullOrEmpty(selectedIDs))
                {
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "alert",
                        "Swal.fire('Error!', 'Please select at least one record!', 'error');", true);
                    return;
                }

                string[] ids = selectedIDs.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                using (SqlConnection conn = new SqlConnection(strcon))
                {
                    conn.Open();
                    foreach (string idStr in ids)
                    {
                        int id = Convert.ToInt32(idStr);
                        string updateQuery = "UPDATE itemListR SET approved = @approve, approver = @approver WHERE id = @id";

                        using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@approve", "declined");
                            cmd.Parameters.AddWithValue("@approver", userName);
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                hfSelectedIDs.Value = string.Empty;
                BindGrid();

                ScriptManager.RegisterStartupScript(this, this.GetType(), "alertRedirect",
                    "Swal.fire('Success!', 'Selected records have been declined successfully!', 'success')" +
                    ".then(function() { window.location = 'approval.aspx'; });", true);

            }
            catch (Exception ex)
            {
                string safeMessage = HttpUtility.JavaScriptStringEncode("Error: " + ex.Message);
                ScriptManager.RegisterStartupScript(this, GetType(), "alert",
                    $"Swal.fire('Error!', '{safeMessage}', 'error');", true);
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
                ShowAlert("Unauthorized", "You do not have permission to access Expiry List", "error");
                return new DataTable();
            }

            var userStores = Session["storeListRaw"] as List<string> ?? new List<string>();
            bool hasHO = userStores.Any(s => s.Equals("HO", StringComparison.OrdinalIgnoreCase));

            StringBuilder query = new StringBuilder(@"
                     SELECT * FROM itemListR 
                     WHERE (status NOT IN ('Reorder Added', 'No Reorder Added') OR status IS NULL) AND  (approved != 'approved')");
                List<SqlParameter> parameters = new List<SqlParameter>();

            // STORE FILTER
            if (filterStore.Checked)
            {
                var selectedStores = lstStoreFilter.Items.Cast<ListItem>()
                    .Where(li => li.Selected && li.Value != "all")
                    .Select(li => li.Value)
                    .ToList();

                List<string> filteredStores = hasHO ? selectedStores : selectedStores.Intersect(userStores).ToList();

                if (filteredStores.Count > 0)
                {
                    string storePlaceholders = string.Join(",", filteredStores.Select((s, i) => $"@Store{i}"));
                    query.Append($" AND storeNo IN ({storePlaceholders})");

                    for (int i = 0; i < filteredStores.Count; i++)
                        parameters.Add(new SqlParameter($"@Store{i}", filteredStores[i]));
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
                    string storePlaceholders = string.Join(",", userStores.Select((s, i) => $"@UserStore{i}"));
                    query.Append($" AND storeNo IN ({storePlaceholders})");

                    for (int i = 0; i < userStores.Count; i++)
                        parameters.Add(new SqlParameter($"@UserStore{i}", userStores[i]));
                }
                else
                {
                    query.Append(" AND 1=0"); // No stores assigned
                }
            }

            // ITEM FILTER
            if (filterItem.Checked && !string.IsNullOrEmpty(item.SelectedValue))
            {
                query.Append(" AND itemNo = @ItemNo");
                parameters.Add(new SqlParameter("@ItemNo", item.SelectedValue));
            }

            // VENDOR FILTER
            if (filterVendor.Checked && !string.IsNullOrEmpty(vendor.SelectedValue))
            {
                query.Append(" AND vendorNo = @VendorNo");
                parameters.Add(new SqlParameter("@VendorNo", vendor.SelectedValue));
            }

            // REGISTRATION DATE FILTER
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

            // Fill DataTable
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