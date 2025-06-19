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
    public partial class WebForm1 : System.Web.UI.Page
    {
        string strcon = ConfigurationManager.ConnectionStrings["con"].ConnectionString;
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void el_Click1(object sender, EventArgs e)
        {
            if (!User.Identity.IsAuthenticated)
            {
                Response.Redirect("loginPage.aspx");
                return;
            }

            var identity = (FormsIdentity)User.Identity;
            string role = identity.Ticket.UserData.ToLower();

            string redirectUrl;
            switch (role)
            {
                case "admin":
                    redirectUrl = "registrationForm.aspx";
                    break;
                case "user":
                    redirectUrl = "registrationForm.aspx";
                    break;
                case "viewer":
                    redirectUrl = "itemList.aspx";
                    break;
                default:
                    ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
                        "swal('Error!', 'Unauthorized access!', 'error');", true);
                    return;
            }
            Response.Redirect(redirectUrl);
        }

        protected void ni_Click1(object sender, EventArgs e)
        {
            if (!User.Identity.IsAuthenticated)
            {
                Response.Redirect("loginPage.aspx");
                return;
            }

            var identity = (FormsIdentity)User.Identity;
            string role = identity.Ticket.UserData.ToLower();

            string redirectUrl;
            switch (role)
            {
                case "admin":
                    redirectUrl = "balanceQty.aspx";
                    break;
                case "user":
                    redirectUrl = "balanceQty.aspx";
                    break;
                case "viewer":
                    redirectUrl = "AdminDashboard.aspx";
                    break;
                default:
                    ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
                        "swal('Error!', 'Unauthorized access!', 'error');", true);
                    return;
            }
            Response.Redirect(redirectUrl);
        }

        protected void cw_Click1(object sender, EventArgs e)
        {
            if (!User.Identity.IsAuthenticated)
            {
                Response.Redirect("~/loginPage.aspx");
                return;
            }

            var identity = (FormsIdentity)User.Identity;
            string role = identity.Ticket.UserData.ToLower();

            string redirectUrl;
            switch (role)
            {
                case "admin":
                    redirectUrl = "~/CarWay/main1.aspx";
                    break;
                case "user":
                case "viewer":
                    redirectUrl = "~/AdminDashboard.aspx";
                    break;
                default:
                    ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
                        "swal('Error!', 'Unauthorized access!', 'error');", true);
                    return;
            }
            Response.Redirect(redirectUrl);
        }

    }
}