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
                  SELECT MIN(w.id) AS Id,
                        w.topic AS topicId,
                        tp.topicName AS topic,
                        l.name AS traineeLevel,
                        STRING_AGG(tr.id, ',') AS trainerIdsCsv,
                        STRING_AGG(tr.name, ', ') AS trainerNamesCsv,
                        MAX(CAST(w.IsActive AS INT)) AS IsActive
                    FROM topicWLT w
                    LEFT JOIN topicT tp ON tp.id = w.topic
                    LEFT JOIN levelT l ON l.id = w.traineeLevel
                    LEFT JOIN trainerT tr ON w.trainerId = tr.id
                    GROUP BY w.topic, tp.topicName, l.name
                    ORDER BY tp.topicName, l.name;";

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

        [System.Web.Services.WebMethod]
        [System.Web.Script.Services.ScriptMethod]
        public static List<TrainerDTO> GetTrainers(string searchTerm)
        {
            List<TrainerDTO> trainers = new List<TrainerDTO>();
            string query = @"SELECT id, name FROM trainerT 
                     WHERE name LIKE '%' + @searchTerm + '%' 
                     ORDER BY name";

            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["con"].ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@searchTerm", searchTerm ?? "");
                con.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        trainers.Add(new TrainerDTO
                        {
                            Id = Convert.ToInt32(dr["id"]),
                            Name = dr["name"].ToString()
                        });
                    }
                }
            }
            return trainers;
        }

        public class TrainerDTO
        {
            public int Id { get; set; }
            public string Name { get; set; }
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
                var dataItem = (DataRowView)e.Row.DataItem;
                var container = e.Row.FindControl("trainerMultiSelect_" + dataItem["id"]);

                if (container != null)
                {
                    var hfTrainerIds = (HiddenField)container.FindControl("hfTrainerIds");
                    var hfTrainerNames = (HiddenField)container.FindControl("hfTrainerNames");

                    string script = $@"
                $(function() {{
                    initTrainerMultiSelect('{container.ClientID}', '{hfTrainerIds.ClientID}', '{dataItem["id"]}');
                }});
            ";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "initTrainerMultiSelect" + dataItem["id"], script, true);
                }
            }
        }

        protected void GridView2_RowEditing(object sender, GridViewEditEventArgs e)
        {
            GridView2.EditIndex = e.NewEditIndex;
            BindUserGrid();
        }

        protected void btnaddTopic_Click(object sender, EventArgs e)
        {
            try
            {
                string topicNameText = topicName.Text.Trim();
                string level = levelDb.SelectedValue;

                if (string.IsNullOrEmpty(topicNameText) || string.IsNullOrEmpty(level))
                {
                    ShowAlert("Error!", "Topic and Level are required!", "error");
                    return;
                }

                string selectedTrainerIds = hfTrainerDp.Value;
                if (string.IsNullOrEmpty(selectedTrainerIds))
                {
                    ShowAlert("Error!", "Please select at least one trainer!", "error");
                    return;
                }

                using (SqlConnection con = new SqlConnection(strcon))
                {
                    con.Open();
                    SqlTransaction tran = con.BeginTransaction();

                    try
                    {
                        // Insert topic
                        string getTopicIdQuery = "SELECT id FROM topicT WHERE topicName = @topicName";
                        int topicId;

                        using (SqlCommand getCmd = new SqlCommand(getTopicIdQuery, con, tran))
                        {
                            getCmd.Parameters.AddWithValue("@topicName", topicNameText);
                            object existingId = getCmd.ExecuteScalar();

                            if (existingId != null)
                            {
                                topicId = Convert.ToInt32(existingId);
                            }
                            else
                            {
                                string insertTopic = "INSERT INTO topicT (topicName) OUTPUT INSERTED.id VALUES (@topicName)";
                                using (SqlCommand insertCmd = new SqlCommand(insertTopic, con, tran))
                                {
                                    insertCmd.Parameters.AddWithValue("@topicName", topicNameText);
                                    topicId = (int)insertCmd.ExecuteScalar();
                                }
                            }
                        }

                        string[] trainerIds = selectedTrainerIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string tIdStr in trainerIds)
                        {
                            if (!int.TryParse(tIdStr, out int trainerId))
                                continue;

                            string checkQuery = @"SELECT COUNT(*) 
                                          FROM topicWLT 
                                          WHERE topic = @topicId 
                                            AND traineeLevel = @level 
                                            AND trainerId = @trainerId";
                            using (SqlCommand checkCmd = new SqlCommand(checkQuery, con, tran))
                            {
                                checkCmd.Parameters.AddWithValue("@topicId", topicId);
                                checkCmd.Parameters.AddWithValue("@level", level);
                                checkCmd.Parameters.AddWithValue("@trainerId", trainerId);

                                int existingCount = (int)checkCmd.ExecuteScalar();
                                if (existingCount > 0)
                                {
                                    continue;
                                }
                            }

                            // Insert into topicWLT
                            string insertWLT = @"INSERT INTO topicWLT (topic, traineeLevel, trainerId, isActive) 
                                         VALUES (@topicId, @level, @trainerId, 1)";
                            using (SqlCommand insertCmd = new SqlCommand(insertWLT, con, tran))
                            {
                                insertCmd.Parameters.AddWithValue("@topicId", topicId);
                                insertCmd.Parameters.AddWithValue("@level", level);
                                insertCmd.Parameters.AddWithValue("@trainerId", trainerId);
                                insertCmd.ExecuteNonQuery();
                            }
                        }

                        tran.Commit();
                        ShowAlert("Success!", "Topic with selected trainer(s) added successfully!", "success");
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
            int rowId = Convert.ToInt32(GridView2.DataKeys[e.RowIndex].Value);
            GridViewRow row = GridView2.Rows[e.RowIndex];

            string newTopicName = ((TextBox)row.FindControl("txtTopic")).Text.Trim();
            string newTraineeLevel = ((DropDownList)row.FindControl("ddlTraineeLevel")).SelectedValue;
            string newTrainerIdsCsv = ((HiddenField)row.FindControl("hfTrainerIds")).Value;
            bool newIsActive = ((CheckBox)row.FindControl("chkTopic_Enable")).Checked;

            using (SqlConnection con = new SqlConnection(strcon))
            {
                con.Open();

                int oldTopicId = 0;
                string oldTraineeLevel = null;
                bool oldIsActive = false;

                // --- Get current row's old values ---
                using (SqlCommand cmdOld = new SqlCommand(
                    "SELECT topic, traineeLevel, IsActive FROM topicWLT WHERE id=@id", con))
                {
                    cmdOld.Parameters.AddWithValue("@id", rowId);
                    using (SqlDataReader dr = cmdOld.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            oldTopicId = Convert.ToInt32(dr["topic"]);
                            oldTraineeLevel = dr["traineeLevel"]?.ToString();
                            oldIsActive = dr["IsActive"] != DBNull.Value && (bool)dr["IsActive"];
                        }
                    }
                }

                // --- Get or insert topicT id ---
                int newTopicId = oldTopicId;
                if (!string.IsNullOrEmpty(newTopicName))
                {
                    using (SqlCommand cmdTopic = new SqlCommand("SELECT id FROM topicT WHERE topicName=@name", con))
                    {
                        cmdTopic.Parameters.AddWithValue("@name", newTopicName);
                        object idObj = cmdTopic.ExecuteScalar();
                        if (idObj != null)
                            newTopicId = Convert.ToInt32(idObj);
                        else
                        {
                            using (SqlCommand cmdInsert = new SqlCommand("INSERT INTO topicT(topicName) OUTPUT INSERTED.id VALUES(@name)", con))
                            {
                                cmdInsert.Parameters.AddWithValue("@name", newTopicName);
                                newTopicId = (int)cmdInsert.ExecuteScalar();
                            }
                        }
                    }
                }

                List<int> newTrainerIds = new List<int>();
                if (!string.IsNullOrEmpty(newTrainerIdsCsv))
                {
                    newTrainerIds = newTrainerIdsCsv.Split(',')
                                    .Select(t => int.TryParse(t.Trim(), out int val) ? val : 0)
                                    .Where(t => t > 0)
                                    .ToList();
                }

                bool topicChanged = newTopicId != oldTopicId;
                bool levelChanged = newTraineeLevel != oldTraineeLevel;
                bool isActiveChanged = newIsActive != oldIsActive;

                if (topicChanged || levelChanged || isActiveChanged)
                {
                    List<string> setClauses = new List<string>();
                    SqlCommand cmdUpdate = new SqlCommand();
                    cmdUpdate.Connection = con;

                    if (topicChanged)
                    {
                        setClauses.Add("topic=@topicId");
                        cmdUpdate.Parameters.AddWithValue("@topicId", newTopicId );
                    }
                    if (levelChanged)
                    {
                        setClauses.Add("traineeLevel=@level");
                        cmdUpdate.Parameters.AddWithValue("@level", (object)newTraineeLevel ?? (object)oldTraineeLevel);
                    }
                    if (isActiveChanged)
                    {
                        setClauses.Add("IsActive=@isActive");
                        cmdUpdate.Parameters.AddWithValue("@isActive", newIsActive);
                    }

                    cmdUpdate.CommandText = $@"
                UPDATE topicWLT
                SET {string.Join(", ", setClauses)}
                WHERE topic=@oldTopic AND traineeLevel=@oldLevel AND IsActive=@oldIsActive";

                    cmdUpdate.Parameters.AddWithValue("@oldTopic", oldTopicId);
                    cmdUpdate.Parameters.AddWithValue("@oldLevel", (object)oldTraineeLevel);
                    cmdUpdate.Parameters.AddWithValue("@oldIsActive", oldIsActive);
                    cmdUpdate.ExecuteNonQuery();
                }

                // --- Sync trainers for the new topic/level/active ---
                foreach (int trainerId in newTrainerIds)
                {
                    using (SqlCommand cmdCheck = new SqlCommand(
                        "SELECT COUNT(1) FROM topicWLT WHERE topic=@topic AND traineeLevel=@level AND trainerId=@trainerId AND IsActive=@isActive", con))
                    {
                        cmdCheck.Parameters.AddWithValue("@topic", newTopicId);
                        cmdCheck.Parameters.AddWithValue("@level", (object)newTraineeLevel ?? DBNull.Value);
                        cmdCheck.Parameters.AddWithValue("@trainerId", trainerId);
                        cmdCheck.Parameters.AddWithValue("@isActive", newIsActive);

                        int exists = (int)cmdCheck.ExecuteScalar();
                        if (exists == 0)
                        {
                            using (SqlCommand cmdInsert = new SqlCommand(
                                "INSERT INTO topicWLT(topic, traineeLevel, trainerId, IsActive) VALUES(@topic,@level,@trainerId,@isActive)", con))
                            {
                                cmdInsert.Parameters.AddWithValue("@topic", newTopicId);
                                cmdInsert.Parameters.AddWithValue("@level", (object)newTraineeLevel ?? DBNull.Value);
                                cmdInsert.Parameters.AddWithValue("@trainerId", trainerId);
                                cmdInsert.Parameters.AddWithValue("@isActive", newIsActive);
                                cmdInsert.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }

            GridView2.EditIndex = -1;
            BindUserGrid();
        }

        protected void GridView2_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            int id = Convert.ToInt32(GridView2.DataKeys[e.RowIndex].Value);

            try
            {
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
            }
            catch (Exception ex)
            {
                string safeMsg = HttpUtility.JavaScriptStringEncode("An error occurred while deleting the topic: " + ex.Message);
                ScriptManager.RegisterStartupScript(
                    this, GetType(), "DeleteError",
                    $"Swal.fire('Error!', '{safeMsg}', 'error');", true);
            }

            BindUserGrid();
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
            topicName.Text = "";
            levelDb.SelectedIndex = 0;
            //trainerDp.Text = "";
        }



    }
}