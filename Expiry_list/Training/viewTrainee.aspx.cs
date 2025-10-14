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
        protected void btnTogglePostBack_Click(object sender, EventArgs e)
        {
            BindUserGrid();
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
                string toggle = hdnToggleStatus.Value;

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
                    var filteredDt = new DataTable();
                    using (var da = new SqlDataAdapter(cmd))
                    using (var dt = new DataTable())
                    {
                        da.Fill(dt);
                        bool hasInactive = dt.AsEnumerable().Any(r => !Convert.ToBoolean(r["IsActive"]));
                        hdnHasInactive.Value = hasInactive ? "1" : "0";

                        if (toggle == "Inactive")
                        {
                            var inactiveRows = dt.AsEnumerable().Where(r => !Convert.ToBoolean(r["IsActive"]));
                            if (inactiveRows.Any())
                                filteredDt = inactiveRows.CopyToDataTable();
                        }
                        else
                        {
                            var activeRows = dt.AsEnumerable().Where(r => Convert.ToBoolean(r["IsActive"]));
                            if (activeRows.Any())
                                filteredDt = activeRows.CopyToDataTable();
                        }

                        if (filteredDt.Rows.Count > 0)
                        {
                            GridView2.DataSource = filteredDt;
                            GridView2.DataBind();
                        }
                        else
                        {
                            filteredDt = dt.Clone();
                            filteredDt.Rows.Add(filteredDt.NewRow());
                            GridView2.DataSource = filteredDt;
                            GridView2.DataBind();

                            if (GridView2.Rows.Count > 0)
                                GridView2.Rows[0].Visible = false;
                        }
                    }
                }

                // Hide "Actions" column
                var formPermissions = Session["formPermissions"] as Dictionary<string, string>;
                string perm = formPermissions != null && formPermissions.ContainsKey("TrainingList")
                              ? formPermissions["TrainingList"]
                              : null;

                if (perm == "super")
                {
                    GridView2.Columns[GridView2.Columns.Count - 1].Visible = false;
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
                BindLevelFilter(traineeId);
                LoadTraineeTopics(traineeId);
                upTopics.Update();

                ScriptManager.RegisterStartupScript(this, GetType(), "initDT2", "initializeDataTable2();", true);
            }
        }

        protected void LoadTraineeTopics(int traineeId)
        {
            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(strcon))
            using (SqlCommand cmd = new SqlCommand(@"
                WITH tp_unique AS (
                    SELECT tt.*, ROW_NUMBER() OVER (
                        PARTITION BY tt.topicId, tt.traineeId 
                        ORDER BY tt.updatedAt DESC, tt.id DESC
                    ) AS rn
                    FROM traineeTopicT tt
                ),
                latest_topic AS (
                    SELECT topicId, traineeId, Status, Exam
                    FROM tp_unique
                    WHERE rn = 1
                ),
                available_topics AS (
                    SELECT w.id AS topicWltId, w.topic AS topicMasterId,
                           ROW_NUMBER() OVER (PARTITION BY w.topic ORDER BY w.id) AS rn
                    FROM topicWLT w
                    INNER JOIN traineeT tr ON tr.id = @traineeId
                    WHERE w.traineeLevel = tr.position 
                      AND w.IsActive = 1
                )
                SELECT 
                    t.id AS topicId,
                    t.topicName,
                    LTRIM(RTRIM(ISNULL(tp.Status, 'Not Registered'))) AS Status,
                    LTRIM(RTRIM(ISNULL(tp.Exam, 'Not Taken'))) AS Exam
                FROM available_topics w
                INNER JOIN TopicT t ON t.id = w.topicMasterId
                LEFT JOIN latest_topic tp ON tp.topicId = w.topicWltId 
                AND tp.traineeId = @traineeId
                WHERE w.rn = 1
                ORDER BY t.topicName;", conn))
            {
                cmd.Parameters.AddWithValue("@traineeId", traineeId);
                conn.Open();

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(dt);
            }

            gvTraineeTopics.DataSource = dt;
            gvTraineeTopics.DataBind();

            foreach (DataRow row in dt.Rows)
            {
                string status = row["Status"].ToString();
                string exam = row["Exam"].ToString();
                System.Diagnostics.Debug.WriteLine($"Topic: {row["topicName"]}, Status: '{status}', Exam: '{exam}'");
            }

            bool canUpgrade = dt.Rows.Count > 0 && dt.AsEnumerable().All(r =>
            {
                string status = (r.Field<string>("Status") ?? "").Trim();
                string exam = (r.Field<string>("Exam") ?? "").Trim();
                return status.Equals("Attend", StringComparison.OrdinalIgnoreCase) &&
                       exam.Equals("Passed", StringComparison.OrdinalIgnoreCase);
            });

            System.Diagnostics.Debug.WriteLine($"=== DEBUG: Can Upgrade = {canUpgrade} ===");

            if (canUpgrade)
            {
                btnUpgrade.Enabled = true;
                btnUpgrade.CssClass = "btn btn-primary";
                btnUpgrade.ToolTip = "Click to upgrade trainee level";
            }
            else
            {
                btnUpgrade.Enabled = false;
                btnUpgrade.CssClass = "btn btn-secondary";

                var incompleteTopics = dt.AsEnumerable()
                    .Where(r =>
                    {
                        string status = (r.Field<string>("Status") ?? "").Trim();
                        string exam = (r.Field<string>("Exam") ?? "").Trim();
                        return !(status.Equals("Attend", StringComparison.OrdinalIgnoreCase) &&
                                 exam.Equals("Passed", StringComparison.OrdinalIgnoreCase));
                    })
                    .Select(r => r.Field<string>("topicName"))
                    .ToList();

                if (incompleteTopics.Any())
                {
                    btnUpgrade.ToolTip = $"Cannot upgrade - incomplete topics: {string.Join(", ", incompleteTopics)}";
                }
                else
                {
                    btnUpgrade.ToolTip = "Upgrade not available";
                }
            }
        }

        private void LoadFilteredTopics(int traineeId, string level)
        {
            DataTable dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(strcon))
            using (SqlCommand cmd = new SqlCommand(@"
                WITH tp_unique AS (
                    SELECT tt.*, ROW_NUMBER() OVER (
                        PARTITION BY tt.topicId, tt.traineeId 
                        ORDER BY tt.updatedAt DESC, tt.id DESC
                    ) AS rn
                    FROM traineeTopicT tt
                    WHERE tt.traineeId = @traineeId
                ),
                latest_topic AS (
                    SELECT topicId, traineeId, Status, Exam
                    FROM tp_unique
                    WHERE rn = 1
                ),
                available_topics AS (
                    SELECT w.id AS topicWltId, w.topic AS topicMasterId,
                           ROW_NUMBER() OVER (PARTITION BY w.topic ORDER BY w.id) AS rn
                    FROM topicWLT w
                    WHERE w.traineeLevel = @level
                      AND w.IsActive = 1
                )
                SELECT 
                    t.id AS topicId,
                    t.topicName,
                    LTRIM(RTRIM(ISNULL(tp.Status, 'Not Registered'))) AS Status,
                    LTRIM(RTRIM(ISNULL(tp.Exam, 'Not Taken'))) AS Exam
                FROM available_topics w
                INNER JOIN TopicT t ON t.id = w.topicMasterId
                LEFT JOIN latest_topic tp ON tp.topicId = w.topicWltId
                WHERE w.rn = 1
                ORDER BY t.topicName;
            ", conn))
            {
                cmd.Parameters.AddWithValue("@traineeId", traineeId);
                cmd.Parameters.AddWithValue("@level", level);
                conn.Open();

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(dt);
            }

            gvTraineeTopics.DataSource = dt;
            gvTraineeTopics.DataBind();

            btnUpgrade.Enabled = false;
            btnUpgrade.CssClass = "btn btn-secondary";
            btnUpgrade.ToolTip = "Upgrade only available for current level topics";
        }

        private void BindLevelFilter(int traineeId)
        {
            try
            {
                DataTable dt = new DataTable();

                using (SqlConnection conn = new SqlConnection(strcon))
                using (SqlCommand cmd = new SqlCommand(@"
           SELECT CAST(tr.position AS NVARCHAR) AS LevelValue,
               'Current Level: ' + ISNULL(l.name, CAST(tr.position AS NVARCHAR)) AS LevelName,
               tr.position AS LevelNum
        FROM traineeT tr
        LEFT JOIN LevelT l ON tr.position = l.id
        WHERE tr.id = @traineeId

        UNION ALL

        -- Get registered levels excluding current level to avoid duplicates
        SELECT DISTINCT CAST(w.traineeLevel AS NVARCHAR) AS LevelValue,
                        ISNULL(l.name, 'Level ' + CAST(w.traineeLevel AS NVARCHAR)) AS LevelName,
                        w.traineeLevel AS LevelNum
        FROM traineeTopicT tp
        INNER JOIN topicWLT w ON tp.topicId = w.id
        LEFT JOIN LevelT l ON w.traineeLevel = l.id
        WHERE tp.traineeId = @traineeId
          AND w.traineeLevel <> (SELECT position FROM traineeT WHERE id = @traineeId)

        ORDER BY LevelNum;
        ", conn))
                {
                    cmd.Parameters.AddWithValue("@traineeId", traineeId);
                    conn.Open();

                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(dt);
                }

                // Bind to dropdown
                ddlFilterLevle.Items.Clear();
                ddlFilterLevle.DataSource = dt;
                ddlFilterLevle.DataTextField = "LevelName";
                ddlFilterLevle.DataValueField = "LevelValue";
                ddlFilterLevle.DataBind();

                // Set selected value to current position
                using (SqlConnection conn = new SqlConnection(strcon))
                using (SqlCommand cmd = new SqlCommand("SELECT position FROM traineeT WHERE id = @traineeId", conn))
                {
                    cmd.Parameters.AddWithValue("@traineeId", traineeId);
                    conn.Open();
                    object result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        string currentPosition = result.ToString();
                        if (ddlFilterLevle.Items.FindByValue(currentPosition) != null)
                            ddlFilterLevle.SelectedValue = currentPosition;
                    }
                }

                // Add default if empty
                if (ddlFilterLevle.Items.Count == 0)
                    ddlFilterLevle.Items.Add(new ListItem("No levels found", "0"));
            }
            catch (Exception ex)
            {
                ddlFilterLevle.Items.Clear();
                ddlFilterLevle.Items.Add(new ListItem("Error loading levels", "0"));
                System.Diagnostics.Debug.WriteLine($"Error in BindLevelFilter: {ex.Message}");
            }
        }

        protected void ddlFilterLevle_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (int.TryParse(hiddenTraineeId.Value, out int traineeId))
            {
                string selectedLevel = ddlFilterLevle.SelectedValue;

                LoadFilteredTopics(traineeId, selectedLevel);
                upTopics.Update();
                ScriptManager.RegisterStartupScript(this, GetType(), "initDT2", "initializeDataTable2();", true);
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
                    SELECT tt.*,
                           ROW_NUMBER() OVER (
                               PARTITION BY tt.topicId, tt.traineeId 
                               ORDER BY tt.updatedAt DESC, tt.id DESC
                           ) AS rn
                    FROM traineeTopicT tt
                ),
                latest_topic AS (
                    SELECT topicId, traineeId, Status, Exam
                    FROM tp_unique
                    WHERE rn = 1
                ),
                available_topics AS (
                    SELECT w.id AS topicWltId, w.topic AS topicMasterId,
                           ROW_NUMBER() OVER (PARTITION BY w.topic ORDER BY w.id) AS rn
                    FROM topicWLT w
                    INNER JOIN traineeT tr ON tr.id = @traineeId
                    WHERE w.traineeLevel = tr.position
                      AND w.IsActive = 1
                )
                SELECT 
                    t.id AS topicId,
                    t.topicName,
                    ISNULL(tp.Status, 'Not Registered') AS Status,
                    ISNULL(tp.Exam, 'Not Taken') AS Exam,
                    tr.position AS Level   -- ✅ Added trainee's current level
                FROM available_topics w
                INNER JOIN TopicT t 
                        ON t.id = w.topicMasterId
                INNER JOIN traineeT tr   -- join traineeT to get the level
                        ON tr.id = @traineeId
                LEFT JOIN latest_topic tp 
                       ON tp.topicId = w.topicWltId
                      AND tp.traineeId = @traineeId
                WHERE w.rn = 1  
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
            //if (e.Row.RowType == DataControlRowType.DataRow)
            //{
            //    DropDownList ddlExam = (DropDownList)e.Row.FindControl("ddlExam");
            //    DataRowView drv = (DataRowView)e.Row.DataItem;

            //    string exam = drv["Exam"].ToString().Trim();

            //    if (ddlExam != null)
            //    {
            //        ddlExam.SelectedValue = exam;

            //        bool allPassed = gvTraineeTopics.DataSource is DataTable dt &&
            //                         dt.AsEnumerable().All(r => string.Equals(r.Field<string>("Exam")?.Trim(), "Passed", StringComparison.OrdinalIgnoreCase));

            //        if (allPassed)
            //        {
            //            ddlExam.Enabled = false;
            //            ddlExam.Attributes["title"] = "All exams are Passed. Cannot change.";
            //        }
            //    }
            //}
        }

        protected void ddlExam_SelectedIndexChanged(object sender, EventArgs e)
        {
            DropDownList ddl = sender as DropDownList;
            GridViewRow row = ddl.NamingContainer as GridViewRow;

            int traineeId = Convert.ToInt32(hiddenTraineeId.Value);
            int topicId = Convert.ToInt32(gvTraineeTopics.DataKeys[row.RowIndex].Value);
            string newExam = ddl.SelectedValue;

            string status = row.Cells[1].Text;  

            try
            {
                if ((newExam == "Passed" || newExam == "Failed") && 
                    (status.Equals("Registered", StringComparison.OrdinalIgnoreCase) ||
                     status.Equals("Not Registered", StringComparison.OrdinalIgnoreCase)))
                {
                    LoadTraineeTopics(traineeId);
                    upTopics.Update();

                    ScriptManager.RegisterStartupScript(
                        this,
                        GetType(),
                        "swalInvalid",
                        "Swal.fire({ icon: 'warning', title: 'Invalid Action', text: 'Exam cannot be set when trainee is not yet attended!' })" +
                        ".then(() => { initializeDataTable2(); });",
                        true
                    );
                    return;
                }

                bool updated = UpdateExamResult(traineeId, topicId, newExam);

                if (updated)
                {
                    LoadTraineeTopics(traineeId);
                    upTopics.Update();

                    ScriptManager.RegisterStartupScript(
                        this,
                        GetType(),
                        "swalSuccess",
                        "Swal.fire({ icon: 'success', title: 'Updated', text: 'Exam updated successfully!' })" +
                        ".then(() => { initializeDataTable2(); });", 
                        true
                    );
                    return;
                }
                else
                {
                    ScriptManager.RegisterStartupScript(
                        this,
                        GetType(),
                        "swalNotRegistered",
                        "Swal.fire({ icon: 'error', title: 'Not Registered', text: 'Please register this topic first before updating exam!' })" +
                        ".then(() => { initializeDataTable2(); });", 
                        true
                    );
                    return;
                }
            }
            catch (Exception ex)
            {
                ScriptManager.RegisterStartupScript(
                    this,
                    GetType(),
                    "swalError",
                    $"Swal.fire({{ icon: 'error', title: 'Error', text: '{ex.Message.Replace("'", "\\'")}' }})" +
                    ".then(() => { initializeDataTable2(); });", 
                    true
                );
                return;
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
                CheckBox chkIsActive = (CheckBox)row.FindControl("chkTrainee");

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

        protected void btnUpgrade_Click(object sender, EventArgs e)
        {
            int traineeId = int.Parse(hiddenTraineeId.Value);

            using (SqlConnection conn = new SqlConnection(strcon))
            {
                conn.Open();

                int currentPosition = 0;

                using (SqlCommand cmdGet = new SqlCommand(
                    "SELECT position FROM traineeT WHERE id = @traineeId", conn))
                {
                    cmdGet.Parameters.AddWithValue("@traineeId", traineeId);
                    object result = cmdGet.ExecuteScalar();
                    if (result != null)
                        currentPosition = Convert.ToInt32(result);
                }

                int newPosition = 0;
                if (currentPosition == 3)
                    newPosition = 1;
                else if (currentPosition == 1)
                    newPosition = 2;
                else if (currentPosition == 2)
                {
                    BindLevelFilter(traineeId);
                    LoadTraineeTopics(traineeId);
                    upTopics.Update();

                    ScriptManager.RegisterStartupScript(this, GetType(), "swalInfo",
                        "initializeDataTable2(); Swal.fire({ icon: 'info', title: 'Upgrade Not Allowed', text: 'Trainee is already at Senior Sale level.' });", true);
                    return;
                }
                else
                {
                    BindLevelFilter(traineeId);
                    LoadTraineeTopics(traineeId);
                    upTopics.Update();

                    ScriptManager.RegisterStartupScript(this, GetType(), "swalError",
                        "initializeDataTable2(); Swal.fire({ icon: 'error', title: 'Invalid Upgrade', text: 'Unknown current position.' });", true);
                    return;
                }

                using (SqlCommand cmdUpdate = new SqlCommand(
                    "UPDATE traineeT SET position = @newPosition WHERE id = @traineeId", conn))
                {
                    cmdUpdate.Parameters.AddWithValue("@newPosition", newPosition);
                    cmdUpdate.Parameters.AddWithValue("@traineeId", traineeId);
                    cmdUpdate.ExecuteNonQuery();
                }
            }

            BindLevelFilter(traineeId);
            LoadTraineeTopics(traineeId);
            upTopics.Update();

            ScriptManager.RegisterStartupScript(this, GetType(), "swalSuccess",
                "initializeDataTable2(); Swal.fire({ icon: 'success', title: 'Success', text: 'Trainee upgraded successfully!' });", true);
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