using System;
using System.Web.UI;

namespace Expiry_list.Training
{
    public partial class scheduleList : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
        }

        protected void btnSubmit_Click(object sender, EventArgs e)
        {
            lblResult.Text = "Selected items are handled client-side. You can store them in a HiddenField to submit to server.";
        }
    }
}
