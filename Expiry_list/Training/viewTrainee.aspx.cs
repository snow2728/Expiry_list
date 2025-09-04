using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;
using static Expiry_list.regeForm1;

namespace Expiry_list.Training
{
    public partial class viewTrainee : System.Web.UI.Page
    {
        string strcon = ConfigurationManager.ConnectionStrings["con"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                BindUserGrid();
                Training.DataBind.BindStore(storeDp);
                Training.DataBind.BindLevel(levelDb);
            }
        }

        private void BindUserGrid()
        {
            try
            {
                using (var conn = new SqlConnection(strcon))
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                    SELECT t.id,
                       t.name,
                       st.storeNo as store,
                       l.name as position
                        FROM traineeT t
                        LEFT JOIN LevelT l ON t.position = l.id
                        LEFT JOIN stores st ON t.store = st.id
                        ORDER BY t.id ASC";

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
            catch (Exception ex)
            {
                ShowMessage("Error loading data: " + ex.Message, "error");
            }
        }

        [System.Web.Services.WebMethod]
        public static List<object> GetTraineeTopics(int traineeId)
        {
            var topics = new List<object>();
            string connStr = ConfigurationManager.ConnectionStrings["con"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string query = @"
                    SELECT 
                        t.id AS id,
                        t.topicName,
                        ISNULL(tp.status, 'Registered') AS Status,
                        ISNULL(tp.exam, 'Not Taken') AS Exam
                    FROM topicWLT w
                    INNER JOIN traineeT tr ON tr.id = @traineeId
                    INNER JOIN TopicT t ON t.id = w.topic
                    LEFT JOIN traineeTopicT tp 
                        ON tp.topicId = t.id AND tp.traineeId = tr.id  
                    WHERE w.traineeLevel = tr.position
                    ORDER BY t.topicName";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@traineeId", traineeId);
                    conn.Open();

                    SqlDataReader dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        topics.Add(new
                        {
                            id = dr["id"].ToString(),
                            name = dr["topicName"].ToString(),
                            status = dr["Status"].ToString(),
                            exam = dr["Exam"].ToString()
                        });
                    }
                }
            }
            return topics;
        }

        [WebMethod]
        public static string UpdateTraineeTopicStatusExam(int traineeId, int topicId, string status, string exam)
        {
            try
            {
                string connStr = ConfigurationManager.ConnectionStrings["con"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    string query = @"
                MERGE traineeTopicT AS target
                USING (SELECT @traineeId AS traineeId, @topicId AS topicId) AS source
                ON target.traineeId = source.traineeId AND target.topicId = source.topicId
                WHEN MATCHED THEN 
                    UPDATE SET status = @status, exam = @exam, updatedAt = GETDATE()
                WHEN NOT MATCHED THEN
                    INSERT (traineeId, topicId, status, exam, updatedAt)
                    VALUES (@traineeId, @topicId, @status, @exam, GETDATE());";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@traineeId", traineeId);
                        cmd.Parameters.AddWithValue("@topicId", topicId);
                        cmd.Parameters.AddWithValue("@status", (object)status ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@exam", (object)exam ?? DBNull.Value);

                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                return "success";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        //[System.Web.Services.WebMethod]
        //public static bool SaveTraineeTopics(int traineeId, List<TopicStatus> topicStatuses)
        //{
        //    if (topicStatuses == null || topicStatuses.Count == 0)
        //        return false;

        //    string connStr = ConfigurationManager.ConnectionStrings["con"].ConnectionString;

        //    using (SqlConnection conn = new SqlConnection(connStr))
        //    {
        //        conn.Open();
        //        using (SqlTransaction transaction = conn.BeginTransaction())
        //        {
        //            try
        //            {
        //                foreach (var topic in topicStatuses)
        //                {
        //                    // Use MERGE to update if exists, insert if not
        //                    string mergeQuery = @"
        //                        MERGE INTO traineeTopicT AS target
        //                        USING (SELECT @traineeId AS traineeId, @topicId AS topicId) AS source
        //                        ON target.traineeId = source.traineeId AND target.topicId = source.topicId
        //                        WHEN MATCHED THEN
        //                            UPDATE SET status = @status, exam = @exam, updatedAt = GETDATE()
        //                        WHEN NOT MATCHED THEN
        //                            INSERT (traineeId, topicId, status, exam, updatedAt)
        //                            VALUES (@traineeId, @topicId, @status, @exam, GETDATE());";

        //                    using (SqlCommand cmd = new SqlCommand(mergeQuery, conn, transaction))
        //                    {
        //                        cmd.Parameters.Add("@traineeId", SqlDbType.Int).Value = traineeId;
        //                        cmd.Parameters.Add("@topicId", SqlDbType.Int).Value = topic.TopicId;
        //                        cmd.Parameters.Add("@status", SqlDbType.NVarChar, 50).Value = topic.Status ?? (object)DBNull.Value;
        //                        cmd.Parameters.Add("@exam", SqlDbType.NVarChar, 50).Value = topic.Exam ?? (object)DBNull.Value;
        //                        cmd.ExecuteNonQuery();
        //                    }
        //                }

        //                transaction.Commit();
        //                return true;
        //            }
        //            catch (Exception ex)
        //            {
        //                transaction.Rollback();
        //                System.Diagnostics.Debug.WriteLine("Error saving topics: " + ex.Message);
        //                return false;
        //            }
        //        }
        //    }
        //}

        public class TopicStatus
        {
            public int TopicId { get; set; }
            public string Status { get; set; }
            public string Exam { get; set; }
        }

        protected void GridView2_RowEditing(object sender, GridViewEditEventArgs e)
        {
            try
            {
                GridView2.EditIndex = e.NewEditIndex;
                BindUserGrid();
            }
            catch (Exception ex)
            {
                ShowMessage("Error entering edit mode: " + ex.Message, "error");
            }
        }

        protected void GridView2_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            try
            {
                int id = Convert.ToInt32(GridView2.DataKeys[e.RowIndex].Value);
                GridViewRow row = GridView2.Rows[e.RowIndex];

                // Find controls
                TextBox txtName = (TextBox)row.FindControl("txtName");
                DropDownList storeDb = (DropDownList)row.FindControl("storeDp");
                DropDownList PositionDb = (DropDownList)row.FindControl("PositionDb");

                if (txtName == null || storeDb == null || PositionDb == null)
                {
                    ShowMessage("Could not find form controls!", "error");
                    return;
                }

                using (SqlConnection con = new SqlConnection(strcon))
                {
                    string query = "UPDATE traineeT SET name=@name, store=@store, position=@level WHERE id=@id";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@name", txtName.Text.Trim());
                        cmd.Parameters.AddWithValue("@store", storeDb.SelectedValue);
                        cmd.Parameters.AddWithValue("@level", PositionDb.SelectedValue);
                        cmd.Parameters.AddWithValue("@id", id);

                        con.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            ShowMessage("Trainee updated successfully!", "success");
                        }
                        else
                        {
                            ShowMessage("No records were updated.", "info");
                        }
                    }
                }

                GridView2.EditIndex = -1;
                BindUserGrid();
            }
            catch (Exception ex)
            {
                ShowMessage("Error updating record: " + ex.Message, "error");
            }
        }

        protected void GridView2_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow &&
                (e.Row.RowState & DataControlRowState.Edit) == DataControlRowState.Edit)
            {
                DropDownList storeDp = (DropDownList)e.Row.FindControl("storeDp");
                if (storeDp != null)
                {
                    Training.DataBind.BindStore(storeDp); 
                    DataRowView rowView = (DataRowView)e.Row.DataItem;
                    if (rowView["store"] != DBNull.Value)
                    {
                        storeDp.SelectedValue = rowView["store"].ToString();
                    }
                }

                DropDownList PositionDb = (DropDownList)e.Row.FindControl("PositionDb");
                if (PositionDb != null)
                {
                    DataRowView drv = (DataRowView)e.Row.DataItem;
                    if (drv["position"] != DBNull.Value)
                    {
                        string posValue = drv["position"].ToString();
                        if (PositionDb.Items.FindByValue(posValue) != null)
                        {
                            PositionDb.SelectedValue = posValue;
                        }
                    }
                }
            }
        }

        protected void GridView2_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            try
            {
                int id = Convert.ToInt32(GridView2.DataKeys[e.RowIndex].Value);

                using (SqlConnection con = new SqlConnection(strcon))
                {
                    string query = "DELETE FROM traineeT WHERE id = @id";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                ShowMessage("Trainee deleted successfully!", "success");
                BindUserGrid();
            }
            catch (Exception ex)
            {
                ShowMessage("Error deleting record: " + ex.Message, "error");
            }
        }

        protected void GridView2_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            GridView2.EditIndex = -1;
            BindUserGrid();
        }

        protected void btnaddTrainee_Click(object sender, EventArgs e)
        {
            try
            {
                string trainee = traineeName.Text.Trim();
                int level = levelDb.SelectedIndex;
                string store = storeDp.SelectedValue;

                if (string.IsNullOrEmpty(trainee) || string.IsNullOrEmpty(store))
                {
                    ShowMessage("All fields are required!", "error");
                    return;
                }

                using (SqlConnection con = new SqlConnection(strcon))
                {
                    string query = "INSERT INTO traineeT (name, store, position) VALUES (@name, @store, @level)";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@name", trainee);
                        cmd.Parameters.AddWithValue("@store", store);
                        cmd.Parameters.AddWithValue("@level", level);
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                ShowMessage("Trainee added successfully!", "success");
                traineeName.Text = "";
                levelDb.SelectedIndex = 0;
                storeDp.SelectedIndex = 0;
                BindUserGrid();

                // Close the modal
                ScriptManager.RegisterStartupScript(this, GetType(), "closeModal",
                    "$('#traineeModal').modal('hide');", true);
            }
            catch (Exception ex)
            {
                ShowMessage("Error adding trainee: " + ex.Message, "error");
            }
        }

        private void ShowMessage(string message, string type)
        {
            // Encode message to prevent JavaScript errors
            string safeMessage = HttpUtility.JavaScriptStringEncode(message);
            string script = $"swal('{type.ToUpper()}', '{safeMessage}', '{type}');";

            ScriptManager.RegisterStartupScript(this, GetType(), "showMessage", script, true);
        }
    }
}