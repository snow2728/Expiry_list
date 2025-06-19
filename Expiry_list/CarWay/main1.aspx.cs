using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Expiry_list.CarWay
{
    public partial class main1 : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void cc_Click1(object sender, EventArgs e)
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
                    redirectUrl = "~/CarWay/dash2.aspx";
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

        protected void c1_Click1(object sender, EventArgs e)
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
                    redirectUrl = "~/CarWay/whView.aspx";
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