using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Expiry_list
{
    public partial class AdminDashboard : System.Web.UI.Page
    {
        string strcon = ConfigurationManager.ConnectionStrings["con"].ConnectionString;
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Session["formPermissions"] == null || !User.Identity.IsAuthenticated)
                {
                    Response.Redirect("~/loginPage.aspx");
                }

                string username = Session["username"]?.ToString();
                if (string.IsNullOrEmpty(username)) return;

                Dictionary<string, string> permissions = GetAllowedFormsByUser(username);

                if (permissions.TryGetValue("ExpiryList", out string expiryPerm))
                    pnlExpiryList.Style["display"] = "block";

                if (permissions.TryGetValue("NegativeInventory", out string negativePerm))
                    pnlNegativeInventory.Style["display"] = "block";

                if (permissions.TryGetValue("SystemSettings", out string settingsPerm))
                    pnlSystemSettings.Style["display"] = "block";

                if (permissions.TryGetValue("CarWay", out string carPerm))
                    pnlCarWayPlan.Style["display"] = "block";

                if (permissions.TryGetValue("ReorderQuantity", out string reorderPerm))
                    pnlReorderQuantity.Style["display"] = "block";

                if (permissions.TryGetValue("ConsignmentList", out string consignPerm))
                    pnlConsignmentList.Style["display"] = "block";

                if (permissions.TryGetValue("TrainingList", out string trainingPerm))
                    pnlScheduleList.Style["display"] = "block";

            }
        }

        protected void el_Click1(object sender, EventArgs e)
        {
            if (!User.Identity.IsAuthenticated)
            {
                Response.Redirect("loginPage.aspx");
                return;
            }

            string username = User.Identity.Name;
            var allowedForms = GetAllowedFormsByUser(username);

            var permissions = Session["formPermissions"] as Dictionary<string, string>;
            string perm = permissions["ExpiryList"];
            List<string> storeNos = GetLoggedInUserStoreNames();
            bool isAdmin = perm.Equals("admin", StringComparison.OrdinalIgnoreCase);
            bool needsStoreFilter = !isAdmin || !storeNos.Any(s => s.Equals("HO", StringComparison.OrdinalIgnoreCase));

            Session["formPermissions"] = allowedForms;
            Session["activeModule"] = "ExpiryList";

          

            string redirectUrl = null;

            if (allowedForms.ContainsKey("ExpiryList") && (perm == "edit" || perm == "admin" ))
            {
                redirectUrl = "registrationForm.aspx";
            }
            else if (allowedForms.ContainsKey("ExpiryList") && perm == "super" && !needsStoreFilter)
            {
                redirectUrl = "final2.aspx";
            }
            else if ((allowedForms.ContainsKey("ExpiryList") && perm == "view") || (allowedForms.ContainsKey("ExpiryList") && perm == "super1") )
            {
                redirectUrl = "itemList.aspx";
            }
            else
            {
                redirectUrl = "AdminDashboard.aspx";
            }

            if (!string.IsNullOrEmpty(redirectUrl))
            {
                Response.Redirect(redirectUrl);
            }
            else
            {
                ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
                    "swal('Access Denied!', 'You do not have permission to access any valid page.', 'error');", true);
            }
        }

        protected void ni_Click1(object sender, EventArgs e)
        {
            if (!User.Identity.IsAuthenticated)
            {
                Response.Redirect("~/loginPage.aspx");
                return;
            }

            string username = User.Identity.Name;
            var allowedForms = GetAllowedFormsByUser(username);
            Session["formPermissions"] = allowedForms;
            Session["activeModule"] = "NegativeInventory";

            string redirectUrl = null;

            if (allowedForms.ContainsKey("NegativeInventory"))
            {
                redirectUrl = "balanceQty.aspx";
            }
            else
            {
                redirectUrl = "AdminDashboard.aspx";
            }

            if (!string.IsNullOrEmpty(redirectUrl))
            {
                Response.Redirect(redirectUrl);
            }
            else
            {
                ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
                    "swal('Access Denied!', 'You do not have permission to access any valid page.', 'error');", true);
            }
        }

        protected void ss_Click1(object sender, EventArgs e)
        {
            if (!User.Identity.IsAuthenticated)
            {
                FormsAuthentication.RedirectToLoginPage();
                return;
            }

            string username = User.Identity.Name;
            var allowedForms = GetAllowedFormsByUser(username);

            Session["formPermissions"] = allowedForms;
            Session["activeModule"] = "SystemSettings";

            var permissions = Session["formPermissions"] as Dictionary<string, string>;
            string perm = permissions["SystemSettings"];

            string redirectUrl = null;

            if (allowedForms.ContainsKey("SystemSettings") &&  perm == "admin")
            {
                redirectUrl = "regeForm1.aspx";
            }
            else
            {
                redirectUrl = "AdminDashboard.aspx";
            }

            if (!string.IsNullOrEmpty(redirectUrl))
            {
                Response.Redirect(redirectUrl);
            }
            else
            {
                ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
                    "swal('Access Denied!', 'You do not have permission to access any valid page.', 'error');", true);
            }
        }

        protected void cw_Click1(object sender, EventArgs e)
        {
            if (!User.Identity.IsAuthenticated)
            {
                Response.Redirect("~/loginPage.aspx");
                return;
            }

            string username = User.Identity.Name;
            var allowedForms = GetAllowedFormsByUser(username);

            Session["formPermissions"] = allowedForms;
            Session["activeModule"] = "CarWay";

            string redirectUrl = null;

            if (allowedForms.ContainsKey("CarWay"))
            {
                redirectUrl = "~/CarWay/main1.aspx";
            }
            else
            {
                redirectUrl = "~/AdminDashboard.aspx";
            }

            if (!string.IsNullOrEmpty(redirectUrl))
            {
                Response.Redirect(redirectUrl);
            }
            else
            {
                ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
                    "swal('Access Denied!', 'You do not have permission to access any valid page.', 'error');", true);
            }
        }

        protected void rq_Click1(object sender, EventArgs e)
        {
            if (!User.Identity.IsAuthenticated)
            {
                Response.Redirect("~/loginPage.aspx");
                return;
            }

            string username = User.Identity.Name;
            var allowedForms = GetAllowedFormsByUser(username);

            Session["formPermissions"] = allowedForms;
            Session["activeModule"] = "ReorderQuantity";

            var permissions = Session["formPermissions"] as Dictionary<string, string>;
            string perm = permissions["ReorderQuantity"];

            string redirectUrl = null;

            if (allowedForms.ContainsKey("ReorderQuantity") && (perm == "edit" || perm == "admin"))
            {
                redirectUrl = "~/ReorderForm/rege1.aspx";
            }
            else if (allowedForms.ContainsKey("ReorderQuantity") && perm == "view")
            {
                redirectUrl = "~/ReorderForm/viewer1.aspx";
            }
            else if (allowedForms.ContainsKey("ReorderQuantity") && (perm == "admin" || perm == "super"))
            {
                redirectUrl = "~/ReorderForm/approval.aspx";
            }
            else
            {
                redirectUrl = "~/AdminDashboard.aspx";
            }

            if (!string.IsNullOrEmpty(redirectUrl))
            {
                Response.Redirect(redirectUrl);
            }
            else
            {
                ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
                    "swal('Access Denied!', 'You do not have permission to access any valid page.', 'error');", true);
            }
        }

        protected void cl_Click1(object sender, EventArgs e)
        {
            if (!User.Identity.IsAuthenticated)
            {
                Response.Redirect("~/loginPage.aspx");
                return;
            }

            string username = User.Identity.Name;
            var allowedForms = GetAllowedFormsByUser(username);

            Session["formPermissions"] = allowedForms;
            Session["activeModule"] = "ConsignmentList";

            var permissions = Session["formPermissions"] as Dictionary<string, string>;
            string perm = permissions["ConsignmentList"];

            string redirectUrl = null;

            if (allowedForms.ContainsKey("ConsignmentList") && (perm == "edit" || perm == "admin"))
            {
                redirectUrl = "~/ConsignItem/rege1.aspx";
            }
            else if (allowedForms.ContainsKey("ConsignmentList") && perm == "view")
            {
                redirectUrl = "~/ConsignItem/viewer1.aspx";
            }
            else
            {
                redirectUrl = "~/AdminDashboard.aspx";
        }

            if (!string.IsNullOrEmpty(redirectUrl))
            {
                Response.Redirect(redirectUrl);
            }
            else
            {
                ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
                    "swal('Access Denied!', 'You do not have permission to access any valid page.', 'error');", true);
            }
        }

        protected void tl_Click1(object sender, EventArgs e)
        {
            if (!User.Identity.IsAuthenticated)
            {
                Response.Redirect("~/loginPage.aspx");
                return;
            }

            string username = User.Identity.Name;
            var allowedForms = GetAllowedFormsByUser(username);

            Session["formPermissions"] = allowedForms;
            Session["activeModule"] = "TrainingList";

            var permissions = Session["formPermissions"] as Dictionary<string, string>;
            string perm = permissions["TrainingList"];

            string redirectUrl = null;

            if (allowedForms.ContainsKey("TrainingList") && (perm == "edit" || perm == "admin"))
            {
                redirectUrl = "~/Training/rege1.aspx";
            }
            else if (allowedForms.ContainsKey("TrainingList") && perm == "view")
            {
                redirectUrl = "~/Training/viewer1.aspx";
            }
            else
            {
                redirectUrl = "~/AdminDashboard.aspx";
            }

            if (!string.IsNullOrEmpty(redirectUrl))
            {
                Response.Redirect(redirectUrl);
            }
            else
            {
                ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
                    "swal('Access Denied!', 'You do not have permission to access any valid page.', 'error');", true);
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
                        WHEN 5 THEN 'super1'
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

    }
}


