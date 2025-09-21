using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Web.Services;

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

        [System.Web.Services.WebMethod]
        [System.Web.Script.Services.ScriptMethod]
        public static List<TrainerDTO> GetTrainersByIds(List<int> ids)
        {
            List<TrainerDTO> trainers = new List<TrainerDTO>();

            if (ids == null || ids.Count == 0)
                return trainers;
            
            string inClause = string.Join(",", ids.Select((id, index) => "@id" + index));

            string query = $@"
                SELECT id, name 
                FROM trainerT 
                WHERE id IN ({inClause})
                ORDER BY name";

            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["con"].ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                // Add parameters safely
                for (int i = 0; i < ids.Count; i++)
                {
                    cmd.Parameters.AddWithValue("@id" + i, ids[i]);
                }

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
                (e.Row.RowState & DataControlRowState.Edit) > 0)
            {
                DataRowView rowView = (DataRowView)e.Row.DataItem;

                DropDownList ddlLevel = (DropDownList)e.Row.FindControl("ddlTraineeLevel");
                if (ddlLevel != null)
                {
                    Training.DataBind.BindLevel(ddlLevel);

                    string currentLevel = rowView["traineeLevel"] != DBNull.Value
                                          ? rowView["traineeLevel"].ToString()
                                          : null;

                    if (!string.IsNullOrEmpty(currentLevel) && ddlLevel.Items.FindByValue(currentLevel) != null)
                    {
                        ddlLevel.SelectedValue = currentLevel; 
                    }
                    else
                    {
                        ddlLevel.Items.Insert(0, new System.Web.UI.WebControls.ListItem("Select Level", "")); 
                        ddlLevel.SelectedIndex = 0;
                    }
                }

                DropDownList ddlTopic = (DropDownList)e.Row.FindControl("ddlTopic");
                if (ddlTopic != null)
                {
                    Training.DataBind.BindTopic(ddlTopic);
                    if (rowView["topic"] != DBNull.Value)
                    {
                        string topicVal = rowView["topic"].ToString();
                        if (ddlTopic.Items.FindByValue(topicVal) != null)
                            ddlTopic.SelectedValue = topicVal;
                    }
                }

                var container = e.Row.FindControl("trainerMultiSelect_" + rowView["id"]);
                if (container != null)
                {
                    var hfTrainerIds = (HiddenField)container.FindControl("hfTrainerIds");
                    var hfTrainerNames = (HiddenField)container.FindControl("hfTrainerNames");

                    string script = $@"
                $(function() {{
                    initTrainerMultiSelect('{container.ClientID}', '{hfTrainerIds.ClientID}', '{rowView["id"]}');
                }});";
                    ScriptManager.RegisterStartupScript(this, this.GetType(),
                        "initTrainerMultiSelect" + rowView["id"], script, true);
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
            string topicNameText = topicName.Text.Trim();
            string level = levelDb.SelectedValue;
            string selectedTrainerIds = hfTrainerDp.Value;

            if (string.IsNullOrEmpty(topicNameText) || string.IsNullOrEmpty(level))
            {
                ShowAlert("Error!", "Topic and Level are required!", "error");
                return;
            }

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
                    int topicId;
                    string getTopicIdQuery = "SELECT id FROM topicT WHERE topicName = @topicName";
                    using (SqlCommand cmd = new SqlCommand(getTopicIdQuery, con, tran))
                    {
                        cmd.Parameters.Add("@topicName", SqlDbType.NVarChar).Value = topicNameText;
                        object existing = cmd.ExecuteScalar();
                        topicId = existing != null ? Convert.ToInt32(existing) : 0;
                    }

                    if (topicId == 0)
                    {
                        string insertTopic = "INSERT INTO topicT (topicName) OUTPUT INSERTED.id VALUES (@topicName)";
                        using (SqlCommand insertCmd = new SqlCommand(insertTopic, con, tran))
                        {
                            insertCmd.Parameters.Add("@topicName", SqlDbType.NVarChar).Value = topicNameText;
                            topicId = (int)insertCmd.ExecuteScalar();
                        }
                    }

                    foreach (string tIdStr in selectedTrainerIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (!int.TryParse(tIdStr, out int trainerId)) continue;

                        string checkQuery = @"SELECT COUNT(*) FROM topicWLT 
                                      WHERE topic = @topicId AND traineeLevel = @level AND trainerId = @trainerId";
                        using (SqlCommand checkCmd = new SqlCommand(checkQuery, con, tran))
                        {
                            checkCmd.Parameters.Add("@topicId", SqlDbType.Int).Value = topicId;
                            checkCmd.Parameters.Add("@level", SqlDbType.NVarChar).Value = level;
                            checkCmd.Parameters.Add("@trainerId", SqlDbType.Int).Value = trainerId;
                            int exists = (int)checkCmd.ExecuteScalar();
                            if (exists > 0) continue;
                        }

                        string insertWLT = @"INSERT INTO topicWLT (topic, traineeLevel, trainerId, isActive)
                                     VALUES (@topicId, @level, @trainerId, 1)";
                        using (SqlCommand insertCmd = new SqlCommand(insertWLT, con, tran))
                        {
                            insertCmd.Parameters.Add("@topicId", SqlDbType.Int).Value = topicId;
                            insertCmd.Parameters.Add("@level", SqlDbType.NVarChar).Value = level;
                            insertCmd.Parameters.Add("@trainerId", SqlDbType.Int).Value = trainerId;
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
            DropDownList ddlTraineeLevel = (DropDownList)row.FindControl("ddlTraineeLevel");
            string selectedLevel = ddlTraineeLevel.SelectedValue;
            string newTrainerIdsCsv = ((HiddenField)row.FindControl("hfTrainerIds")).Value;
            bool newIsActive = ((System.Web.UI.WebControls.CheckBox)row.FindControl("chkTopic_Enable")).Checked;

            List<int> selectedTrainerIds = string.IsNullOrEmpty(newTrainerIdsCsv)
                ? new List<int>()
                : newTrainerIdsCsv.Split(',').Select(x => int.Parse(x.Trim())).Distinct().ToList();

            using (SqlConnection con = new SqlConnection(strcon))
            {
                con.Open();

                int topicId;
                using (SqlCommand cmdTopic = new SqlCommand("SELECT id FROM topicT WHERE topicName=@name", con))
                {
                    cmdTopic.Parameters.AddWithValue("@name", newTopicName);
                    var idObj = cmdTopic.ExecuteScalar();
                    if (idObj != null)
                        topicId = Convert.ToInt32(idObj);
                    else
                    {
                        using (SqlCommand cmdInsert = new SqlCommand(
                            "INSERT INTO topicT(topicName) OUTPUT INSERTED.id VALUES(@name)", con))
                        {
                            cmdInsert.Parameters.AddWithValue("@name", newTopicName);
                            topicId = (int)cmdInsert.ExecuteScalar();
                        }
                    }
                }

                string currentTraineeLevel;
                using (SqlCommand cmdOld = new SqlCommand(
                    "SELECT traineeLevel FROM topicWLT WHERE id=@id", con))
                {
                    cmdOld.Parameters.AddWithValue("@id", rowId);
                    currentTraineeLevel = cmdOld.ExecuteScalar()?.ToString();
                }

                string traineeLevelToUse = (!string.IsNullOrEmpty(selectedLevel) && selectedLevel != "Select Level")
                                            ? selectedLevel
                                            : currentTraineeLevel;

                if (string.IsNullOrEmpty(traineeLevelToUse))
                    traineeLevelToUse = "DefaultLevel"; 

                using (SqlCommand cmdUpdateBase = new SqlCommand(
                    "UPDATE topicWLT SET topic=@topic, traineeLevel=@level, IsActive=@isActive WHERE id=@id AND trainerId IS NULL", con))
                {
                    cmdUpdateBase.Parameters.AddWithValue("@topic", topicId);
                    cmdUpdateBase.Parameters.AddWithValue("@level", traineeLevelToUse);
                    cmdUpdateBase.Parameters.AddWithValue("@isActive", newIsActive);
                    cmdUpdateBase.Parameters.AddWithValue("@id", rowId);
                    cmdUpdateBase.ExecuteNonQuery();
                }

                Dictionary<int, int> existingTrainerRows = new Dictionary<int, int>();
                using (SqlCommand cmdExisting = new SqlCommand(
                    "SELECT id, trainerId FROM topicWLT WHERE topic=@topic AND trainerId IS NOT NULL", con))
                {
                    cmdExisting.Parameters.AddWithValue("@topic", topicId);
                    using (SqlDataReader dr = cmdExisting.ExecuteReader())
                    {
                        while (dr.Read())
                            existingTrainerRows[Convert.ToInt32(dr["trainerId"])] = Convert.ToInt32(dr["id"]);
                    }
                }

                foreach (var trainerId in existingTrainerRows.Keys.Except(selectedTrainerIds))
                {
                    int rowPK = existingTrainerRows[trainerId];

                    using (SqlCommand cmdCheck = new SqlCommand(
                        "SELECT COUNT(*) FROM traineeTopicT WHERE topicId=@topicId", con))
                    {
                        cmdCheck.Parameters.AddWithValue("@topicId", rowPK);

                        int count = (int)cmdCheck.ExecuteScalar();
                        if (count > 0)
                        {
                            string script = @"Swal.fire({
                                icon: 'error',
                                title: 'Cannot Delete',
                                text: 'This trainer has other schedules and cannot be deleted!',
                                confirmButtonText: 'OK'
                              });";

                            ScriptManager.RegisterStartupScript(this, this.GetType(), "swalAlert", script, true);
                            continue; 
                        }
                    }

                    using (SqlCommand cmdDel = new SqlCommand(
                        "DELETE FROM topicWLT WHERE id=@rowPK", con))
                    {
                        cmdDel.Parameters.AddWithValue("@rowPK", rowPK);
                        cmdDel.ExecuteNonQuery();
                    }
                }


                foreach (var trainerId in selectedTrainerIds)
                {
                    if (!existingTrainerRows.ContainsKey(trainerId))
                    {
                        using (SqlCommand cmdInsert = new SqlCommand(
                            "INSERT INTO topicWLT(topic, traineeLevel, trainerId, IsActive) VALUES(@topic,@level,@trainerId,@isActive)", con))
                        {
                            cmdInsert.Parameters.AddWithValue("@topic", topicId);
                            cmdInsert.Parameters.AddWithValue("@trainerId", trainerId);
                            cmdInsert.Parameters.AddWithValue("@isActive", newIsActive);
                            cmdInsert.Parameters.AddWithValue("@level", traineeLevelToUse);
                            cmdInsert.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        using (SqlCommand cmdUpdate = new SqlCommand(
                            "UPDATE topicWLT SET traineeLevel=@level, IsActive=@isActive WHERE id=@rowPK", con))
                        {
                            cmdUpdate.Parameters.AddWithValue("@rowPK", existingTrainerRows[trainerId]);
                            cmdUpdate.Parameters.AddWithValue("@isActive", newIsActive);
                            cmdUpdate.Parameters.AddWithValue("@level", traineeLevelToUse);
                            cmdUpdate.ExecuteNonQuery();
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