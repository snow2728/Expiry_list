using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;
using static Expiry_list.regeForm1;
using static Expiry_list.Training.scheduleList;

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
                string username = HttpContext.Current.User.Identity.Name;
                var allowedForms = GetAllowedFormsByUser(username);

                bool isAdmin = allowedForms.Values.Contains("admin")
                               || allowedForms.Values.Contains("super")
                               || username.Equals("admin", StringComparison.OrdinalIgnoreCase);

                List<string> storeNos = GetLoggedInUserStoreNames();

                using (var conn = new SqlConnection(strcon))
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT t.id,
                               t.name,
                               st.id AS storeId,
                               st.storeNo AS storeName,
                               l.id AS positionId,
                               l.name AS positionName,
                               t.IsActive
                        FROM traineeT t
                        LEFT JOIN LevelT l ON t.position = l.id
                        LEFT JOIN stores st ON t.store = st.id
                        WHERE 1=1"; 

                    if (!isAdmin && storeNos.Any())
                    {
                        var storeParams = storeNos.Select((s, i) => $"@store{i}").ToList();
                        cmd.CommandText += $" AND st.storeNo IN ({string.Join(",", storeParams)})";

                        for (int i = 0; i < storeNos.Count; i++)
                        {
                            cmd.Parameters.AddWithValue($"@store{i}", storeNos[i]);
                        }
                    }

                    cmd.CommandText += " ORDER BY t.id ASC";

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

        protected void btnLoadTopics_Click(object sender, EventArgs e)
        {
            int traineeId;
            if (int.TryParse(hiddenTraineeId.Value, out traineeId))
            {
                LoadTraineeTopics(traineeId);
                upTopics.Update();

                ScriptManager.RegisterStartupScript(this, GetType(), "initDT2", "initializeDataTable2();", true);
            }
        }

        protected void LoadTraineeTopics(int traineeId)
        {
            using (SqlConnection conn = new SqlConnection(strcon))
            using (SqlCommand cmd = new SqlCommand(@"
                WITH tp_unique AS (
                    SELECT *, 
                           ROW_NUMBER() OVER (PARTITION BY topicId, traineeId, scheduleId ORDER BY updatedAt DESC, id DESC) AS rn
                    FROM traineeTopicT
                )
                SELECT DISTINCT t.id AS id, 
                       t.topicName,
                       ISNULL(tp.Status, 'Not Registered') AS Status,
                       ISNULL(tp.Exam, 'Not Taken') AS Exam
                FROM topicWLT w
                INNER JOIN TopicT t ON t.id = w.topic
                LEFT JOIN tp_unique tp 
                       ON tp.topicId = w.id 
                      AND tp.traineeId = @traineeId 
                      AND tp.rn = 1
                WHERE w.traineeLevel = (SELECT position FROM traineeT WHERE id=@traineeId)
                  AND w.IsActive = 1
                ORDER BY t.topicName;", conn))                                                                                      
            {
                cmd.Parameters.AddWithValue("@traineeId", traineeId);
                conn.Open();

                DataTable dt = new DataTable();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(dt);

                gvTraineeTopics.DataSource = dt;
                gvTraineeTopics.DataBind();
            }
        }

        [WebMethod]
        public static List<object> GetTraineeTopics(int traineeId)
        {
            var topics = new List<object>();
            string connStr = ConfigurationManager.ConnectionStrings["con"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string query = @"
                    WITH tp_unique AS (
                        SELECT *, 
                               ROW_NUMBER() OVER (PARTITION BY topicId, traineeId, scheduleId ORDER BY updatedAt DESC, id DESC) AS rn
                        FROM traineeTopicT
                    )
                    SELECT t.id AS topicId, 
                           tp.id AS traineeTopicId,
                           t.topicName,
                           ISNULL(tp.Status, 'Not Registered') AS Status,
                           ISNULL(tp.Exam, 'Not Taken') AS Exam
                    FROM topicWLT w
                    INNER JOIN TopicT t ON t.id = w.topic
                    LEFT JOIN tp_unique tp 
                           ON tp.topicId = w.id 
                          AND tp.traineeId = @traineeId 
                          AND tp.rn = 1
                    WHERE w.traineeLevel = (SELECT position FROM traineeT WHERE id=@traineeId)
                      AND w.IsActive = 1
                    ORDER BY t.topicName;";

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
                            topicName = dr["topicName"].ToString(),
                            Status = dr["Status"].ToString(),
                            Exam = dr["Exam"].ToString()
                        });
                    }
                }
            }

            return topics;
        }

        protected void gvTraineeTopics_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                gvTraineeTopics.UseAccessibleHeader = true;
                if (gvTraineeTopics.HeaderRow != null)
                    gvTraineeTopics.HeaderRow.TableSection = TableRowSection.TableHeader;

                DropDownList ddlExam = (DropDownList)e.Row.FindControl("ddlExam");

                DataRowView drv = (DataRowView)e.Row.DataItem;

                string exam = drv["exam"].ToString();

                if (ddlExam != null)
                {
                    if (ddlExam.Items.FindByValue(exam) != null)
                        ddlExam.SelectedValue = exam;

                    var formPermissions = Session["formPermissions"] as Dictionary<string, string>;
                    string perm = formPermissions != null && formPermissions.ContainsKey("TrainingList")
                                  ? formPermissions["TrainingList"]
                                  : null;

                    if (perm == "edit")
                    {
                        ddlExam.Enabled = false;
                        ddlExam.CssClass += " bg-light"; 
                    }
                    else
                    {
                        ddlExam.Enabled = true;
                    }
                }
            }
        }

        protected void ddlExam_SelectedIndexChanged(object sender, EventArgs e)
        {
            DropDownList ddl = sender as DropDownList;
            GridViewRow row = ddl.NamingContainer as GridViewRow;

            int traineeId = Convert.ToInt32(hiddenTraineeId.Value);
            int topicId = Convert.ToInt32(gvTraineeTopics.DataKeys[row.RowIndex].Value);
            string newExam = ddl.SelectedValue;

            try
            {
                bool updated = UpdateExamResult(traineeId, topicId, newExam);

                if (updated)
                {
                    LoadTraineeTopics(traineeId);
                    upTopics.Update();

                    ScriptManager.RegisterStartupScript(this, GetType(), "initDT2", "initializeDataTable2();", true);
                }
                else
                {
                    ScriptManager.RegisterStartupScript(
                        this,
                        GetType(),
                        "swal",
                        "Swal.fire({ icon: 'error', title: 'Not Registered', text: 'Please register this topic first before updating exam!' });",
                        true
                    );
                }
            }
            catch (Exception ex)
            {
                ScriptManager.RegisterStartupScript(
                    this,
                    GetType(),
                    "swalError",
                    $"Swal.fire({{ icon: 'error', title: 'Error', text: '{ex.Message.Replace("'", "\\'")}' }});",
                    true
                );
            }
        }

        private bool UpdateExamResult(int traineeId, int topicId, string exam)
        {
            using (SqlConnection con = new SqlConnection(strcon))
            {
                con.Open();

                string updateQuery = @"
            UPDATE tt
            SET tt.exam = @exam,
                tt.updatedAt = GETDATE()
            FROM traineeTopicT tt
            INNER JOIN topicWLT w ON tt.topicId = w.id
            WHERE tt.traineeId = @traineeId AND w.topic = @topicTId";

                using (SqlCommand cmd = new SqlCommand(updateQuery, con))
                {
                    cmd.Parameters.AddWithValue("@exam", exam);
                    cmd.Parameters.AddWithValue("@traineeId", traineeId);
                    cmd.Parameters.AddWithValue("@topicTId", topicId);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        }

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
                int id = Convert.ToInt32(GridView2.DataKeys[e.RowIndex].Values["id"]);
                GridViewRow row = GridView2.Rows[e.RowIndex];

                TextBox txtName = (TextBox)row.FindControl("txtName");
                DropDownList storeDb = (DropDownList)row.FindControl("storeDp");
                DropDownList PositionDb = (DropDownList)row.FindControl("PositionDb");
                CheckBox chkIsActive = (CheckBox)row.FindControl("chkTopic_Enable");

                // Old values from DataKeys
                string oldName = GridView2.DataKeys[e.RowIndex].Values["name"].ToString();
                string oldStore = GridView2.DataKeys[e.RowIndex].Values["storeId"].ToString();
                string oldLevel = GridView2.DataKeys[e.RowIndex].Values["positionId"].ToString();
                bool oldIsActive = Convert.ToBoolean(GridView2.DataKeys[e.RowIndex].Values["IsActive"]);

                // Validate controls
                if (txtName == null || storeDb == null || PositionDb == null)
                {
                    ShowMessage("Could not find form controls!", "error");
                    return;
                }

                // Use new value if entered, otherwise old value
                string newName = string.IsNullOrWhiteSpace(txtName.Text) ? oldName : txtName.Text.Trim();
                string newStore = !string.IsNullOrEmpty(storeDb.SelectedValue) ? storeDb.SelectedValue : oldStore;
                string newLevel = !string.IsNullOrEmpty(PositionDb.SelectedValue) ? PositionDb.SelectedValue : oldLevel;
                bool newIsActive = chkIsActive != null ? chkIsActive.Checked : oldIsActive;

                // Debug: check values before updating
                System.Diagnostics.Debug.WriteLine($"Updating ID: {id}, Name: {newName}, Store: {newStore}, Level: {newLevel}, Active: {newIsActive}");

                using (SqlConnection con = new SqlConnection(strcon))
                {
                    string query = @"UPDATE traineeT 
                             SET name = @name, 
                                 store = @store, 
                                 position = @level, 
                                 IsActive = @isActive 
                             WHERE id = @id";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@name", newName);

                        // Store
                        if (int.TryParse(newStore, out int storeId))
                            cmd.Parameters.Add("@store", SqlDbType.Int).Value = storeId;
                        else
                            cmd.Parameters.Add("@store", SqlDbType.NVarChar).Value = newStore;

                        // Level
                        if (int.TryParse(newLevel, out int levelId))
                            cmd.Parameters.Add("@level", SqlDbType.Int).Value = levelId;
                        else
                            cmd.Parameters.Add("@level", SqlDbType.NVarChar).Value = newLevel;

                        cmd.Parameters.AddWithValue("@isActive", newIsActive);
                        cmd.Parameters.AddWithValue("@id", id);

                        con.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                            ShowMessage("Trainee updated successfully!", "success");
                        else
                            ShowMessage("No records were updated.", "info");
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

        protected void GridView2_RowDataBound(object sender, GridViewRowEventArgs e) { 
            if (e.Row.RowType == DataControlRowType.DataRow && (e.Row.RowState & DataControlRowState.Edit) == DataControlRowState.Edit) { 
                DataRowView rowView = (DataRowView)e.Row.DataItem; 
                DropDownList storeDp = (DropDownList)e.Row.FindControl("storeDp"); 
                if (storeDp != null) {
                    Training.DataBind.BindStore(storeDp); string currentStoreId = rowView["storeId"].ToString(); 
                    if (storeDp.Items.FindByValue(currentStoreId) != null) 
                        storeDp.SelectedValue = currentStoreId;
                } 

                DropDownList ddlLevel = (DropDownList)e.Row.FindControl("PositionDb"); 
                if (ddlLevel != null) { Training.DataBind.BindLevel(ddlLevel); 
                    string currentLevelId = rowView["positionId"].ToString();
                    
                if (ddlLevel.Items.FindByValue(currentLevelId) != null)
                        ddlLevel.SelectedValue = currentLevelId;
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
            string safeMessage = HttpUtility.JavaScriptStringEncode(message);
            string script = $"swal('{type.ToUpper()}', '{safeMessage}', '{type}');";

            ScriptManager.RegisterStartupScript(this, GetType(), "showMessage", script, true);
        }

        private static List<string> GetLoggedInUserStoreNames()
        {
            List<string> storeNos = HttpContext.Current.Session["storeListRaw"] as List<string>;
            List<string> storeNames = new List<string>();

            if (storeNos == null || storeNos.Count == 0)
                return storeNames;

            string strcon = ConfigurationManager.ConnectionStrings["con"].ConnectionString;
            string query = $"SELECT storeNo FROM Stores WHERE storeNo IN ({string.Join(",", storeNos.Select((s, i) => $"@store{i}"))})";

            using (SqlConnection conn = new SqlConnection(strcon))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                for (int i = 0; i < storeNos.Count; i++)
                {
                    cmd.Parameters.AddWithValue($"@store{i}", storeNos[i]);
                }

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        storeNames.Add(reader["storeNo"].ToString());
                    }
                }
            }

            return storeNames;
        }

        private static Dictionary<string, string> GetAllowedFormsByUser(string username)
        {
            Dictionary<string, string> forms = new Dictionary<string, string>();
            string strcon = ConfigurationManager.ConnectionStrings["con"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(strcon))
            {
                string query = @"
            SELECT f.name AS FormName,
                   CASE up.permission_level
                        WHEN 1 THEN 'view'
                        WHEN 2 THEN 'edit'
                        WHEN 3 THEN 'admin'
                        WHEN 4 THEN 'super'
                        WHEN 5 THEN 'super1'
                        ELSE 'none'
                   END AS Permission
            FROM UserPermissions up
            INNER JOIN Forms f ON up.form_id = f.id
            INNER JOIN Users u ON up.user_id = u.id
            WHERE u.username = @Username";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Username", username);
                conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string form = reader["FormName"].ToString();
                        string permission = reader["Permission"].ToString();
                        forms[form] = permission;
                    }
                }
            }

            return forms;
        }
    }
}