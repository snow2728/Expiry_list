using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Script.Services;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;
using DocumentFormat.OpenXml.Math;
using Newtonsoft.Json;

namespace Expiry_list
{
    public partial class regeForm1 : System.Web.UI.Page
    {
        string strcon = ConfigurationManager.ConnectionStrings["con"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {

            if (!IsPostBack)
            {
                BindUserGrid();
            }
        }

        private void BindUserGrid()
        {
            using (var conn = new SqlConnection(strcon))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT
                        u.id,
                        u.username,
                        u.isEnabled,
                        ISNULL((
                            SELECT STRING_AGG(ds.storeNo, ', ')
                            FROM (
                                SELECT DISTINCT s2.storeNo
                                FROM UserStores us2
                                JOIN Stores s2
                                  ON us2.storeId = s2.id
                                WHERE us2.userId = u.id
                            ) AS ds
                        ), '') AS StoreNos,
                        ISNULL((
                            SELECT STRING_AGG(f.name + ':' + pl.name, ', ')
                            FROM UserPermissions up
                            JOIN Forms f
                              ON up.form_id = f.id
                            JOIN PermissionLevels pl
                              ON up.permission_level = pl.id
                            WHERE up.user_id = u.id
                        ), '') AS Permissions
                    FROM Users u
                    ORDER BY u.id DESC;
                    ";

                conn.Open();
                using (var da = new SqlDataAdapter(cmd))
                using (var dt = new DataTable())
                {
                    da.Fill(dt);
                    GridView2.DataSource = dt;
                    GridView2.DataBind();
                }
            }
        }

        protected void GridView2_RowEditing(object sender, GridViewEditEventArgs e)
        {
            GridView2.EditIndex = e.NewEditIndex;
            BindUserGrid();
        }

        protected void GridView2_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            int userId = Convert.ToInt32(GridView2.DataKeys[e.RowIndex].Value);
            GridViewRow row = GridView2.Rows[e.RowIndex];

            try
            {
                //Retrieve basic user information
                string username = ((TextBox)row.FindControl("txtUsername")).Text.Trim();
                string password = ((TextBox)row.FindControl("txtPassword")).Text.Trim();
                bool isEnabled = ((CheckBox)row.FindControl("chkEnabled")).Checked;

                ListBox lstStores = (ListBox)row.FindControl("lstStores");
                var selectedStores = lstStores.Items
                    .Cast<ListItem>()
                    .Where(li => li.Selected)
                    .Select(li => (Id: int.Parse(li.Value), No: li.Text))
                    .ToList();

                //Retrieve permission settings
                var permissions = new Dictionary<string, string>
                {
                    {"ExpiryList", GetPermissionLevel(row, "Expiry")},
                    {"NegativeInventory", GetPermissionLevel(row, "Negative")},
                    {"SystemSettings", GetPermissionLevel(row, "System")},
                    {"CarWay", GetPermissionLevel(row, "CarWay")},
                    {"ReorderQuantity", GetPermissionLevel(row, "ReorderQuantity")},
                    {"ConsignmentList", GetPermissionLevel(row, "ConsignList") },
                    {"TrainingList", GetPermissionLevel(row, "TrainingList") },
                    {"DailyStatement", GetPermissionLevel(row, "DailyStatement") }
                };

                // Update database within transaction
                using (SqlConnection conn = new SqlConnection(strcon))
                {
                    conn.Open();
                    using (SqlTransaction trx = conn.BeginTransaction())
                    {
                        try
                        {
                            // Update user details
                            UpdateUserDetails(conn, trx, userId, username, password, isEnabled);

                            // Update store associations
                            UpdateUserStores(conn, trx, userId, selectedStores);

                            // Update permissions
                            UpdatePermissions(conn, trx, userId, permissions);

                            trx.Commit();
                        }
                        catch (Exception ex)
                        {
                            trx.Rollback();
                            throw new Exception("Transaction failed: " + ex.Message);
                        }
                    }
                }

                GridView2.EditIndex = -1;
                BindUserGrid();
            }
            catch (Exception ex)
            {
                ShowAlert("Error!", $"Unexpected error: {ex.Message}", "error");
            }
        }

        private string GetPermissionLevel(GridViewRow row, string prefix)
        {
            string checkboxId = $"chk{prefix}_Enable";
            CheckBox chkEnable = row.FindControl(checkboxId) as CheckBox;

            if (chkEnable == null || !chkEnable.Checked)
                return "None";

            if (((RadioButton)row.FindControl($"rdo{prefix}_View"))?.Checked == true)
                return "View";
            if (((RadioButton)row.FindControl($"rdo{prefix}_Edit"))?.Checked == true)
                return "Edit";
            if (((RadioButton)row.FindControl($"rdo{prefix}_Super"))?.Checked == true)
                return "Super";
            if (((RadioButton)row.FindControl($"rdo{prefix}_Super1"))?.Checked == true)
                return "Super1";

            return "Admin";
        }

