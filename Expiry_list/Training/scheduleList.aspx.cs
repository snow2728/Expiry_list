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
                dateTb.Text = DateTime.Now.ToString("yyyy-MM");

                Training.DataBind.BindTrainer(trainerDp);
                Training.DataBind.BindRoom(locationDp);
                Training.DataBind.BindLevel(levelDp);
                Training.DataBind.BindTopic(topicName);

                ViewState["FilterMonth"] = dateTb.Text;
                ViewState["FilterLevel"] = "0";
                ViewState["FilterTopicId"] = "0";
                ViewState["FilterStoreId"] = "0";
                ViewState["FilterTrainerId"] = "0";
                ViewState["HasFilters"] = true;

                BindGrid();
            }
            else
            {
                if (ViewState["FilterLevel"] != null && levelDp.Items.FindByValue(ViewState["FilterLevel"].ToString()) != null)
                    levelDp.SelectedValue = ViewState["FilterLevel"].ToString();

                if (ViewState["FilterTopicId"] != null && topicName.Items.FindByValue(ViewState["FilterTopicId"].ToString()) != null)
                    topicName.SelectedValue = ViewState["FilterTopicId"].ToString();

                if (ViewState["FilterStoreId"] != null && locationDp.Items.FindByValue(ViewState["FilterStoreId"].ToString()) != null)
                    locationDp.SelectedValue = ViewState["FilterStoreId"].ToString();

                if (ViewState["FilterTrainerId"] != null && trainerDp.Items.FindByValue(ViewState["FilterTrainerId"].ToString()) != null)
                    trainerDp.SelectedValue = ViewState["FilterTrainerId"].ToString();
            }
        }

        private void BindGrid()
        {
            string filterMonth = ViewState["FilterMonth"] != null ? ViewState["FilterMonth"].ToString() : DateTime.Now.ToString("yyyy-MM");
            int filterLevel = ViewState["FilterLevel"] != null ? Convert.ToInt32(ViewState["FilterLevel"]) : 0;
            int filterTopicId = ViewState["FilterTopicId"] != null ? Convert.ToInt32(ViewState["FilterTopicId"]) : 0;
            int filterStoreId = ViewState["FilterStoreId"] != null ? Convert.ToInt32(ViewState["FilterStoreId"]) : 0;
            int filterTrainerId = ViewState["FilterTrainerId"] != null ? Convert.ToInt32(ViewState["FilterTrainerId"]) : 0;

            DataTable dt = GetFilteredData(filterMonth, filterLevel, filterTopicId, filterStoreId, filterTrainerId);

            if (dt.Rows.Count == 0)
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(),
                    "NoDataAlert", "Swal.fire({ icon: 'info', title: 'No Data', text: 'No data found for the selected month!' });", true);

                dt.Rows.Add(dt.NewRow());
                GridView2.DataSource = dt;
                GridView2.DataBind();

                if (GridView2.Rows.Count > 0)
                    GridView2.Rows[0].Visible = false;
            }
            else
            {
                GridView2.DataSource = dt;
                GridView2.DataBind();
            }
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
                DropDownList ddlStatus = (DropDownList)e.Row.FindControl("ddlStatus");
                if (ddlStatus != null)
                {
                    string status = DataBinder.Eval(e.Row.DataItem, "status")?.ToString() ?? "";

                    if (ddlStatus.Items.FindByValue(status) != null)
                    {
                        ddlStatus.SelectedValue = status;
                    }
                    else
                    {
                        ddlStatus.SelectedIndex = 0; 
                    }
                }
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

                    if (status != null)
                        cmd.CommandText = "UPDATE traineeTopicT SET status=@status WHERE id=@id";

                    cmd.Parameters.AddWithValue("@id", traineeId);

                    if (status != null) cmd.Parameters.AddWithValue("@status", status);

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

                string username = HttpContext.Current.User.Identity.Name;

                bool isAdmin = false;
                var allowedForms = GetAllowedFormsByUser(username);
                if (allowedForms.Values.Contains("admin") || allowedForms.Values.Contains("super") || username.Equals("admin", StringComparison.OrdinalIgnoreCase))
                {
                    isAdmin = true;
                }

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

        // Trainee class 
        public class Trainee
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Position { get; set; }
            public string Store { get; set; }
        }

        public class TraineeJson
        {
            public string Id { get; set; }
            public string Text { get; set; }
        }

        // Get all items
        private DataTable GetAllItems()
        {
            DataTable dt = new DataTable();
            string query = @"
                SELECT 
                    s.id,
                    s.tranNo,
                    tw.id as topicWLTId,
                    tw.id AS topicId,
                    t.topicName AS topicName,
                    ISNULL(l.name, '') AS traineeLevel, 
                    ISNULL(tr.name, '') AS trainerName,  
                    s.position AS positionId,
                    ISNULL(l2.name, '') AS position,   
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
        private DataTable GetFilteredData(string filterMonth, int filterLevel, int filterTopicId, int filterStoreId, int filterTrainerId)
        {
            DataTable dt = new DataTable();
            var whereClauses = new List<string>();
            var parameters = new List<SqlParameter>();

            // Month filter (using YEAR + MONTH instead of BETWEEN)
            if (!string.IsNullOrEmpty(filterMonth))
            {
                if (DateTime.TryParse(filterMonth + "-01", out DateTime monthStart))
                {
                    whereClauses.Add("YEAR(s.date) = YEAR(@MonthDate) AND MONTH(s.date) = MONTH(@MonthDate)");
                    parameters.Add(new SqlParameter("@MonthDate", monthStart));
                }
            }

            if (filterLevel > 0)
            {
                whereClauses.Add("s.position = @Level");
                parameters.Add(new SqlParameter("@Level", filterLevel));
            }

            if (filterTopicId > 0)
            {
                whereClauses.Add("tw.topic = @TopicId");
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
                    tw.id as topicWLTId,
                    tw.topic AS topicId,      
                    s.position AS positionId,
                    t.topicName AS topicName,
                    lo.name AS room,
                    ISNULL(tr.name, '') AS trainerName,
                    ISNULL(l.name, '') AS traineeLevel,
                    ISNULL(l2.name, '') AS position,
                    s.date,
                    s.time
                FROM scheduleT s
                LEFT JOIN topicWLT tw ON s.topicName = tw.id  
                LEFT JOIN topicT t ON tw.topic = t.id     
                LEFT JOIN trainerT tr ON tw.trainerId = tr.id 
                LEFT JOIN levelT l ON tw.traineeLevel = l.id 
                LEFT JOIN levelT l2 ON s.position = l2.id    
                LEFT JOIN locationT lo ON s.room = lo.id 
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
            string filterMonth = dateTb.Text.Trim();
            int filterLevel = string.IsNullOrEmpty(levelDp.SelectedValue) ? 0 : Convert.ToInt32(levelDp.SelectedValue);
            int filterTopicId = string.IsNullOrEmpty(topicName.SelectedValue) ? 0 : Convert.ToInt32(topicName.SelectedValue);
            int filterStoreId = string.IsNullOrEmpty(locationDp.SelectedValue) ? 0 : Convert.ToInt32(locationDp.SelectedValue);
            int filterTrainerId = string.IsNullOrEmpty(trainerDp.SelectedValue) ? 0 : Convert.ToInt32(trainerDp.SelectedValue);

            // Save filter values
            ViewState["FilterMonth"] = filterMonth;
            ViewState["FilterLevel"] = filterLevel.ToString();
            ViewState["FilterTopicId"] = filterTopicId.ToString();
            ViewState["FilterStoreId"] = filterStoreId.ToString();
            ViewState["FilterTrainerId"] = filterTrainerId.ToString();
            ViewState["HasFilters"] = true;

            BindGrid();
        }

        protected void resetBtn_Click(object sender, EventArgs e)
        {
            dateTb.Text = DateTime.Now.ToString("yyyy-MM");
            levelDp.SelectedIndex = 0;
            topicName.SelectedIndex = 0;
            locationDp.SelectedIndex = 0;
            trainerDp.SelectedIndex = 0;

            ViewState["FilterMonth"] = dateTb.Text;
            ViewState["FilterLevel"] = "0";
            ViewState["FilterTopicId"] = "0";
            ViewState["FilterStoreId"] = "0";
            ViewState["FilterTrainerId"] = "0";
            ViewState["HasFilters"] = true;

            BindGrid();
        }

        protected void btnSaveRegister_Click(object sender, EventArgs e)
        {
            // Validate scheduleId
            if (!int.TryParse(hfScheduleId.Value, out int scheduleId))
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), "alert",
                    "Swal.fire('Invalid Schedule Id!', '', 'error');", true);
                return;
            }

            // Validate topicId
            if (!int.TryParse(hfTopicId.Value, out int topicId))
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), "alert",
                    "Swal.fire('Invalid Topic Id!', '', 'error');", true);
                return;
            }

            string selectedJson = hfSelectedTrainees.Value;

            // No trainees selected
            if (string.IsNullOrWhiteSpace(selectedJson))
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), "noTraineeAlert",
                    "Swal.fire('No Trainee Selected', 'Please select at least one trainee to register.', 'warning');", true);
                return;
            }

            var selectedTrainees = JsonConvert.DeserializeObject<List<TraineeJson>>(selectedJson)
                .Where(t => !string.IsNullOrWhiteSpace(t?.Id?.ToString()))
                .Select(t => new Trainee { Id = t.Id, Name = t.Text })
                .ToList();

            if (selectedTrainees.Count == 0)
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), "noTraineeAlert",
                    "Swal.fire('No Trainee Selected', 'Please select at least one trainee to register.', 'warning');", true);
                return;
            }

            using (SqlConnection conn = new SqlConnection(strcon))
            {
                conn.Open();

                List<string> blockedTrainees = new List<string>(); // trainees already registered for topic
                List<Trainee> validTrainees = new List<Trainee>(); // trainees allowed to register for this schedule
                int registeredCount = 0;

                foreach (var trainee in selectedTrainees)
                {
                    if (!int.TryParse(trainee.Id?.ToString(), out int traineeId))
                        continue;

                    // Get trainee name
                    string actualName = null;
                    using (SqlCommand cmd = new SqlCommand("SELECT name FROM traineeT WHERE id=@traineeId", conn))
                    {
                        cmd.Parameters.AddWithValue("@traineeId", traineeId);
                        actualName = cmd.ExecuteScalar()?.ToString();
                    }

                    if (string.IsNullOrEmpty(actualName))
                        continue; // skip if trainee doesn't exist

                    // Check if trainee already registered for this topic in any schedule
                    using (SqlCommand checkDup = new SqlCommand(@"
                SELECT COUNT(1)
                FROM traineeTopicT
                WHERE traineeId=@traineeId AND topicId=@topicId", conn))
                    {
                        checkDup.Parameters.AddWithValue("@traineeId", traineeId);
                        checkDup.Parameters.AddWithValue("@topicId", topicId);

                        if (Convert.ToInt32(checkDup.ExecuteScalar()) > 0)
                            blockedTrainees.Add(actualName);
                        else
                            validTrainees.Add(new Trainee { Id = traineeId.ToString(), Name = actualName });
                    }
                }

                // Insert only valid trainees into the selected schedule
                foreach (var trainee in validTrainees)
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
                        registeredCount++;
                    }
                }

                // Build result message
                string message;
                string messageType = "success";

                if (blockedTrainees.Count > 0 && registeredCount > 0)
                {
                    message = $"{registeredCount} trainee(s) registered successfully. The following trainee(s) are already registered for this topic in another schedule: {string.Join(", ", blockedTrainees)}";
                    messageType = "warning";
                }
                else if (blockedTrainees.Count > 0 && registeredCount == 0)
                {
                    message = $"No new trainees registered. The following trainee(s) are already registered for this topic in another schedule: {string.Join(", ", blockedTrainees)}";
                    messageType = "warning";
                }
                else if (registeredCount > 0)
                {
                    message = $"{registeredCount} trainee(s) registered successfully.";
                    messageType = "success";
                }
                else
                {
                    message = "No valid trainees found to register.";
                    messageType = "warning";
                }

                // Show SweetAlert
                string script = $@"
            Swal.fire({{
                icon: '{messageType}',
                title: 'Registration Result',
                text: '{message.Replace("'", "\\'")}',
                confirmButtonColor: '#3085d6'
            }});";

                ScriptManager.RegisterStartupScript(this, this.GetType(), "registrationResult", script, true);
            }

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