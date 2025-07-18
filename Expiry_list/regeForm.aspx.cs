using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
<<<<<<< HEAD
using System.Text;
=======
>>>>>>> dd28a8dd26355ac93475b3760a0023853d81994b
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Expiry_list
{
    public partial class regeForm : System.Web.UI.Page
    {
        string strcon = ConfigurationManager.ConnectionStrings["con"].ConnectionString;
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                BindStores();
                BindGrid();
            }
        }

        private void BindStores()
        {
<<<<<<< HEAD
            try
            {
                lstStoreFilter.Items.Clear();

                using (SqlConnection con = new SqlConnection(strcon))
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT DISTINCT id, LTRIM(RTRIM(storeNo)) AS storeNo 
                        FROM stores 
                        WHERE storeNo IS NOT NULL 
                        AND storeNo <> ''", con))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string storeNo = reader["storeNo"].ToString().Trim();
                            string storeId = reader["id"].ToString().Trim();

                            if (!string.IsNullOrEmpty(storeNo) && !string.IsNullOrEmpty(storeId))
                            {
                                lstStoreFilter.Items.Add(new ListItem(storeNo, storeId));
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
=======
            using (SqlConnection con = new SqlConnection(strcon))
            {
                string query = "SELECT id, storeNo FROM stores ORDER BY storeNo";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        storeTextBox.DataSource = reader;
                        storeTextBox.DataTextField = "storeNo";
                        storeTextBox.DataValueField = "id";
                        storeTextBox.DataBind();
                    }
                }
            }

            storeTextBox.Items.Insert(0, new ListItem("", ""));
>>>>>>> dd28a8dd26355ac93475b3760a0023853d81994b
        }

        protected void btnRegister_Click(object sender, EventArgs e)
        {
            try
            {
<<<<<<< HEAD
                string username = usernameTextBox.Text.Trim();
                string password = passwordTextBox.Text.Trim();
                
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    ShowAlert("Error!", "Username and password are required!", "error");
                    return;
                }

                using (SqlConnection con = new SqlConnection(strcon))
                {
                    con.Open();
                    SqlTransaction tran = con.BeginTransaction();

                    try
                    {
                        if (UserExists(con, tran, username))
                        {
                            ShowAlert("Duplicate!", "Username already exists", "warning");
                            return;
                        }

                        int userId;

                        if (!chkEnable.Checked)
                        {
                            userId = InsertUser(con, tran, username, password, "False");
                        }
                        else
                        {
                            userId = InsertUser(con, tran, username, password, "True");
                        }

                        // Store assignments
                        InsertUserStores(con, tran, userId);

                        InsertFormPermissions(con, tran, userId);

                        tran.Commit();
                        ShowAlert("Success!", "User registered successfully!", "success");
                        ClearForm();
                        BindGrid();
                        Response.Redirect("regeForm1.aspx");
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        ShowAlert("Error!", $"Registration failed: {ex.Message}", "error");
=======
                using (SqlConnection con = new SqlConnection(strcon))
                {
                    con.Open();
                    string query = "INSERT INTO users (username, password, role, storeNo) " +
                                   "VALUES (@username, @password, @role, @storeNo)";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        string role = roleTextBox.SelectedValue.ToLower();
                        cmd.Parameters.AddWithValue("@username", usernameTextBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@password", passwordTextBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@role", role);
                        cmd.Parameters.AddWithValue("@storeNo", storeTextBox.SelectedValue.ToLower());

                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            ScriptManager.RegisterStartupScript(this, this.GetType(), "alert",
                                "swal('Success!', 'User is registered successfully!', 'success');", true);

                            clearForm();
                            BindGrid();
                            UpdatePanel1.Update();
                        }
                        else
                        {
                            ScriptManager.RegisterStartupScript(this, this.GetType(), "alert",
                                "swal('Error!', 'Registration failed!', 'error');", true);
                        }
>>>>>>> dd28a8dd26355ac93475b3760a0023853d81994b
                    }
                }
            }
            catch (Exception ex)
            {
<<<<<<< HEAD
                ShowAlert("Error!", $"Unexpected error: {ex.Message}", "error");
            }
        }

        private void InsertFormPermissions(SqlConnection con, SqlTransaction tran, int userId)
        {
            var permissions = new List<(string formName, string permission)>();

            if (chkExpiryList_Enable.Checked)
            {
                string level = GetSelectedPermission(rdoExpiryList_View, rdoExpiryList_Edit, rdoExpiryList_Admin, rdoExpiryList_Super);
                permissions.Add(("ExpiryList", level));
            }

            if (chkNegativeInventory_Enable.Checked)
            {
                string level = GetSelectedPermission(rdoNegativeInventory_View, rdoNegativeInventory_Edit, rdoNegativeInventory_Admin, rdoNegativeInventory_Super);
                permissions.Add(("NegativeInventory", level));
            }

            if (chkSystemSettings_Enable.Checked)
            {
                string level = GetSelectedPermission(rdoSystemSettings_View, rdoSystemSettings_Edit, rdoSystemSettings_Admin, rdoSystemSettings_Super);
                permissions.Add(("SystemSettings", level));
            }

            if (chkCarWayPlan_Enable.Checked)
            {
                string level = GetSelectedPermission(rdoCarWayPlan_View, rdoCarWayPlan_Edit, rdoCarWayPlan_Admin, rdoCarWayPlan_Super);
                permissions.Add(("CarWay", level));
            }

            if (chkReorderQuantity_Enable.Checked)
            {
                string level = GetSelectedPermission(rdoReorderQuantity_View, rdoReorderQuantity_Edit, rdoReorderQuantity_Admin, rdoReorderQuantity_Super);
                permissions.Add(("ReorderQuantity", level));
            }

            foreach (var (formName, permission) in permissions)
            {
                int? formId = GetFormId(con, tran, formName);
                if (formId == null)
                {
                    continue;
                }

                using (SqlCommand cmd = new SqlCommand(@"
                    INSERT INTO UserPermissions (User_Id, form_id, Permission_Level)
                    VALUES (@UserId, @FormId, @PermissionLevel)", con, tran))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@FormId", formId);
                    cmd.Parameters.AddWithValue("@PermissionLevel", permission);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private int? GetFormId(SqlConnection con, SqlTransaction tran, string formName)
        {
            using (SqlCommand cmd = new SqlCommand("SELECT id FROM forms WHERE name = @FormName", con, tran))
            {
                cmd.Parameters.AddWithValue("@FormName", formName);
                object result = cmd.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : (int?)null;
            }
        }

        private string GetSelectedPermission(RadioButton view, RadioButton edit, RadioButton admin, RadioButton super)
        {
            if (super.Checked) return "4";
            if (admin.Checked) return "3";
            if (edit.Checked) return "2";
            if (view.Checked) return "1";
            return "None";
        }


        // Helper to show alerts
        private void ShowAlert(string title, string message, string type)
        {
            ScriptManager.RegisterStartupScript(this, GetType(), "alert",
                $"swal('{title}', '{HttpUtility.JavaScriptStringEncode(message)}', '{type}');", true);
        }
        private bool UserExists(SqlConnection con, SqlTransaction tran, string username)
        {
            string query = "SELECT COUNT(*) FROM users WHERE username = @username";
            using (SqlCommand cmd = new SqlCommand(query, con, tran))
            {
                cmd.Parameters.AddWithValue("@username", username);
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        private int InsertUser(SqlConnection con, SqlTransaction tran, string username, string password, string enable)
        {
            string query = @"INSERT INTO users (username, password, IsEnabled) 
                     OUTPUT INSERTED.id 
                     VALUES (@username, @password, @enable)";

            using (SqlCommand cmd = new SqlCommand(query, con, tran))
            {
                cmd.Parameters.AddWithValue("@username", username);
                cmd.Parameters.AddWithValue("@password", password);
                cmd.Parameters.AddWithValue("@enable", enable);
                return (int)cmd.ExecuteScalar();
            }
        }

        private void InsertUserStores(SqlConnection con, SqlTransaction tran, int userId)
        {
            // Handle "All Stores" selection
            bool allStoresSelected = lstStoreFilter.Items.Cast<ListItem>()
                .Any(li => li.Value == "all" && li.Selected);

            if (allStoresSelected)
            {
                string query = "INSERT INTO UserStores (userId, storeId, storeNo) " +
                               "SELECT @userId, id, storeNo FROM stores";

                using (SqlCommand cmd = new SqlCommand(query, con, tran))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
            else
            {
                foreach (ListItem item in lstStoreFilter.Items)
                {
                    if (item.Selected && item.Value != "all")
                    {
                        string query = "INSERT INTO UserStores (userId, storeId, storeNo) " +
                                       "VALUES (@userId, @storeId, @storeNo)";

                        using (SqlCommand cmd = new SqlCommand(query, con, tran))
                        {
                            cmd.Parameters.AddWithValue("@userId", userId);
                            cmd.Parameters.AddWithValue("@storeId", item.Value);
                            cmd.Parameters.AddWithValue("@storeNo", item.Text);
                            cmd.ExecuteNonQuery();
=======
                Response.Write("<script>alert('An error occurred: " + ex.Message + "');</script>");
                clearForm();
            }
        }

        protected void userGridView_RowEditing(object sender, GridViewEditEventArgs e)
        {
            userGridView.EditIndex = e.NewEditIndex;
            BindGrid();
        }

        protected void userGridView_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            userGridView.EditIndex = -1;
            BindGrid();
        }

        protected void userGridView_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            try
            {
                GridViewRow row = userGridView.Rows[e.RowIndex];

                string id = userGridView.DataKeys[e.RowIndex].Value.ToString();
                string username = ((TextBox)row.FindControl("txtUsername")).Text.Trim();
                string passwordInput = ((TextBox)row.FindControl("txtPassword")).Text.Trim();
                string role = ((DropDownList)row.FindControl("ddlEditRole")).SelectedValue;
                string storeNo = ((DropDownList)row.FindControl("ddlEditStore")).SelectedValue;
                bool isEnabled = ((CheckBox)row.FindControl("chkEditIsEnabled")).Checked;

                string passwordToUpdate = passwordInput;

                if (string.IsNullOrEmpty(passwordInput))
                {
                    using (SqlConnection con = new SqlConnection(strcon))
                    {
                        con.Open();
                        string selectQuery = "SELECT password FROM users WHERE id = @id";
                        using (SqlCommand selectCmd = new SqlCommand(selectQuery, con))
                        {
                            selectCmd.Parameters.AddWithValue("@id", id);
                            object result = selectCmd.ExecuteScalar();
                            if (result != null)
                            {
                                passwordToUpdate = result.ToString();
                            }
                            else
                            {
                                ClientScript.RegisterClientScriptBlock(this.GetType(), "alert", "swal('Error!', 'User not found.', 'error');", true);
                                return;
                            }
                        }
                    }
                }

                using (SqlConnection con = new SqlConnection(strcon))
                {
                    con.Open();
                    string updateQuery = "UPDATE users SET username = @username, password = @password, role = @role, storeNo = @storeNo, IsEnabled = @isEnabled WHERE id = @id";
                    using (SqlCommand updateCmd = new SqlCommand(updateQuery, con))
                    {
                        updateCmd.Parameters.AddWithValue("@id", id);
                        updateCmd.Parameters.AddWithValue("@username", username);
                        updateCmd.Parameters.AddWithValue("@password", passwordToUpdate);
                        updateCmd.Parameters.AddWithValue("@role", role);
                        updateCmd.Parameters.AddWithValue("@storeNo", storeNo);
                        updateCmd.Parameters.AddWithValue("@isEnabled", isEnabled);

                        updateCmd.ExecuteNonQuery();
                    }
                }

                userGridView.EditIndex = -1;
                BindGrid();
            }
            catch (Exception ex)
            {
                ClientScript.RegisterClientScriptBlock(this.GetType(), "alert", "swal('Error!', 'An unexpected error occurred during update. Please try again.', 'error');", true);
            }
        }

        protected void userGridView_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            string id = userGridView.DataKeys[e.RowIndex].Value.ToString();

            string query = "DELETE FROM Users WHERE id = @id";

            using (SqlConnection con = new SqlConnection(strcon))
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@id", id);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            BindGrid();
        }

        protected void userGridView_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow && userGridView.EditIndex == e.Row.RowIndex)
            {
                DropDownList ddlStore = (DropDownList)e.Row.FindControl("ddlEditStore");
                if (ddlStore != null)
                {
                    BindStoreDropDown(ddlStore);

                    // Get the current store name from the row
                    string currentStoreName = DataBinder.Eval(e.Row.DataItem, "StoreName")?.ToString();

                    // Set the selected value
                    if (!string.IsNullOrEmpty(currentStoreName))
                    {
                        ListItem selectedItem = ddlStore.Items.FindByText(currentStoreName);
                        if (selectedItem != null)
                        {
                            ddlStore.ClearSelection();
                            selectedItem.Selected = true;
>>>>>>> dd28a8dd26355ac93475b3760a0023853d81994b
                        }
                    }
                }
            }
        }

<<<<<<< HEAD
        private void InsertPermissions(SqlConnection con, SqlTransaction tran, int userId, List<FormPermission> permissions)
        {
            foreach (var permission in permissions)
            {
                if (permission.ViewLevel > PermissionLevel.None)
                {
                    string query = @"MERGE INTO UserPermissions AS target
                            USING (VALUES (@userId, @formName)) AS source (userId, formName)
                            ON target.userId = source.userId AND target.formName = source.formName
                            WHEN MATCHED THEN
                                UPDATE SET permissionLevel = @permissionLevel
                            WHEN NOT MATCHED THEN
                                INSERT (userId, formName, permissionLevel)
                                VALUES (source.userId, source.formName, @permissionLevel);";

                    using (SqlCommand cmd = new SqlCommand(query, con, tran))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        cmd.Parameters.AddWithValue("@formName", permission.FormName);
                        cmd.Parameters.AddWithValue("@permissionLevel", permission.ViewLevel);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }
=======
        protected void userGridView_DataBound(object sender, EventArgs e)
        {
            if (userGridView.PageCount > 1 && userGridView.BottomPagerRow != null)
            {
                LinkButton lnkPrev = (LinkButton)userGridView.BottomPagerRow.FindControl("lnkPrev");
                LinkButton lnkNext = (LinkButton)userGridView.BottomPagerRow.FindControl("lnkNext");

                if (lnkPrev != null)
                {
                    if (userGridView.PageIndex == 0)
                    {
                        lnkPrev.Enabled = false;
                        lnkPrev.CssClass = "btn btn-secondary m-1 disabled";
                    }
                    else
                    {
                        lnkPrev.Enabled = true;
                        lnkPrev.CssClass = "btn btn-secondary m-1";
                    }
                }

                if (lnkNext != null)
                {
                    if (userGridView.PageIndex == userGridView.PageCount - 1)
                    {
                        lnkNext.Enabled = false;
                        lnkNext.CssClass = "btn btn-secondary m-1 disabled";
                    }
                    else
                    {
                        lnkNext.Enabled = true;
                        lnkNext.CssClass = "btn btn-secondary m-1";
                    }
                }

                Label lblShowing = (Label)userGridView.BottomPagerRow.FindControl("lblShowing");
                if (lblShowing != null)
                {
                    int totalRows = GetTotalUsersCount();
                    int currentPage = userGridView.PageIndex;
                    int pageSize = userGridView.PageSize;
                    int startIndex = currentPage * pageSize + 1;
                    int endIndex = Math.Min((currentPage + 1) * pageSize, totalRows);

                    lblShowing.Text = $"Showing {startIndex} to {endIndex} of {totalRows} entries";
                }

            }
        }

>>>>>>> dd28a8dd26355ac93475b3760a0023853d81994b
        private int GetTotalUsersCount()
        {
            int total = 0;
            using (SqlConnection con = new SqlConnection(strcon))
            {
                string query = "SELECT COUNT(*) FROM Users";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    total = (int)cmd.ExecuteScalar();
                }
            }
            return total;
        }

        private void BindStoreDropDown(DropDownList ddlStore)
        {
            using (SqlConnection con = new SqlConnection(strcon))
            {
<<<<<<< HEAD
                string query = "SELECT id, storeNo FROM stores where storeNo IS NOT NULL AND storeNo <> '' ORDER BY storeNo ";
=======
                string query = "SELECT id, storeNo FROM stores ORDER BY storeNo";
>>>>>>> dd28a8dd26355ac93475b3760a0023853d81994b
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        ddlStore.DataSource = reader;
                        ddlStore.DataTextField = "storeNo";
                        ddlStore.DataValueField = "id";
                        ddlStore.DataBind();
                    }
                }
            }

            ddlStore.Items.Insert(0, new ListItem("", ""));
        }

<<<<<<< HEAD
        private void ClearForm()
        {
            usernameTextBox.Text = "";
            passwordTextBox.Text = "";

            // Clear store selections
            foreach (ListItem item in lstStoreFilter.Items)
                item.Selected = false;

            ScriptManager.RegisterStartupScript(this, GetType(), "ResetPills",
                "resetLocationPills();", true);
=======

        private void clearForm()
        {
            usernameTextBox.Text = string.Empty;
            passwordTextBox.Text = string.Empty;
            storeTextBox.SelectedValue = "";
            roleTextBox.SelectedValue = "";

>>>>>>> dd28a8dd26355ac93475b3760a0023853d81994b
        }

        private void BindGrid()
        {
<<<<<<< HEAD
            string query = @"
                    SELECT 
                        u.id,
                        u.username,
                        u.password,
                        u.isEnabled,
                        ISNULL((
                            SELECT STRING_AGG(storeNo, ', ')
                            FROM (
                                SELECT DISTINCT s.storeNo
                                FROM UserStores us2
                                INNER JOIN Stores s ON us2.storeId = s.id
                                WHERE us2.userId = u.id
                            ) AS DistinctStores
                        ), '') AS StoreNos,
                        ISNULL((
                            SELECT STRING_AGG(f.name + ':' + pl.name, ', ')
                            FROM UserPermissions up
                            INNER JOIN Forms f ON up.form_id = f.id
                            INNER JOIN PermissionLevels pl ON up.permission_level = pl.id
                            WHERE up.user_id = u.id
                        ), '') AS Permissions
                    FROM Users u
                    ORDER BY u.id DESC";
=======
            string query = "SELECT u.id, u.Username, u.Password, u.Role, s.StoreName, u.IsEnabled FROM Users u JOIN Stores s ON u.StoreNo = s.id order by u.id desc";
>>>>>>> dd28a8dd26355ac93475b3760a0023853d81994b

            using (SqlConnection con = new SqlConnection(strcon))
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
<<<<<<< HEAD

                    //userGridView.DataSource = dt;
                    //userGridView.DataBind();
=======
                    ViewState["UserData"] = dt;

                    userGridView.DataSource = dt;
                    userGridView.DataBind();
>>>>>>> dd28a8dd26355ac93475b3760a0023853d81994b
                }
            }
        }

<<<<<<< HEAD
=======
        protected void userGridView_Sorting(object sender, GridViewSortEventArgs e)
        {
            // Retrieve data from ViewState
            DataTable dt = ViewState["UserData"] as DataTable;
            if (dt != null)
            {
                DataView dv = new DataView(dt);

                // Toggle sorting direction
                if (ViewState["SortDirection"] == null || (string)ViewState["SortDirection"] == "ASC")
                {
                    dv.Sort = e.SortExpression + " DESC";
                    ViewState["SortDirection"] = "DESC";
                }
                else
                {
                    dv.Sort = e.SortExpression + " ASC";
                    ViewState["SortDirection"] = "ASC";
                }

                userGridView.DataSource = dv;
                userGridView.DataBind();
            }
        }

        protected void GridView2_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            userGridView.PageIndex = e.NewPageIndex;
            BindGrid();
        }

>>>>>>> dd28a8dd26355ac93475b3760a0023853d81994b
        protected string MaskPassword(object password)
        {
            if (password == null)
                return string.Empty;

            int length = password.ToString().Length;
            return new string('*', length);
        }
    }
<<<<<<< HEAD

    public class FormPermission
    {
        public string FormName { get; set; }
        public int ViewLevel { get; set; } // 0 = No access, 1 = View, 2 = Edit
    }

    public static class PermissionLevel
    {
        public const int None = 0;
        public const int View = 1;
        public const int Edit = 2;
    }

}

=======
}
>>>>>>> dd28a8dd26355ac93475b3760a0023853d81994b
