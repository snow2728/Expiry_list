using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Expiry_list.Training
{
    public partial class addTrainee : System.Web.UI.Page
    {
        string strcon = ConfigurationManager.ConnectionStrings["con"].ConnectionString;
        protected void Page_Load(object sender, EventArgs e)
        {
           
        }

        protected void btnaddTrainee_Click(object sender, EventArgs e)
        {
            try
            {
                string trainee = traineeName.Text.Trim();
                string level = levelDb.SelectedValue;
                string store = txtStore.Text.Trim();
                if (string.IsNullOrEmpty(trainee) || string.IsNullOrEmpty(level) || string.IsNullOrEmpty(store))
                {
                    ShowAlert("Error!", "All fields are required!", "error");
                    return;
                }
                using (SqlConnection con = new SqlConnection(strcon))
                {
                    con.Open();
                    string query = "INSERT INTO traineeT (name, store, level) VALUES (@name, @store, @level)";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@name", trainee);
                        cmd.Parameters.AddWithValue("@store", store);
                        cmd.Parameters.AddWithValue("@level", level);
                        cmd.ExecuteNonQuery();
                    }
                }
                ShowAlert("Success!", "Trainee added successfully!", "success");
                ClearForm();
            }
            catch (Exception ex)
            {
                ShowAlert("Error!", ex.Message, "error");
            }
        }

        private void ShowAlert(string title, string message, string type)
        {
            ScriptManager.RegisterStartupScript(this, GetType(), "alert",
                $"swal('{title}', '{HttpUtility.JavaScriptStringEncode(message)}', '{type}');", true);
        }

        private void ClearForm()
        {
            traineeName.Text = "";
            levelDb.SelectedIndex = 0;
            txtStore.Text = "";
        }
    }
}