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
            BindTrainer();
        }

        private void BindTrainer()
        {
            using (SqlConnection con = new SqlConnection(strcon))
            {
                con.Open();
                string query = "SELECT id, name FROM trainerT ORDER BY name";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        trainerDp.DataSource = reader;
                        trainerDp.DataTextField = "name";
                        trainerDp.DataValueField = "id";
                        trainerDp.DataBind();
                    }
                }
                trainerDp.Items.Insert(0, new ListItem("Select Trainer", ""));
            }
        }

        protected void btnaddTrainee_Click(object sender, EventArgs e)
        {
            try
            {
                string trainee = traineeName.Text.Trim();
                string level = levelDb.SelectedValue;
                string trainer = trainerDp.SelectedValue;
                if (string.IsNullOrEmpty(trainee) || string.IsNullOrEmpty(level) || string.IsNullOrEmpty(trainer))
                {
                    ShowAlert("Error!", "All fields are required!", "error");
                    return;
                }
                using (SqlConnection con = new SqlConnection(strcon))
                {
                    con.Open();
                    string query = "INSERT INTO traineeT (name, email, phone) VALUES (@name, @email, @phone)";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@name", trainee);
                        cmd.Parameters.AddWithValue("@email", level);
                        cmd.Parameters.AddWithValue("@phone", trainer);
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
            trainerDp.SelectedIndex = 0;
            levelDb.SelectedIndex = 0;
        }
    }
}