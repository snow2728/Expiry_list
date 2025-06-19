using System;
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
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
                    "swal('Error!', 'An unexpected error occurred. Please try again later.','error');", true);
                clearForm();
            }
        }

        // Clears the form after login attempt
        private void clearForm()
        {
            usernameTextBox.Text = string.Empty;
            passwordTextBox.Text = string.Empty;
        }

    }
}

