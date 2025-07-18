
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
<<<<<<< HEAD
            Session["Init"] = true;
=======
            
>>>>>>> dd28a8dd26355ac93475b3760a0023853d81994b
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
<<<<<<< HEAD
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
=======
                    // Decrypt the ticket
                    FormsAuthenticationTicket ticket = FormsAuthentication.Decrypt(authCookie.Value);
                    if (ticket != null && !ticket.Expired)
                    {
                        // Extract roles from UserData (comma-separated)
                        string[] roles = ticket.UserData.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                        // Create identity and principal
                        FormsIdentity identity = new FormsIdentity(ticket);
                        GenericPrincipal principal = new GenericPrincipal(identity, roles);

                        // Attach to current request
                        Context.User = principal;
                        HttpContext.Current.User = principal; 
                    }
                }
                catch (Exception ex)
>>>>>>> dd28a8dd26355ac93475b3760a0023853d81994b
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
            //Response.Redirect("loginPage.aspx");
        }


        protected void Application_End(object sender, EventArgs e)
        {

        }
    }
}