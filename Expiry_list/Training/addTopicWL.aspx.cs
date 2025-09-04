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
    public partial class addTopicWL : System.Web.UI.Page
    {
        string strcon = ConfigurationManager.ConnectionStrings["con"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                Training.DataBind.BindTopic(topicName);
                Training.DataBind.BindLevel(levelDb);
                ClearForm();
            }
        }

        protected void btnaddTopic_Click(object sender, EventArgs e)
        {
            try
            {
                string topicId = topicName.SelectedValue;
                string level = levelDb.SelectedValue;

                if (string.IsNullOrEmpty(topicId) || string.IsNullOrEmpty(level))
                {
                    ShowAlert("Error!", "Topic and Level are required!", "error");
                    return;
                }

                string trainerId = string.Empty;
                string trainerName = trainerDp.Text;

                using (SqlConnection con = new SqlConnection(strcon))
                {
                    con.Open();
                    string trainerQuery = @"SELECT tr.id, tr.name 
                                    FROM topicT t 
                                    INNER JOIN trainerT tr ON t.trainerId = tr.id
                                    WHERE t.id = @topicId or tr.name=@trname";

                    using (SqlCommand cmd = new SqlCommand(trainerQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@topicId", topicId);
                        cmd.Parameters.AddWithValue("@trname", trainerName);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                trainerId = reader["id"].ToString();
                                trainerName = reader["name"].ToString();
                            }
                            else
                            {
                                ShowAlert("Error!", "Trainer not found for selected topic!", "error");
                                return;
                            }
                        }
                    }

                    string checkQuery = "SELECT COUNT(*) FROM topicWLT WHERE topic = @topicId AND traineeLevel = @level AND trainerId = @trainerId";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, con))
                    {
                        checkCmd.Parameters.AddWithValue("@topicId", topicId);
                        checkCmd.Parameters.AddWithValue("@level", level);
                        checkCmd.Parameters.AddWithValue("@trainerId", trainerId);

                        int existingCount = (int)checkCmd.ExecuteScalar();
                        if (existingCount > 0)
                        {
                            ShowAlert("Error!", "A topic is already assigned to this level!", "error");
                            return;
                        }
                    }

                    SqlTransaction tran = con.BeginTransaction();

                    try
                    {
                        string insertQuery = @"INSERT INTO topicWLT (topic, traineeLevel, trainerId, trainerName)
                                       VALUES (@topic, @level, @trainerId, @trainerName)";

                        using (SqlCommand insertCmd = new SqlCommand(insertQuery, con, tran))
                        {
                            insertCmd.Parameters.AddWithValue("@topic", topicId);
                            insertCmd.Parameters.AddWithValue("@level", level);
                            insertCmd.Parameters.AddWithValue("@trainerId", trainerId);
                            insertCmd.Parameters.AddWithValue("@trainerName", trainerName);

                            insertCmd.ExecuteNonQuery();
                        }

                        tran.Commit();
                        ShowAlert("Success!", "Topic with Level added successfully!", "success");
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
            topicName.SelectedIndex = 0;
            levelDb.SelectedIndex = 0;
            trainerDp.Text = "";
        }
    }
}