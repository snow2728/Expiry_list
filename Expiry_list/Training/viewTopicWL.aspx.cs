using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Expiry_list.Training
{
    public partial class viewTopicWL : System.Web.UI.Page
    {
        string strcon = ConfigurationManager.ConnectionStrings["con"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {

            if (!IsPostBack)
            {
                BindUserGrid();
                Training.DataBind.BindTopic(topicName);
                Training.DataBind.BindLevel(levelDb);
                ClearForm();
            }
        }

        private void BindUserGrid()
        {
            using (var conn = new SqlConnection(strcon))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT w.id,
                           w.topic, 
                           t.topicName,
                           tr.name as trainerName,
                           l.name as traineeLevel
                            FROM topicWLT w
                            LEFT JOIN topicT t ON w.topic = t.id
                            LEFT JOIN levelT l ON l.id = w.traineeLevel
                            LEFT JOIN trainerT tr ON w.trainerId = tr.id
                            ORDER BY w.id ASC;";

                conn.Open();
                using (var da = new SqlDataAdapter(cmd))
                using (var dt = new DataTable())
                {
                    da.Fill(dt);
                    GridView2.DataSource = dt;
                    GridView2.DataBind();
                }
            }
        }
        protected void GridView2_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow &&
                (e.Row.RowState & DataControlRowState.Edit) == DataControlRowState.Edit)
            {
                DataRowView rowView = (DataRowView)e.Row.DataItem;

                DropDownList ddlTopic = (DropDownList)e.Row.FindControl("ddlTopic");
                if (ddlTopic != null)
                {
                    Training.DataBind.BindTopic(ddlTopic);
                    if (rowView["topic"] != DBNull.Value)
                    {
                        ddlTopic.SelectedValue = rowView["topic"].ToString(); 
                    }
                }

                DropDownList ddlLevel = (DropDownList)e.Row.FindControl("ddlTraineeLevel");
                if (ddlLevel != null)
                {
                    Training.DataBind.BindLevel(ddlLevel);
                    if (rowView["traineeLevel"] != DBNull.Value)
                    {
                        ddlLevel.SelectedValue = rowView["traineeLevel"].ToString(); 
                    }
                }

                TextBox txtTrainerName = (TextBox)e.Row.FindControl("txtTrainerName");
                HiddenField hfTrainerId = (HiddenField)e.Row.FindControl("hfTrainerId");
                if (txtTrainerName != null && rowView["trainerName"] != DBNull.Value)
                {
                    txtTrainerName.Text = rowView["trainerName"].ToString();
                }
                if (hfTrainerId != null && rowView["trainerId"] != DBNull.Value)
                {
                    hfTrainerId.Value = rowView["trainerId"].ToString();
                }
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
                        string insertQuery = @"INSERT INTO topicWLT (topic, traineeLevel, trainerId)
                                       VALUES (@topic, @level, @trainerId )";

                        using (SqlCommand insertCmd = new SqlCommand(insertQuery, con, tran))
                        {
                            insertCmd.Parameters.AddWithValue("@topic", topicId);
                            insertCmd.Parameters.AddWithValue("@level", level);
                            insertCmd.Parameters.AddWithValue("@trainerId", trainerId);

                            insertCmd.ExecuteNonQuery();
                        }

                        tran.Commit();
                        ShowAlert("Success!", "Topic with Level added successfully!", "success");
                        ClearForm();
                        BindUserGrid();
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

        protected void GridView2_RowEditing(object sender, GridViewEditEventArgs e)
        {
            GridView2.EditIndex = e.NewEditIndex;
            BindUserGrid();
        }

        protected void ddlTopic_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                DropDownList ddlTopic = (DropDownList)sender;
                GridViewRow row = (GridViewRow)ddlTopic.NamingContainer;

                if (!string.IsNullOrEmpty(ddlTopic.SelectedValue))
                {
                    int topicId = Convert.ToInt32(ddlTopic.SelectedValue);

                    using (SqlConnection con = new SqlConnection(strcon))
                    {
                        string query = @"SELECT tr.id AS trainerId, tr.name AS trainerName
                                 FROM topicT t
                                 INNER JOIN trainerT tr ON t.trainerId = tr.id
                                 WHERE t.id = @topicId";

                        using (SqlCommand cmd = new SqlCommand(query, con))
                        {
                            cmd.Parameters.AddWithValue("@topicId", topicId);
                            con.Open();

                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    TextBox txtTrainerName = (TextBox)row.FindControl("txtTrainerName");
                                    HiddenField hfTrainerId = (HiddenField)row.FindControl("hfTrainerId");

                                    if (txtTrainerName != null)
                                        txtTrainerName.Text = reader["trainerName"].ToString();

                                    if (hfTrainerId != null)
                                        hfTrainerId.Value = reader["trainerId"].ToString();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ScriptManager.RegisterStartupScript(this, GetType(), "Error",
                    $"alert('Error loading trainer: {ex.Message}');", true);
            }
        }

        protected void GridView2_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            int id = Convert.ToInt32(GridView2.DataKeys[e.RowIndex].Value);
            GridViewRow row = GridView2.Rows[e.RowIndex];

            DropDownList ddlTopic = (DropDownList)row.FindControl("ddlTopic");
            DropDownList ddlTraineeLevel = (DropDownList)row.FindControl("ddlTraineeLevel");

            // Old values from GridView
            string oldTopicId = GridView2.DataKeys[e.RowIndex].Values["topic"].ToString();
            string oldTraineeLevel = GridView2.DataKeys[e.RowIndex].Values["traineeLevel"].ToString();

            string updateQuery = "UPDATE topicWLT SET ";
            List<string> setClauses = new List<string>();
            List<SqlParameter> parameters = new List<SqlParameter>();

            using (SqlConnection con = new SqlConnection(strcon))
            {
                con.Open();

                // If Topic changed
                if (!string.IsNullOrEmpty(ddlTopic.SelectedValue) && ddlTopic.SelectedValue != oldTopicId)
                {
                    setClauses.Add("topic = @topicId");
                    parameters.Add(new SqlParameter("@topicId", ddlTopic.SelectedValue));

                    // Get trainerId & trainerName from topicT
                    string getTrainerQuery = "SELECT trainerId FROM topicT WHERE id = @topicId";
                    int? trainerId = null;
                    string trainerName = null;

                    using (SqlCommand getTrainerCmd = new SqlCommand(getTrainerQuery, con))
                    {
                        getTrainerCmd.Parameters.AddWithValue("@topicId", ddlTopic.SelectedValue);
                        object result = getTrainerCmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            trainerId = Convert.ToInt32(result);

                            string getNameQuery = "SELECT name FROM trainerT WHERE id = @trainerId";
                            using (SqlCommand getNameCmd = new SqlCommand(getNameQuery, con))
                            {
                                getNameCmd.Parameters.AddWithValue("@trainerId", trainerId.Value);
                                trainerName = getNameCmd.ExecuteScalar()?.ToString();
                            }
                        }
                    }

                    if (trainerId.HasValue)
                    {
                        setClauses.Add("trainerId = @trainerId");
                        parameters.Add(new SqlParameter("@trainerId", trainerId.Value));
                    }
                    if (!string.IsNullOrEmpty(trainerName))
                    {
                        setClauses.Add("trainerName = @trainerName");
                        parameters.Add(new SqlParameter("@trainerName", trainerName));
                    }
                }

                // If trainee level changed
                if (!string.IsNullOrEmpty(ddlTraineeLevel.SelectedValue) &&
                    ddlTraineeLevel.SelectedValue != oldTraineeLevel)
                {
                    setClauses.Add("traineeLevel = @traineeLevel");
                    parameters.Add(new SqlParameter("@traineeLevel", ddlTraineeLevel.SelectedValue));
                }

                // Only update if something changed
                if (setClauses.Count > 0)
                {
                    updateQuery += string.Join(", ", setClauses) + " WHERE id = @id";
                    parameters.Add(new SqlParameter("@id", id));

                    using (SqlCommand cmd = new SqlCommand(updateQuery, con))
                    {
                        cmd.Parameters.AddRange(parameters.ToArray());
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            GridView2.EditIndex = -1;
            BindUserGrid();
        }

        protected void GridView2_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            try
            {
                int id = Convert.ToInt32(GridView2.DataKeys[e.RowIndex].Value);

                using (SqlConnection con = new SqlConnection(strcon))
                {
                    con.Open();

                    string query = "DELETE FROM topicWLT WHERE id = @id";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                }

                ScriptManager.RegisterStartupScript(
                    this, GetType(), "DeleteSuccess",
                    "Swal.fire('Deleted!', 'Topic deleted successfully.', 'success');", true);

                BindUserGrid();
            }
            catch (Exception ex)
            {
                string safeMsg = HttpUtility.JavaScriptStringEncode(
                    "An error occurred while deleting the topic: " + ex.Message);

                ScriptManager.RegisterStartupScript(
                    this, GetType(), "DeleteError",
                    $"Swal.fire('Error!', '{safeMsg}', 'error');", true);
            }
        }

        protected void GridView2_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            GridView2.EditIndex = -1;
            BindUserGrid();
            Response.Redirect("viewTopicWL.aspx");
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