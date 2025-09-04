
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Expiry_list
{
    public partial class Site1 : System.Web.UI.MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetExpires(DateTime.Now.AddSeconds(-1));
            Response.Cache.SetNoStore();
            if (!IsPostBack)
            {
                if (!Request.Path.EndsWith("loginPage.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    if (Session["username"] != null)
                    {
                        // Display the username and logout panel
                        usernameSpan.Text = Session["username"].ToString();
                        usernameSpan.Visible = true;
                        panel1.Visible = true;
                        tabs.Visible = true;
                    }
                    else
                    {
                        Response.Redirect("loginPage.aspx");
                        usernameSpan.Visible = false;
                        panel1.Visible = false;
                        tabs.Visible = false;
                    }
                }
                else
                {
                    // On the login page, hide these elements
                    usernameSpan.Visible = false;
                    panel1.Visible = false;
                    tabs.Visible = false;
                }
                if (Request.Url.AbsolutePath == "/" || Request.Url.AbsolutePath == "" || Request.Url.AbsolutePath == "/loginPage.aspx")
                {
                    div_content_container.Style["margin-top"] = "0px";
                    div_content_container.Style["margin-bottom"] = "0px";
                    string currentClass = div_content_container.Attributes["class"] ?? "";
                    div_content_container.Attributes["class"] = currentClass.Replace("pt-5", "").Trim();
                }
                else if (Request.Url.AbsolutePath == "/AdminDashboard.aspx")
                {
                    btn_navbar.Style["display"] = "none";                    
                }
            }
        }

        protected void LogoutUser_Click(object sender, EventArgs e)
        {
            // Sign out and clear session
            FormsAuthentication.SignOut();

            // Clear all session variables
            Session.Clear();
            Session.RemoveAll();
            Session["ItemLines"] = null;

            Session.Abandon();

            // Remove session cookie
            if (Request.Cookies["ASP.NET_SessionId"] != null)
            {
                HttpCookie sessionCookie = new HttpCookie("ASP.NET_SessionId", "");
                sessionCookie.Expires = DateTime.Now.AddYears(-1);
                Response.Cookies.Add(sessionCookie);
            }

            // Remove authentication cookie
            HttpCookie authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, "");
            authCookie.Expires = DateTime.Now.AddYears(-1); 
            Response.Cookies.Add(authCookie);

            // Output session data before redirecting (debugging)
            DisplaySessionData();

            // Redirect to login page
            Response.Redirect("loginPage.aspx");

            // Make sure to complete the request
            Context.ApplicationInstance.CompleteRequest();
        }

        private void DisplaySessionData()
        {
            // You can use this to inspect the session contents before redirecting (for debugging purposes)
            foreach (string key in Session.Keys)
            {
                Debug.WriteLine($"Session Key: {key}, Value: {Session[key]}");
            }
        }

        protected void Page_PreRender(object sender, EventArgs e)
        {
            // Avoid setting headers if we're exporting
            if (!HttpContext.Current.Response.ContentType.StartsWith("application/vnd.openxmlformats-officedocument"))
            {
            Response.Headers.Add("Content-Security-Policy", "frame-ancestors 'none'");
            Response.Headers.Add("X-Frame-Options", "DENY");
            Response.Headers.Add("Referrer-Policy", "no-referrer");
            }
        }

    }
}