using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Web.UI.WebControls;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Web.Script.Services;
using System.Web.Services;
using Newtonsoft.Json;
using System.Web.UI;
using System.Web;

namespace Expiry_list.Training
{
    public partial class scheduleList : System.Web.UI.Page
    {
        string strcon = ConfigurationManager.ConnectionStrings["con"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                GridView2.DataSource = new List<string>();
                GridView2.DataBind();

                BindGrid();
                Training.DataBind.BindTrainer(trainerDp); 
                Training.DataBind.BindRoom(locationDp);
                Training.DataBind.BindTopic(topicName);
            }
        }

        private void BindGrid()
        {
            GridView2.DataSource = GetAllItems();
            GridView2.DataBind();
        }

        protected void btnLoadTrainees_Click(object sender, EventArgs e)
        {
            int scheduleId;
            if (int.TryParse(hfSelectedScheduleId.Value, out scheduleId))
            {
                LoadScheduleTrainees(scheduleId);
                upTrainees.Update();

                // Initialize DataTable after UpdatePanel refresh
                ScriptManager.RegisterStartupScript(this, GetType(), "initDT2", "initializeDataTable2();", true);
            }
        }

        protected void LoadScheduleTrainees(int scheduleId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(strcon))
                using (SqlCommand cmd = new SqlCommand(@"
                    SELECT t.id,
                        tr.name,
                        st.storeNo   AS store,
                        lv.name   AS position,
                        t.status,
                        t.exam
                    FROM traineeTopicT t
                    JOIN traineeT tr ON t.traineeId = tr.id
                    LEFT JOIN stores st ON tr.store = st.id
                    LEFT JOIN levelT lv ON tr.position = lv.id
                    WHERE t.scheduleId = @ScheduleId;
                    ", conn))
                {
                    cmd.Parameters.AddWithValue("@scheduleId", scheduleId);

                    conn.Open();
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    gvScheduleTrainees.DataSource = dt;
                    gvScheduleTrainees.DataBind();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while fetching trainees: " + ex.Message);
            }
        }

        protected void gvScheduleTrainees_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
            //    HiddenField hf = (HiddenField)e.Row.FindControl("hfTopicId");
            //    hf.Attributes["class"] = "hidden-field";

                gvScheduleTrainees.UseAccessibleHeader = true;
                gvScheduleTrainees.HeaderRow.TableSection = TableRowSection.TableHeader;

            DropDownList ddlStatus = (DropDownList)e.Row.FindControl("ddlStatus");
                DropDownList ddlExam = (DropDownList)e.Row.FindControl("ddlExam");

                DataRowView drv = (DataRowView)e.Row.DataItem;

                string status = drv["status"].ToString();
                string exam = drv["exam"].ToString();

                if (ddlStatus.Items.FindByValue(status) != null)
                    ddlStatus.SelectedValue = status;

                if (ddlExam.Items.FindByValue(exam) != null)
                    ddlExam.SelectedValue = exam;
            }
        }

        protected void ddlStatus_SelectedIndexChanged(object sender, EventArgs e)
        {
            DropDownList ddl = sender as DropDownList;
            GridViewRow row = ddl.NamingContainer as GridViewRow;
            int traineeId = Convert.ToInt32(gvScheduleTrainees.DataKeys[row.RowIndex].Value);

            string newStatus = ddl.SelectedValue;

            UpdateTraineeStatusOrExam(traineeId, newStatus, null);

            // Rebind GridView
            int scheduleId = Convert.ToInt32(hfSelectedScheduleId.Value);
            LoadScheduleTrainees(scheduleId);
            upTrainees.Update();

            // Re-initialize DataTables on client
            ScriptManager.RegisterStartupScript(this, GetType(), "initDT2", "initializeDataTable2();", true);
        }

        protected void ddlExam_SelectedIndexChanged(object sender, EventArgs e)
        {
            DropDownList ddl = sender as DropDownList;
            GridViewRow row = ddl.NamingContainer as GridViewRow;
            int traineeId = Convert.ToInt32(gvScheduleTrainees.DataKeys[row.RowIndex].Value);

            string newExam = ddl.SelectedValue;

            UpdateTraineeStatusOrExam(traineeId, null, newExam);

            int scheduleId = Convert.ToInt32(hfSelectedScheduleId.Value);
            LoadScheduleTrainees(scheduleId);
            upTrainees.Update();

            // Re-initialize DataTables on client
            ScriptManager.RegisterStartupScript(this, GetType(), "initDT2", "initializeDataTable2();", true);


        }

        private void UpdateTraineeStatusOrExam(int traineeId, string status, string exam)
        {

            using (SqlConnection con = new SqlConnection(strcon))
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = con;

                    // Build dynamic query based on which value changed
                    if (status != null)
                        cmd.CommandText = "UPDATE traineeTopicT SET status=@status WHERE id=@id";
                    else if (exam != null)
                        cmd.CommandText = "UPDATE traineeTopicT SET exam=@exam WHERE id=@id";

                    cmd.Parameters.AddWithValue("@id", traineeId);

                    if (status != null) cmd.Parameters.AddWithValue("@status", status);
                    if (exam != null) cmd.Parameters.AddWithValue("@exam", exam);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<Trainee> GetTrainee(string searchTerm, int positionId)
        {
            try
            {
                var trainees = new List<Trainee>();
                string connectionString = ConfigurationManager.ConnectionStrings["con"].ConnectionString;

                // get current username
                string username = HttpContext.Current.User.Identity.Name;

                // check if user is admin
                bool isAdmin = false;
                var allowedForms = GetAllowedFormsByUser(username);
                if (allowedForms.Values.Contains("admin") || allowedForms.Values.Contains("super") || username.Equals("admin", StringComparison.OrdinalIgnoreCase))
                {
                    isAdmin = true;
                }

                // get logged-in user's store list
                List<string> storeNos = GetLoggedInUserStoreNames();

                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();

                    string sql = @"
                    SELECT t.id, t.name, t.position AS positionId, p.name AS position, st.storeNo as store
                    FROM traineeT t
                    LEFT JOIN levelT p ON t.position = p.id
                    LEFT JOIN stores st ON t.store = st.id
                    WHERE t.name LIKE @SearchTerm
                    AND (@PositionId = 0 OR t.position = @PositionId)";

                    // apply store restriction if not admin
                    if (!isAdmin && storeNos.Any())
                    {
                        sql += $" AND st.storeNo IN ({string.Join(",", storeNos.Select((s, i) => $"@store{i}"))})";
                    }

                    sql += " ORDER BY t.name";

                    using (var cmd = new SqlCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@SearchTerm", "%" + searchTerm + "%");
                        cmd.Parameters.AddWithValue("@PositionId", positionId);

                        if (!isAdmin && storeNos.Any())
                        {
                            for (int i = 0; i < storeNos.Count; i++)
                            {
                                cmd.Parameters.AddWithValue($"@store{i}", storeNos[i]);
                            }
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var trainee = new Trainee
                                {
                                    Id = reader["id"].ToString(),
                                    Name = reader["name"]?.ToString(),
                                    Position = reader["position"]?.ToString(),
                                    Store = reader["store"]?.ToString()
                                };
                                trainees.Add(trainee);
                            }
                        }
                    }
                }

                return trainees;
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while fetching trainees: " + ex.Message);
            }
        }

        // Trainee class definition
        public class Trainee
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Position { get; set; }
            public string Store { get; set; }
        }

        // Get all items
        private DataTable GetAllItems()
        {
            DataTable dt = new DataTable();
            string query = @"
                SELECT 
                    s.id,
                    s.tranNo,
                    tw.id AS topicId,
                    t.topicName AS topicName,
                    ISNULL(l.name, '') AS traineeLevel, 
                    ISNULL(tr.name, '') AS trainerName,  
                    s.position AS positionId,
                    ISNULL(l2.name, '') AS position,   
                    s.description,
                    lo.name AS room,
                    s.date,
                    s.time
                FROM scheduleT s
                INNER JOIN topicWLT tw 
                    ON s.topicName = tw.id 
                INNER JOIN topicT t
                    ON tw.topic = t.id      
                LEFT JOIN trainerT tr       
                    ON tw.trainerId = tr.id 
                LEFT JOIN levelT l          
                    ON tw.traineeLevel = l.id  
                LEFT JOIN levelT l2        
                    ON s.position = l2.id     
                INNER JOIN locationT lo
                    ON s.room = lo.id
                ORDER BY s.id ASC;
            ";

            using (SqlConnection conn = new SqlConnection(strcon))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                {
                    adapter.Fill(dt);
                }
            }
            return dt;
        }

        // Get filtered data
        private DataTable GetFilteredData(string filterDate, string filterTime, int filterTopicId, int filterStoreId, int filterTrainerId)
        {
            DataTable dt = new DataTable();
            var whereClauses = new List<string>();
            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrEmpty(filterDate))
            {
                DateTime parsedDate;
                if (DateTime.TryParse(filterDate, out parsedDate))
                {
                    whereClauses.Add("CAST(s.date AS DATE) = @Date");
                    parameters.Add(new SqlParameter("@Date", parsedDate.Date));
                }
            }

            if (!string.IsNullOrEmpty(filterTime))
            {
                whereClauses.Add("s.time = @Time");
                parameters.Add(new SqlParameter("@Time", filterTime));
            }

            if (filterTopicId > 0)
            {
                whereClauses.Add("t.id = @TopicId");
                parameters.Add(new SqlParameter("@TopicId", filterTopicId));
            }

            if (filterStoreId > 0)
            {
                whereClauses.Add("lo.id = @StoreId");
                parameters.Add(new SqlParameter("@StoreId", filterStoreId));
            }

            if (filterTrainerId > 0)
            {
                whereClauses.Add("tr.id = @TrainerId");
                parameters.Add(new SqlParameter("@TrainerId", filterTrainerId));
            }

            string where = whereClauses.Count > 0 ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";

            string query = $@"
                SELECT 
                    s.id,
                    s.tranNo,
                    tw.topic AS topicId,        -- keep for JS
                    s.position AS positionId,   -- keep for JS
                    t.topicName AS topicName,
                    s.description,
                    lo.name AS room,
                    ISNULL(tr.name, '') AS trainerName,
                    ISNULL(l.name, '') AS traineeLevel,
                    ISNULL(l2.name, '') AS position,
                    s.date,
                    s.time
                FROM scheduleT s
                INNER JOIN topicWLT tw ON s.topicName = tw.id  
                INNER JOIN topicT t ON tw.topic = t.id     
                LEFT JOIN trainerT tr ON tw.trainerId = tr.id 
                LEFT JOIN levelT l ON tw.traineeLevel = l.id 
                LEFT JOIN levelT l2 ON s.position = l2.id    
                INNER JOIN locationT lo ON s.room = lo.id 
                {where}
                ORDER BY s.id ASC";

            using (SqlConnection conn = new SqlConnection(strcon))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddRange(parameters.ToArray());
                conn.Open();
                using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                {
                    adapter.Fill(dt);
                }
            }

            return dt;
        }

        protected void showBtn_Click(object sender, EventArgs e)
        {
            string filterDate = dateTb.Text.Trim();
            string filterTime = timeDp.SelectedValue;
            int filterTopicId = string.IsNullOrEmpty(topicName.SelectedValue) ? 0 : Convert.ToInt32(topicName.SelectedValue);
            int filterStoreId = string.IsNullOrEmpty(locationDp.SelectedValue) ? 0 : Convert.ToInt32(locationDp.SelectedValue);
            int filterTrainerId = string.IsNullOrEmpty(trainerDp.SelectedValue) ? 0 : Convert.ToInt32(trainerDp.SelectedValue);

            DataTable dt = GetFilteredData(filterDate, filterTime, filterTopicId, filterStoreId, filterTrainerId);

            if (dt.Rows.Count == 0)
            {
                dt.Rows.Add(dt.NewRow());
                GridView2.DataSource = dt;
                GridView2.DataBind();

                GridView2.Rows[0].Visible = false;
            }
            else
            {
                GridView2.DataSource = dt;
                GridView2.DataBind();
            }
        }

        protected void resetBtn_Click(object sender, EventArgs e)
        {
            dateTb.Text = string.Empty;
            timeDp.SelectedIndex = 0;
            topicName.SelectedIndex = 0;
            locationDp.SelectedIndex = 0;
            trainerDp.SelectedIndex = 0;
            BindGrid();
        }

        protected void btnSaveRegister_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(hfScheduleId.Value, out int scheduleId))
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), "alert",
                    "Swal.fire('Invalid Schedule Id!', '', 'error');", true);
                return;
            }

            if (!int.TryParse(hfTopicId.Value, out int topicId))
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), "alert",
                    "Swal.fire('Invalid Topic Id!', '', 'error');", true);
                return;
            }

            string selectedJson = hfSelectedTrainees.Value;
            if (string.IsNullOrWhiteSpace(selectedJson))
                return;

            var selectedTrainees = JsonConvert.DeserializeObject<List<Trainee>>(selectedJson);

            using (SqlConnection conn = new SqlConnection(strcon))
            {
                conn.Open();

                // Collect duplicate trainee NAMES instead of IDs
                List<string> duplicateTrainees = new List<string>();

                foreach (var trainee in selectedTrainees)
                {
                    if (!int.TryParse(trainee.Id?.ToString(), out int traineeId))
                        continue;

                    // Validate trainee exists
                    using (SqlCommand checkTrainee = new SqlCommand("SELECT COUNT(1) FROM traineeT WHERE id=@traineeId", conn))
                    {
                        checkTrainee.Parameters.AddWithValue("@traineeId", traineeId);
                        if (Convert.ToInt32(checkTrainee.ExecuteScalar()) == 0)
                            continue;
                    }

                    // Check duplicate registration
                    using (SqlCommand checkDup = new SqlCommand(@"
                SELECT COUNT(1)
                FROM traineeTopicT
                WHERE traineeId=@traineeId AND topicId=@topicId AND scheduleId=@scheduleId", conn))
                    {
                        checkDup.Parameters.AddWithValue("@traineeId", traineeId);
                        checkDup.Parameters.AddWithValue("@topicId", topicId);
                        checkDup.Parameters.AddWithValue("@scheduleId", scheduleId);

                        if (Convert.ToInt32(checkDup.ExecuteScalar()) > 0)
                        {
                            // Add trainee NAME instead of ID
                            duplicateTrainees.Add(trainee.Name ?? traineeId.ToString());
                        }
                    }
                }

                if (duplicateTrainees.Count > 0)
                {
                    string dupList = string.Join(", ", duplicateTrainees);
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "dupAlert",
                        $"Swal.fire('Duplicate Registration', 'The following trainee(s) are already registered: {dupList}', 'warning');", true);
                    return;
                }

                // Insert trainees
                foreach (var trainee in selectedTrainees)
                {
                    if (!int.TryParse(trainee.Id?.ToString(), out int traineeId))
                        continue;

                    string query = @"
                INSERT INTO traineeTopicT
                (traineeId, topicId, scheduleId, status, exam, updatedAt, updatedBy)
                VALUES
                (@traineeId, @topicId, @scheduleId, @status, @exam, @updatedAt, @updatedBy)";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@traineeId", traineeId);
                        cmd.Parameters.AddWithValue("@topicId", topicId);
                        cmd.Parameters.AddWithValue("@scheduleId", scheduleId);
                        cmd.Parameters.AddWithValue("@status", "Registered");
                        cmd.Parameters.AddWithValue("@exam", "Not Taken");
                        cmd.Parameters.AddWithValue("@updatedAt", DateTime.Now);
                        cmd.Parameters.AddWithValue("@updatedBy", User.Identity.Name);

                        cmd.ExecuteNonQuery();
                    }
                }
            }

            ScriptManager.RegisterStartupScript(this, this.GetType(), "successAlert",
                "Swal.fire('Success!', 'Trainees registered successfully.', 'success');", true);

            BindGrid();
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