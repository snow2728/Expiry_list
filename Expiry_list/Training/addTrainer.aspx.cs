using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using AjaxControlToolkit.HtmlEditor.ToolbarButtons;

namespace Expiry_list.Training
{
    public partial class addTrainer : System.Web.UI.Page
    {
        string strcon = ConfigurationManager.ConnectionStrings["con"].ConnectionString;
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                ClearForm();
            }
        }

        protected void btnaddTrainer_Click(object sender, EventArgs e)
        {
            try
            {
                string name = trainerName.Text.Trim();
                string position = trainerPosition.SelectedValue;

                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(position))
                {
                    ShowAlert("Error!", "Trainer name and position are required!", "error");
                    return;
                }

                using (SqlConnection con = new SqlConnection(strcon))
                {
                    con.Open();

                    string checkQuery = "SELECT COUNT(*) FROM trainerT WHERE name = @name";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, con))
                    {
                        checkCmd.Parameters.AddWithValue("@name", name);
                        int existingCount = (int)checkCmd.ExecuteScalar();
                        if (existingCount > 0)
                        {
                            ShowAlert("Error!", "A trainer with this name already exists!", "error");
                            return;
                        }
                    }

                    SqlTransaction tran = con.BeginTransaction();

                    try
                    {
                        string query = @"INSERT INTO trainerT (name, position) 
                                 OUTPUT INSERTED.id 
                                 VALUES (@name, @position)";

                        using (SqlCommand cmd = new SqlCommand(query, con, tran))
                        {
                            cmd.Parameters.AddWithValue("@name", name);
                            cmd.Parameters.AddWithValue("@position", position);
                            cmd.ExecuteNonQuery();
                        }

                        tran.Commit();
                        ShowAlert("Success!", "Trainer registered successfully!", "success");
                        ClearForm();
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        ShowAlert("Error!", $"Registration failed: {ex.Message}", "error");
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
            trainerName.Text = "";
            trainerPosition.SelectedIndex = 0;
        }
    }
}