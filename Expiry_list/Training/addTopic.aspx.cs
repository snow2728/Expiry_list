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
    public partial class addTopic : System.Web.UI.Page
    {
        string strcon = ConfigurationManager.ConnectionStrings["con"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                BindTrainer();
                ClearForm();
            }
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
                        traineDp.DataSource = reader;
                        traineDp.DataTextField = "name";
                        traineDp.DataValueField = "id";
                        traineDp.DataBind();
                    }
                }
                traineDp.Items.Insert(0, new ListItem("Select Trainer", ""));
            }
        }

        protected void btnaddTopic_Click(object sender, EventArgs e)
        {
            try
            {
                string name = topicName.Text.Trim();
                string desc = topicdesc.Text.Trim();
                string trainerId = traineDp.SelectedValue;

                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(trainerId))
                {
                    ShowAlert("Error!", "Topic name and trainer are required!", "error");
                    return;
                }

                using (SqlConnection con = new SqlConnection(strcon))
                {
                    con.Open();
                    string checkQuery = "SELECT COUNT(*) FROM topicT WHERE topicname = @name";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, con))
                    {
                        checkCmd.Parameters.AddWithValue("@name", name);
                        int existingCount = (int)checkCmd.ExecuteScalar();
                        if (existingCount > 0)
                        {
                            ShowAlert("Error!", "A topic with this name already exists!", "error");
                            return;
                        }
                    }

                    SqlTransaction tran = con.BeginTransaction();

                    try
                    {
                        string insertQuery = @"INSERT INTO topicT (topicName, description, trainerId)
                                      VALUES (@name, @description, @trainer_id)";

                        using (SqlCommand cmd = new SqlCommand(insertQuery, con, tran))
                        {
                            cmd.Parameters.AddWithValue("@name", name);
                            cmd.Parameters.AddWithValue("@description", desc);
                            cmd.Parameters.AddWithValue("@trainer_id", trainerId);
                            cmd.ExecuteNonQuery();
                        }

                        tran.Commit();
                        ShowAlert("Success!", "Topic added successfully!", "success");
                        ClearForm();
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        ShowAlert("Error!", $"Insert failed: {ex.Message}", "error");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowAlert("Error!", $"Unexpected error: {ex.Message}", "error");
            }
        }

        private void ShowAlert(string title, string message, string type)
        {
            ScriptManager.RegisterStartupScript(this, GetType(), "alert",
                $"swal('{title}', '{HttpUtility.JavaScriptStringEncode(message)}', '{type}');", true);
        }

        private void ClearForm()
        {
            topicName.Text = "";
            topicdesc.Text = "";
            traineDp.SelectedIndex = 0;
        }
    }
}