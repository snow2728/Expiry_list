
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using System.Web.UI;


namespace Expiry_list
{
    public class Global : System.Web.HttpApplication
    {

        protected void Application_Start(object sender, EventArgs e)
        {
            // Register jQuery from the CDN
            ScriptManager.ScriptResourceMapping.AddDefinition("jquery", new ScriptResourceDefinition
            {
                Path = "https://code.jquery.com/jquery-3.6.0.min.js",
                DebugPath = "https://code.jquery.com/jquery-3.6.0.js",
                CdnSupportsSecureConnection = true
            });
        }


        protected void Session_Start(object sender, EventArgs e)
        {
            Session["Init"] = true;
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {

        }

        // Global.asax
        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {
            HttpCookie authCookie = Request.Cookies[FormsAuthentication.FormsCookieName];
            if (authCookie != null)
            {
                try
                {
                    FormsAuthenticationTicket ticket = FormsAuthentication.Decrypt(authCookie.Value);
                    if (ticket != null && !ticket.Expired)
                    {
                        FormsIdentity identity = new FormsIdentity(ticket);

                        string username = ticket.Name;

                        GenericPrincipal principal = new GenericPrincipal(identity, new string[] { });

                        HttpContext.Current.User = principal;
                        System.Threading.Thread.CurrentPrincipal = principal;
                    }
                }
                catch
                {
                    FormsAuthentication.SignOut();
                    Response.Redirect(FormsAuthentication.LoginUrl);
                }
            }
        }

        protected void Application_Error(object sender, EventArgs e)
        {

        }

        protected void Session_End(object sender, EventArgs e)
        {
            //Response.Redirect("~/loginPage.aspx");
        }

        protected void Application_EndRequest(Object sender, EventArgs e)
        {
            if (Response.StatusCode == 302 && Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                Response.Clear();
                Response.StatusCode = 401;
                Response.ContentType = "application/json";
                Response.Write("{\"error\":\"Session expired\"}");
                Response.End();
            }
        }

    }
}