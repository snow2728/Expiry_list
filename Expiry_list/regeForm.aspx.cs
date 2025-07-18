using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
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
        }

        protected void btnRegister_Click(object sender, EventArgs e)
        {
            try
            {
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
                    }
                }
            }
            catch (Exception ex)
            {
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
                        }
                    }
                }
            }
        }

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
                string query = "SELECT id, storeNo FROM stores where storeNo IS NOT NULL AND storeNo <> '' ORDER BY storeNo ";
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

        private void ClearForm()
        {
            usernameTextBox.Text = "";
            passwordTextBox.Text = "";

            // Clear store selections
            foreach (ListItem item in lstStoreFilter.Items)
                item.Selected = false;

            ScriptManager.RegisterStartupScript(this, GetType(), "ResetPills",
                "resetLocationPills();", true);
        }

        private void BindGrid()
        {
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

            using (SqlConnection con = new SqlConnection(strcon))
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                }
            }
        }

        protected string MaskPassword(object password)
        {
            if (password == null)
                return string.Empty;

            int length = password.ToString().Length;
            return new string('*', length);
        }
    }

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

