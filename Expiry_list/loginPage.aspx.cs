<<<<<<< HEAD
﻿
using System;
=======
﻿using System;
>>>>>>> dd28a8dd26355ac93475b3760a0023853d81994b
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Security;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace Expiry_list
{
    public partial class loginPage : System.Web.UI.Page
    {
        string strcon = ConfigurationManager.ConnectionStrings["con"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetExpires(DateTime.Now.AddSeconds(-1));
            Response.Cache.SetNoStore();

        }

        protected void Unnamed1_Click(object sender, EventArgs e)
        {
            try
            {
<<<<<<< HEAD
                string inputUsername = usernameTextBox.Text.Trim();
                string inputPassword = passwordTextBox.Text.Trim();

                using (SqlConnection con = new SqlConnection(strcon))
                {
                    con.Open();

                    // 1. Check username/password and IsEnabled
                    string query = "SELECT id, username, IsEnabled FROM users WHERE username = @username AND password = @password";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@username", inputUsername);
                        cmd.Parameters.AddWithValue("@password", inputPassword);

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            if (!dr.Read())
                            {
                                ClientScript.RegisterStartupScript(this.GetType(), "alert",
                                    "swal('Error!', 'Invalid Username or Password!', 'error');", true);
                                clearForm();
                                return;
                            }

                            bool isEnabled = dr["IsEnabled"] != DBNull.Value && Convert.ToBoolean(dr["IsEnabled"]);
                            if (!isEnabled)
                            {
                                ClientScript.RegisterStartupScript(this.GetType(), "alert",
                                    "swal('Access Denied!', 'This user account has been disabled.', 'warning');", true);
                                clearForm();
                                return;
                            }

                            int userId = Convert.ToInt32(dr["id"]);
                            string username = dr["username"].ToString().Trim();

                            Session["id"] = userId;
                            Session["username"] = username;

                            dr.Close();

                            // 2. Get assigned stores
                            List<string> storeList = new List<string>();
                            using (SqlCommand storeCmd = new SqlCommand("SELECT storeNo FROM UserStores WHERE userId = @userId", con))
                            {
                                storeCmd.Parameters.AddWithValue("@userId", userId);
                                using (SqlDataReader storeDr = storeCmd.ExecuteReader())
                                {
                                    while (storeDr.Read())
                                    {
                                        storeList.Add(storeDr["storeNo"].ToString().Trim());
                                    }
                                }
                            }

                            Session["storeNoList"] = string.Join(",", storeList);
                            Session["storeListRaw"] = storeList;

                            // 3. Get form permissions
                            Dictionary<string, string> formPermissions = new Dictionary<string, string>();
                            using (SqlCommand permCmd = new SqlCommand(@"
                               SELECT f.name AS FormName,
                               CASE up.permission_level 
                                    WHEN 1 THEN 'view' 
                                    WHEN 2 THEN 'edit' 
                                    WHEN 3 THEN 'admin'
                                    WHEN 4 THEN 'super'
                                    ELSE 'none' 
                                END AS Permission
                                FROM UserPermissions up
                                JOIN Forms f ON up.form_id = f.id
                                WHERE up.user_id = @userId", con))
                            {
                                permCmd.Parameters.AddWithValue("@userId", userId);
                                using (SqlDataReader permDr = permCmd.ExecuteReader())
                                {
                                    while (permDr.Read())
                                    {
                                        string form = permDr["FormName"].ToString();
                                        string permission = permDr["Permission"].ToString();
                                        formPermissions[form] = permission;
                                    }
                                }
                            }

                            Session["formPermissions"] = formPermissions;

                            // 4. Create auth ticket
                            FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(
                                1,
                                username,
                                DateTime.Now,
                                DateTime.Now.AddMinutes(60),
                                false,
                                "",
                                FormsAuthentication.FormsCookiePath
                            );

                            string encryptedTicket = FormsAuthentication.Encrypt(ticket);
                            HttpCookie authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket)
                            {
                                HttpOnly = true,
                                Secure = FormsAuthentication.RequireSSL,
                                Path = FormsAuthentication.FormsCookiePath,
                                Domain = FormsAuthentication.CookieDomain
                            };
                            if (ticket.IsPersistent) authCookie.Expires = ticket.Expiration;
                            Response.Cookies.Add(authCookie);

                            string returnUrl = Request.QueryString["ReturnUrl"];
                            Response.Redirect(string.IsNullOrEmpty(returnUrl) ? "AdminDashboard.aspx" : returnUrl);
=======
                using (SqlConnection con = new SqlConnection(strcon))
                {
                    con.Open();
                    string query = "SELECT * FROM users WHERE LOWER(RTRIM(LTRIM(username))) LIKE LOWER('%' + @username + '%') AND LOWER(RTRIM(LTRIM(password))) = LOWER(@password)";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@username", usernameTextBox.Text.Trim().ToLower());
                        cmd.Parameters.AddWithValue("@password", passwordTextBox.Text.Trim().ToLower());

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            if (dr.HasRows)
                            {
                                while (dr.Read())
                                {
                                    string id = dr["id"].ToString().Trim();
                                    string username = dr["username"].ToString().Trim();
                                    string role = dr["role"].ToString().Trim().ToLower();
                                    string storeNo = dr["storeNo"]?.ToString().Trim() ?? string.Empty;

                                    // Store user details in session variables
                                    Session["id"] = id;
                                    Session["username"] = username;
                                    Session["role"] = role;
                                    Session["storeNo"] = storeNo;

                                    // Check if a ReturnUrl exists
                                    string returnUrl = Request.QueryString["ReturnUrl"];
                                    string redirectUrl = string.IsNullOrEmpty(returnUrl) ? "" : returnUrl;

                                    // If there's no ReturnUrl, go to a default based on role
                                    if (string.IsNullOrEmpty(redirectUrl))
                                    {
                                        switch (role)
                                        {
                                            case "user":
                                                redirectUrl = "AdminDashboard.aspx";
                                                break;
                                            case "viewer":
                                                redirectUrl = "AdminDashboard.aspx";
                                                break;
                                            case "admin":
                                                redirectUrl = "AdminDashboard.aspx";
                                                break;
                                            default:
                                                redirectUrl = "~/loginForm.aspx";
                                                ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
                                                    "swal('Error!', 'Role not recognized!', 'error');", true);
                                                return;
                                        }
                                    }

                                    FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(
                                        1,
                                        username,
                                        DateTime.Now,
                                        DateTime.Now.AddMinutes(2880),
                                        false,
                                        role,
                                        FormsAuthentication.FormsCookiePath
                                    );
                                    string encryptedTicket = FormsAuthentication.Encrypt(ticket);
                                    HttpCookie authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);
                                    Response.Cookies.Add(authCookie);

                                    // If you rely on Session for UI, do NOT set Session["role"] = null here
                                    Response.Redirect(redirectUrl);

                                }
                            }
                            else
                            {
                                ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
                                    "swal('Error!', 'Invalid Username or Password!', 'error');", true);
                                clearForm();
                            }
>>>>>>> dd28a8dd26355ac93475b3760a0023853d81994b
                        }
                    }
                }
            }
            catch (Exception ex)
            {
<<<<<<< HEAD
                ClientScript.RegisterStartupScript(this.GetType(), "alert",
                    "swal('Error!', 'An unexpected error occurred. Please try again later.', 'error');", true);
=======
                ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
                    "swal('Error!', 'An unexpected error occurred. Please try again later.','error');", true);
>>>>>>> dd28a8dd26355ac93475b3760a0023853d81994b
                clearForm();
            }
        }

<<<<<<< HEAD
=======
        // Clears the form after login attempt
>>>>>>> dd28a8dd26355ac93475b3760a0023853d81994b
        private void clearForm()
        {
            usernameTextBox.Text = string.Empty;
            passwordTextBox.Text = string.Empty;
        }

    }
}
<<<<<<< HEAD
=======

>>>>>>> dd28a8dd26355ac93475b3760a0023853d81994b
