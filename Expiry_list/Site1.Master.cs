
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
                    usernameSpan.Visible = false;
                    panel1.Visible = false;
                    tabs.Visible = false;
                }

            }
        }

        protected void LogoutUser_Click(object sender, EventArgs e)
        {
            // Sign out and clear session
            FormsAuthentication.SignOut();

            Session.Clear();
            Session.RemoveAll();
            Session["ItemLines"] = null;

            Session.Abandon();

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

            DisplaySessionData();
            Response.Redirect("loginPage.aspx");

            Context.ApplicationInstance.CompleteRequest();
        }

        private void DisplaySessionData()
        {
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
