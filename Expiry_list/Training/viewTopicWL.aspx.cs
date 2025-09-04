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
                           w.traineeLevel,
                           ISNULL(w.trainerId, t.trainerId) AS trainerId,
                           ISNULL(w.trainerName, tr.name) AS trainerName
                            FROM topicWLT w
                            LEFT JOIN topicT t ON w.topic = t.id
                            LEFT JOIN trainerT tr ON ISNULL(w.trainerId, t.trainerId) = tr.id
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
        private void BindTopicDropdown(DropDownList dropdown)
        {
            using (SqlConnection con = new SqlConnection(strcon))
            {
                con.Open();
                string query = "SELECT id, topicName FROM topicT ORDER BY topicName";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        dropdown.DataSource = reader;
                        dropdown.DataTextField = "topicName";
                        dropdown.DataValueField = "id";
                        dropdown.DataBind();
                    }
                }
                dropdown.Items.Insert(0, new ListItem("Select Topic", ""));
            }
        }

        protected void GridView2_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                if ((e.Row.RowState & DataControlRowState.Edit) == DataControlRowState.Edit)
                {
                    DropDownList ddlTopic = (DropDownList)e.Row.FindControl("ddlTopic");

                    if (ddlTopic != null)
                    {
                        BindTopicDropdown(ddlTopic);

                        DataRowView rowView = (DataRowView)e.Row.DataItem;
                        if (rowView["topicName"] != DBNull.Value)
                        {
                            ddlTopic.SelectedValue = rowView["topicName"].ToString();
                        }
                    }
                }
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
                        string query = @"SELECT t.traineeLevel, tr.id AS trainerId, tr.name AS trainerName
                           FROM topicT t
                           LEFT JOIN trainers tr ON t.trainerId = tr.id
                           WHERE t.id = @topicId";

                        using (SqlCommand cmd = new SqlCommand(query, con))
                        {
                            cmd.Parameters.AddWithValue("@topicId", topicId);
                            con.Open();

                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    // Update trainee level
                                    DropDownList ddlTraineeLevel = (DropDownList)row.FindControl("ddlTraineeLevel");
                                    if (ddlTraineeLevel != null)
                                    {
                                        ddlTraineeLevel.SelectedValue = reader["traineeLevel"].ToString();
                                    }

                                    // Update trainer info
                                    TextBox txtTrainerName = (TextBox)row.FindControl("txtTrainerName");
                                    txtTrainerName.Text = reader["trainerName"].ToString();

                                    HiddenField hfTrainerId = (HiddenField)row.FindControl("hfTrainerId");
                                    hfTrainerId.Value = reader["trainerId"] != DBNull.Value ?
                                                      reader["trainerId"].ToString() : string.Empty;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error in ddlTopic_SelectedIndexChanged: " + ex.Message);
                ScriptManager.RegisterStartupScript(this, GetType(), "Error",
                    $"alert('Error loading topic details: {ex.Message}');", true);
            }
        }

        protected void GridView2_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            int id = Convert.ToInt32(GridView2.DataKeys[e.RowIndex].Value);
            GridViewRow row = GridView2.Rows[e.RowIndex];

            DropDownList ddlTopic = (DropDownList)row.FindControl("ddlTopic");
            DropDownList ddlTraineeLevel = (DropDownList)row.FindControl("ddlTraineeLevel");
            HiddenField hfTrainerId = (HiddenField)row.FindControl("hfTrainerId");

            using (SqlConnection con = new SqlConnection(strcon))
            {
                int? trainerId = null;
                string trainerName = string.Empty;

                string getTrainerQuery = "SELECT trainerId FROM topicT WHERE id = @topicId";
                using (SqlCommand getTrainerCmd = new SqlCommand(getTrainerQuery, con))
                {
                    getTrainerCmd.Parameters.AddWithValue("@topicId", ddlTopic.SelectedValue);
                    con.Open();

                    object result = getTrainerCmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        trainerId = Convert.ToInt32(result);

                        // Get trainer name if needed
                        string getNameQuery = "SELECT name FROM trainerT WHERE id = @trainerId";
                        using (SqlCommand getNameCmd = new SqlCommand(getNameQuery, con))
                        {
                            getNameCmd.Parameters.AddWithValue("@trainerId", trainerId);
                            trainerName = getNameCmd.ExecuteScalar()?.ToString();
                        }
                    }
                }

                // Now perform the update
                string updateQuery = @"UPDATE topicWLT 
                          SET topic = @topicId, 
                              traineeLevel = @traineeLevel,
                              trainerId = @trainerId,
                              trainerName = @trainerName
                          WHERE id = @id";

                using (SqlCommand cmd = new SqlCommand(updateQuery, con))
                {
                    cmd.Parameters.AddWithValue("@topicId", ddlTopic.SelectedValue);
                    cmd.Parameters.AddWithValue("@traineeLevel", ddlTraineeLevel.SelectedValue);
                    cmd.Parameters.AddWithValue("@trainerId",
                        trainerId.HasValue ? (object)trainerId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@trainerName",
                        string.IsNullOrEmpty(trainerName) ? DBNull.Value : (object)trainerName);
                    cmd.Parameters.AddWithValue("@id", id);

                    cmd.ExecuteNonQuery();
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

    }
}