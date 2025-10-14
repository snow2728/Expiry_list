using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Script.Services;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json;

namespace Expiry_list.Training
{
    public partial class approve : System.Web.UI.Page
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

            var formPermissions = Session["formPermissions"] as Dictionary<string, string>;
            string perm = formPermissions != null && formPermissions.ContainsKey("TrainingList")
                ? formPermissions["TrainingList"]
                : null;

            // Hide Action Column for view-only permissions
            int actionsColIndex = GridView2.Columns.Cast<DataControlField>()
                .ToList()
                .FindIndex(c => c.HeaderText == "Actions");

            if (actionsColIndex >= 0)
                GridView2.Columns[actionsColIndex].Visible = !(perm == "view" || string.IsNullOrEmpty(perm));

            DataTable dt = GetFilteredData(filterMonth, filterLevel, filterTopicId, filterStoreId, filterTrainerId);

            if (dt.Rows.Count == 0)
            {
                // Show no data message only if there are active filters
                bool hasActiveFilters = filterLevel > 0 || filterTopicId > 0 || filterStoreId > 0 || filterTrainerId > 0 ||
                                       !string.IsNullOrEmpty(filterMonth) && filterMonth != DateTime.Now.ToString("yyyy-MM");

                if (hasActiveFilters)
                {
                    ScriptManager.RegisterStartupScript(this, this.GetType(),
                        "NoDataAlert", "Swal.fire({ icon: 'info', title: 'No Data', text: 'No schedules found for the selected filters!' });", true);
                }

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

        protected void GridView2_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            int scheduleId = Convert.ToInt32(GridView2.DataKeys[e.RowIndex].Value);

            using (SqlConnection conn = new SqlConnection(strcon))
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    //string updateTraineeTopics = @"UPDATE traineeTopicT 
                    //                     SET status = 'Cancelled', 
                    //                         updatedAt = @updatedAt,
                    //                         updatedBy = @updatedBy
                    //                     WHERE scheduleId = @scheduleId";

                    //using (SqlCommand cmd = new SqlCommand(updateTraineeTopics, conn, transaction))
                    //{
                    //    cmd.Parameters.AddWithValue("@scheduleId", scheduleId);
                    //    cmd.Parameters.AddWithValue("@updatedAt", DateTime.Now);
                    //    cmd.Parameters.AddWithValue("@updatedBy", User.Identity.Name);
                    //    cmd.ExecuteNonQuery();
                    //}

                    string updateSchedule = @"UPDATE scheduleT 
                                    SET IsCancel = 1,
                                        updatedAt = @updatedAt,
                                        updatedBy = @updatedBy
                                    WHERE id = @scheduleId";

                    using (SqlCommand cmd = new SqlCommand(updateSchedule, conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@scheduleId", scheduleId);
                        cmd.Parameters.AddWithValue("@updatedAt", DateTime.Now);
                        cmd.Parameters.AddWithValue("@updatedBy", User.Identity.Name);
                        cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    BindGrid();

                    // Success message
                    ScriptManager.RegisterStartupScript(this, GetType(), "CancelSuccess",
                        "Swal.fire('Cancelled!', 'The schedule has been cancelled successfully.', 'success');", true);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    // Error message
                    string errorMessage = $"Failed to cancel schedule: {ex.Message}";
                    ScriptManager.RegisterStartupScript(this, GetType(), "CancelError",
                        $"Swal.fire('Error!', '{errorMessage.Replace("'", "\\'")}', 'error');", true);
                }
            }
        }

        protected void GridView2_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "CancelSchedule")
            {
                int scheduleId = Convert.ToInt32(e.CommandArgument);
                CancelSchedule(scheduleId);

                // Refresh the grid after cancel
                BindGrid();

                ScriptManager.RegisterStartupScript(this, GetType(), "CancelSuccess",
                    "Swal.fire('Cancelled!', 'The schedule has been cancelled successfully.', 'success');", true);
            }
        }

        private void CancelSchedule(int scheduleId)
        {
            using (SqlConnection conn = new SqlConnection(strcon))
            using (SqlCommand cmd = new SqlCommand(
                "UPDATE scheduleT SET IsCancel = 1, updatedAt = @updatedAt, updatedBy = @updatedBy WHERE id = @id", conn))
            {
                cmd.Parameters.AddWithValue("@id", scheduleId);
                cmd.Parameters.AddWithValue("@updatedAt", DateTime.Now);
                cmd.Parameters.AddWithValue("@updatedBy", User.Identity.Name);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
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
            s.time,
            s.IsCancel
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
        WHERE s.IsCancel = 0
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

            // Add condition to exclude cancelled schedules
            whereClauses.Add("s.IsCancel = 0");

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
                    s.time,
                    s.IsCancel
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