        private void UpdateUserDetails(SqlConnection conn, SqlTransaction trx, int userId, string username, string password, bool isEnabled)
        {
            StringBuilder query = new StringBuilder(@"
                UPDATE Users 
                SET username = @username, 
                IsEnabled = @isEnabled");

            if (!string.IsNullOrEmpty(password))
            {
                query.Append(", password = @password");
            }

            query.Append(" WHERE id = @id");

            using (SqlCommand cmd = new SqlCommand(query.ToString(), conn, trx))
            {
                cmd.Parameters.AddWithValue("@username", username);
                cmd.Parameters.AddWithValue("@isEnabled", isEnabled);
                cmd.Parameters.AddWithValue("@id", userId);

                if (!string.IsNullOrEmpty(password))
                {
                    cmd.Parameters.AddWithValue("@password", password);
                }

                cmd.ExecuteNonQuery();
            }
        }

        private void UpdateUserStores(SqlConnection conn, SqlTransaction trx, int userId,List<(int Id, string No)> selectedStores)
        {
            // Delete existing associations
            using (SqlCommand cmd = new SqlCommand(
                "DELETE FROM UserStores WHERE userId = @userId", conn, trx))
            {
                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.ExecuteNonQuery();
            }

            // Insert new associations
            foreach (var store in selectedStores)
            {
                using (SqlCommand cmd = new SqlCommand(@"
                    INSERT INTO UserStores (userId, storeId, storeNo)
                    VALUES (@userId, @storeId, @storeNo)", conn, trx))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@storeId", store.Id);
                    cmd.Parameters.AddWithValue("@storeNo", store.No);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void UpdatePermissions(SqlConnection conn, SqlTransaction trx, int userId,Dictionary<string, string> permissions)
        {
            // Get form IDs
            var formIds = new Dictionary<string, int>();
            using (SqlCommand cmd = new SqlCommand("SELECT id, name FROM forms", conn, trx))
            {
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        formIds[reader["name"].ToString()] = Convert.ToInt32(reader["id"]);
                    }
                }
            }

            foreach (var perm in permissions)
            {
                string formName = perm.Key;
                string permLevel = perm.Value;

                if (!formIds.ContainsKey(formName))
                {
                    throw new Exception($"Form '{formName}' not found in database");
                }
                int formId = formIds[formName];

                // Delete existing permission
                using (SqlCommand cmd = new SqlCommand(@"
                    DELETE FROM UserPermissions 
                    WHERE user_id = @userId 
                    AND form_id = @formId", conn, trx))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@formId", formId);
                    cmd.ExecuteNonQuery();
                }

                // Insert new permission if not "None"
                if (permLevel != "None")
                {
                    // Map permission level to numeric value
                    int permLevelValue = MapPermissionLevel(permLevel);

                    using (SqlCommand cmd = new SqlCommand(@"
                        INSERT INTO UserPermissions (user_id, form_id, permission_level)
                        VALUES (@userId, @formId, @permLevel)", conn, trx))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        cmd.Parameters.AddWithValue("@formId", formId);
                        cmd.Parameters.AddWithValue("@permLevel", permLevelValue);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private int MapPermissionLevel(string permLevel)
        {
            switch (permLevel)
            {
                case "View": return 1;
                case "Edit": return 2;
                case "Admin": return 3;
                case "Super": return 4;
                case "Super1": return 5;
                default:
                    throw new ArgumentException($"Invalid permission level: {permLevel}");
            }
        }

        protected void GridView2_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow ||
                (e.Row.RowState & DataControlRowState.Edit) == 0) return;

            var lstStores = (ListBox)e.Row.FindControl("lstStores");
            if (lstStores == null) return;

            using (var cn = new SqlConnection(strcon))
            using (var cmd = new SqlCommand("SELECT id, storeNo FROM Stores ORDER BY storeNo", cn))
            {
                cn.Open();
                lstStores.DataSource = cmd.ExecuteReader();
                lstStores.DataValueField = "id";
                lstStores.DataTextField = "storeNo";
                lstStores.DataBind();
            }

            int userId = Convert.ToInt32(DataBinder.Eval(e.Row.DataItem, "id"));
            using (var con = new SqlConnection(strcon))
            using (var cmd = new SqlCommand("SELECT storeId FROM UserStores WHERE userId = @uid", con))
            {
                cmd.Parameters.AddWithValue("@uid", userId);
                con.Open();
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                    {
                        var li = lstStores.Items.FindByValue(rdr[0].ToString());
                        if (li != null) li.Selected = true;
                    }
            }

            string js = $@"
                $(function(){{
                    var $lb = $('#{lstStores.ClientID}');
                    if ($lb.hasClass('select2-hidden-accessible')) return;
                    $lb.show().select2({{ placeholder:'Select stores', width:'100%', closeOnSelect:false }});
                }});
            ";
            ScriptManager.RegisterStartupScript(this, GetType(), "sel2_" + lstStores.ClientID, js, true);

            var perms = GetUserPermissions(userId);

            var meta = new[]
            {
                new{ FormId = 1, CtrlPrefix = "Expiry"             },
                new{ FormId = 2, CtrlPrefix = "Negative"  },
                new{ FormId = 3, CtrlPrefix = "ReorderQuantity"    },
                new{ FormId = 4, CtrlPrefix = "System"             },
                new{ FormId = 6, CtrlPrefix = "CarWay"             },
                new{ FormId = 7, CtrlPrefix = "ConsignList"        },
                new{ FormId = 8, CtrlPrefix = "TrainingList"        },
                new{ FormId = 9, CtrlPrefix = "DailyStatement"        }
            };

            foreach (var m in meta)
            {
                bool hasPerm = perms.TryGetValue(m.FormId, out string level);

                CheckBox chkEnable = (CheckBox)e.Row.FindControl($"chk{m.CtrlPrefix}_Enable");
                if (chkEnable != null) chkEnable.Checked = hasPerm;

                foreach (string lv in new[] { "View", "Edit", "Admin", "Super", "Super1" })
                {
                    RadioButton rdo = (RadioButton)e.Row.FindControl($"rdo{m.CtrlPrefix}_{lv}");
                    if (rdo != null) rdo.Checked = hasPerm && level == lv;
                }
            }
        }

        private Dictionary<int, string> GetUserPermissions(int userId)
        {
            var dict = new Dictionary<int, string>();

            string LevelToText(byte b)
            {
                switch (b)
                {
                    case 1: return "View";
                    case 2: return "Edit";
                    case 3: return "Admin";
                    case 4: return "Super";
                    case 5: return "Super1";
                    default: return "";
                }
            }

            using (var con = new SqlConnection(strcon))
            using (var cmd = new SqlCommand("SELECT form_id, permission_level FROM UserPermissions WHERE user_id=@uid", con))
            {
                cmd.Parameters.AddWithValue("@uid", userId);
                con.Open();
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                    {
                        int formId = rdr.GetInt32(0);
                        string level = rdr[1] is byte b ? LevelToText(b) : rdr[1].ToString();
                        dict[formId] = level;
                    }
            }

            return dict;
        }


        [WebMethod, ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<StoreDto> GetStores(string searchTerm)
        {
            var list = new List<StoreDto>();
            string cs = ConfigurationManager.ConnectionStrings["con"].ConnectionString;

            using (var cn = new SqlConnection(cs))
            using (var cmd = new SqlCommand(
                  @"SELECT id, storeNo 
                  FROM   Stores
                  WHERE  @term='' OR storeNo LIKE '%'+@term+'%';", cn))
            {
                cmd.Parameters.AddWithValue("@term", searchTerm ?? "");
                cn.Open();
                using (var dr = cmd.ExecuteReader())
                    while (dr.Read())
                        list.Add(new StoreDto { id = dr.GetInt32(0), text = dr.GetString(1) });
            }
            return list;
        }

        public class StoreDto
        {
            public int id { get; set; }
            public string text { get; set; }
        }

        private void ShowAlert(string title, string message, string type)
        {
            ScriptManager.RegisterStartupScript(this, GetType(), "alert",
                $"swal('{title}', '{HttpUtility.JavaScriptStringEncode(message)}', '{type}');", true);
        }

        protected void GridView2_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            int userId = Convert.ToInt32(GridView2.DataKeys[e.RowIndex].Value);

            using (SqlConnection conn = new SqlConnection(strcon))
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    using (SqlCommand cmd = new SqlCommand(
                        "DELETE FROM UserStores WHERE userId = @userId", conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        cmd.ExecuteNonQuery();
                    }

                    using (SqlCommand cmd = new SqlCommand(
                        "DELETE FROM UserPermissions WHERE user_id = @userId", conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        cmd.ExecuteNonQuery();
                    }

                    using (SqlCommand cmd = new SqlCommand(
                        "DELETE FROM Users WHERE id = @userId", conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();

                    ScriptManager.RegisterStartupScript(
                        this, GetType(), "DeleteSuccess",
                        "Swal.fire('Deleted!', 'User deleted successfully.', 'success');", true);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    string safeMsg = HttpUtility.JavaScriptStringEncode(
                        "An error occurred while deleting user: " + ex.Message);

                    ScriptManager.RegisterStartupScript(
                        this, GetType(), "DeleteError",
                        $"Swal.fire('Error!', '{safeMsg}', 'error');", true);
                }
            }
            BindUserGrid();
        }

        protected void GridView2_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            GridView2.EditIndex = -1;
            BindUserGrid();
            Response.Redirect("regeForm1.aspx");
        }
    }
}