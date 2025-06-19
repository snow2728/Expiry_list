using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Expiry_list.Training
{
    public partial class rege1 : System.Web.UI.Page
    {
        string strcon = ConfigurationManager.ConnectionStrings["con"].ConnectionString;
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                no.Text = GetNextTranNo();
                tdyDate.Text = DateTime.Now.ToString("dd/MM/yyyy");
                BindTopic();
            }
        }

        protected void createBtn_Click(object sender, EventArgs e)
        {
            try
            {
                string tranNo = no.Text.Trim();
                string topicId = topicDP.SelectedValue;
                string description = desc.Text.Trim();
                string room = locationDp.SelectedValue;
                string date1 = date.Text.Trim();
                string time1 = time.Text.Trim();

                if (string.IsNullOrEmpty(topicId) || string.IsNullOrEmpty(room))
                {
                    ShowAlert("Error!", "Topic and Training Room are required!", "error");
                    return;
                }

                using (SqlConnection con = new SqlConnection(strcon))
                {
                    con.Open();

                    string trainerName = "";
                    string level = "";

                    string query = @"SELECT trainerName, traineeLevel 
                             FROM topicWLT 
                             WHERE topic = @topicId";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@topicId", topicId);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                trainerName = reader["trainerName"].ToString();
                                level = reader["traineeLevel"].ToString();
                            }
                        }
                    }

                    string insertQuery = @"INSERT INTO scheduleT 
                (tranNo, topicName, description, room, trainerName, position, date, time)
                VALUES 
                (@tranNo, @topicName, @description, @room, @trainerName, @position, @date, @time)";

                    using (SqlCommand insertCmd = new SqlCommand(insertQuery, con))
                    {
                        insertCmd.Parameters.AddWithValue("@tranNo", tranNo);
                        insertCmd.Parameters.AddWithValue("@topicName", topicDP.SelectedItem.Text);
                        insertCmd.Parameters.AddWithValue("@description", description);
                        insertCmd.Parameters.AddWithValue("@room", room);
                        insertCmd.Parameters.AddWithValue("@trainerName", trainerName);
                        insertCmd.Parameters.AddWithValue("@position", level);
                        insertCmd.Parameters.AddWithValue("@date", date1);
                        insertCmd.Parameters.AddWithValue("@time", time1);

                        insertCmd.ExecuteNonQuery();
                    }

                    ShowAlert("Success!", "Schedule created successfully!", "success");
                    ClearForm();
                }
            }
            catch (Exception ex)
            {
                ShowAlert("Error!", $"Failed to create schedule: {ex.Message}", "error");
            }
        }

        private void BindTopic()
        {
            using (SqlConnection con = new SqlConnection(strcon))
            {
                con.Open();
                string query = @"SELECT t.id, t.topicName, tr.traineeLevel AS [level], tr.trainerName 
                        FROM topicT t 
                        INNER JOIN topicWLT tr ON t.id = tr.topic 
                        ORDER BY t.topicName";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        topicDP.Items.Clear();
                        while (reader.Read())
                        {
                            ListItem item = new ListItem(
                                reader["topicName"].ToString(),
                                reader["id"].ToString()
                            );

                            // Set both attributes
                            item.Attributes["data-trainer"] = reader["trainerName"].ToString();
                            item.Attributes["data-level"] = reader["level"].ToString();
                            topicDP.Items.Add(item);
                        }
                    }
                }
                topicDP.Items.Insert(0, new ListItem("Select Topic", ""));
            }
        }

        private string GetNextTranNo()
        {
            string prefix = "TS";
            string query = @"SELECT MAX(CAST(SUBSTRING(tranNo, CHARINDEX('-', tranNo) + 1, LEN(tranNo)) AS INT))
                     FROM scheduleT
                     WHERE tranNo LIKE @pattern";

            using (SqlConnection conn = new SqlConnection(strcon))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                cmd.Parameters.AddWithValue("@pattern", prefix + "-%");
                object result = cmd.ExecuteScalar();

                int lastNumber = result == DBNull.Value ? 0 : Convert.ToInt32(result);
                int nextNumber = lastNumber + 1;

                return $"{prefix}-{nextNumber}";
            }
        }

        private void ShowAlert(string title, string message, string type)
        {
            ScriptManager.RegisterStartupScript(this, GetType(), "alert",
                $"swal('{title}', '{HttpUtility.JavaScriptStringEncode(message)}', '{type}');", true);
        }

        private void ClearForm()
        {
            topicDP.SelectedIndex = 0;
            desc.Text = "";
            locationDp.SelectedIndex = 0;
            trainerDp.Text = string.Empty;
            position.Text = string.Empty;
            date.Text = string.Empty;
            time.Text = string.Empty;   
        }

    }
}