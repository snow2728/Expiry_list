using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Spreadsheet;
using OfficeOpenXml;


namespace Expiry_list.Training
{
    public partial class rege1 : System.Web.UI.Page
    {
        string strcon = ConfigurationManager.ConnectionStrings["con"].ConnectionString;
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                no.Text = GetNextTranNo();
                tdyDate.Text = DateTime.Now.ToString("dd/MM/yyyy");

                BindTopics();
                Training.DataBind.BindRoom(locationDp);
            }
        }

        protected void topicDP_SelectedIndexChanged(object sender, EventArgs e)
        {
            trainerDp.Items.Clear();
            trainerDp.Items.Add(new ListItem("Select Trainer", ""));
            position.Text = "";

            if (string.IsNullOrEmpty(topicDP.SelectedValue)) return;
            string selectedTopicName = topicDP.SelectedItem.Text;

            var dt = ViewState["TopicMapping"] as System.Data.DataTable;
            if (dt == null) return;

            var rows = dt.AsEnumerable()
                         .Where(r => r.Field<string>("topic").Equals(selectedTopicName, StringComparison.OrdinalIgnoreCase))
                         .ToList();

            if (!rows.Any()) return;

            foreach (var r in rows)
            {
                trainerDp.Items.Add(new ListItem(r.Field<string>("trainerName"), r.Field<int>("trainerId").ToString()));
            }

            // one trainer
            if (rows.Count == 1)
            {
                trainerDp.SelectedIndex = 1;
                position.Text = rows[0].Field<string>("levelName");
            }
            else
            {   // multiple trainers
                trainerDp.SelectedIndex = 0; 
                position.Text = rows[0].Field<string>("levelName");
            }
        }

        protected void btnUploadExcel_Click(object sender, EventArgs e)
        {
            if (!fuExcel.HasFile)
            {
                ShowError("Please select an Excel file to upload.");
                return;
            }

            string ext = Path.GetExtension(fuExcel.FileName).ToLower();
            if (ext != ".xlsx" && ext != ".xls")
            {
                ShowError("Only Excel files (.xlsx, .xls) are allowed.");
                return;
            }

            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                System.Data.DataTable dtExcel = new System.Data.DataTable();

                using (var package = new ExcelPackage(fuExcel.PostedFile.InputStream))
                {
                    var ws = package.Workbook.Worksheets[0];
                    if (ws.Dimension == null)
                    {
                        ShowError("Excel is empty.");
                        return;
                    }

                    // Columns - normalize header names by removing spaces
                    for (int col = 1; col <= ws.Dimension.End.Column; col++)
                    {
                        string colName = ws.Cells[1, col].Value?.ToString().Trim() ?? $"Column{col}";
                        colName = colName.Replace(" ", ""); // Remove spaces
                        dtExcel.Columns.Add(colName);
                    }

                    // Rows
                    for (int row = 2; row <= ws.Dimension.End.Row; row++)
                    {
                        if (ws.Cells[row, 1].Value == null)
                            continue;

                        DataRow dr = dtExcel.NewRow();
                        for (int col = 1; col <= ws.Dimension.End.Column; col++)
                            dr[col - 1] = ws.Cells[row, col].Value?.ToString().Trim() ?? "";

                        dtExcel.Rows.Add(dr);
                    }
                }

                bool success = InsertExcelDataToDB(dtExcel);

                if (success)
                {
                    ShowAlert("Success!", "Schedules inserted successfully!", "success");
                    Response.Redirect("scheduleList.aspx");
                }
            }
            catch (Exception ex)
            {
                ShowError("Error in Excel file: " + ex.Message);
            }
        }

        private bool InsertExcelDataToDB(System.Data.DataTable dtExcel)
        {
            var topicDict = LoadLookup("SELECT id, topicName FROM topicT");
            var trainerDict = LoadLookup("SELECT id, name FROM trainerT");
            var levelDict = LoadLookup("SELECT id, name FROM levelT");
            var roomDict = LoadLookup("SELECT id, name FROM locationT");

            var topicWLTDict = new Dictionary<string, int>();
            using (SqlConnection con = new SqlConnection(strcon))
            using (SqlCommand cmd = new SqlCommand("SELECT id, topic, trainerId, traineeLevel, IsActive FROM topicWLT", con))
            {
                con.Open();
                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        int id = rdr.GetInt32(0);
                        int topicId = rdr.GetInt32(1);
                        int trainerId = rdr.GetInt32(2);
                        int levelId = rdr.GetInt32(3);
                        bool isActive = rdr.GetBoolean(4);

                        if (isActive)
                        {
                            string key = $"{topicId}-{trainerId}-{levelId}";
                            topicWLTDict[key] = id;
                        }
                    }
                }
            }

            System.Data.DataTable dtBulk = new System.Data.DataTable();
            dtBulk.Columns.Add("tranNo", typeof(string));
            dtBulk.Columns.Add("topicName", typeof(int));
            dtBulk.Columns.Add("room", typeof(int));
            dtBulk.Columns.Add("trainerName", typeof(int));
            dtBulk.Columns.Add("position", typeof(int));
            dtBulk.Columns.Add("date", typeof(DateTime));
            dtBulk.Columns.Add("time", typeof(string));

            int lastTranNo = GetLastTranNoNumber();
            string prefix = "TS";

            foreach (DataRow row in dtExcel.Rows)
            {
                lastTranNo++;
                string tranNo = $"{prefix}-{lastTranNo}";

                string topicNameFromExcel = row["TopicName"].ToString().Trim();
                string roomFromExcel = row["Room"].ToString().Trim();
                string trainerName = row["TrainerName"].ToString().Trim();
                string position = row["Position"].ToString().Trim();
                string timeStr = row["Time"].ToString().Trim();

                // Validate Topic
                if (!topicDict.TryGetValue(topicNameFromExcel, out int topicId))
                    throw new Exception($"Topic '{topicNameFromExcel}' not found.");

                // Validate Trainer
                if (!trainerDict.TryGetValue(trainerName, out int trainerId))
                    throw new Exception($"Trainer '{trainerName}' not found.");

                // Validate Level
                if (!levelDict.TryGetValue(position, out int levelId))
                    throw new Exception($"Trainee Level '{position}' not found.");

                // Validate Room
                if (!roomDict.TryGetValue(roomFromExcel, out int roomId))
                    throw new Exception($"Training Room '{roomFromExcel}' not found.");

                // Validate TopicWLT
                string wltKey = $"{topicId}-{trainerId}-{levelId}";
                if (!topicWLTDict.TryGetValue(wltKey, out int topicWLTId))
                    throw new Exception($"No active TopicWLT mapping for Topic '{topicNameFromExcel}', Trainer '{trainerName}', Level '{position}'.");

                // Validate Date (dd-MM-yy)
                object dateObj = row["Date"];
                DateTime dateValue;

                if (dateObj == null || dateObj == DBNull.Value)
                    throw new Exception($"Date is empty for Topic '{dateObj}'");

                if (dateObj is DateTime)
                {
                    dateValue = (DateTime)dateObj;
                }
                else
                {
                    if (!DateTime.TryParse(dateObj.ToString().Trim(), out dateValue))
                        throw new Exception($"Invalid Date format for Topic '{dateObj}'");
                }

                // Validate Time
                if (!System.Text.RegularExpressions.Regex.IsMatch(
                    timeStr, @"^\d{1,2}:\d{2}\s?(AM|PM)\s?-\s?\d{1,2}:\d{2}\s?(AM|PM)$", RegexOptions.IgnoreCase))
                    throw new Exception($"Invalid Time format for Topic '{timeStr}'");


                dtBulk.Rows.Add(tranNo, topicWLTId, roomId, trainerId, levelId, dateValue, timeStr);
            }

            using (SqlConnection con = new SqlConnection(strcon))
            {
                con.Open();
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(con, SqlBulkCopyOptions.Default, null))
                {
                    bulkCopy.DestinationTableName = "scheduleT";
                    bulkCopy.ColumnMappings.Add("tranNo", "tranNo");
                    bulkCopy.ColumnMappings.Add("topicName", "topicName");
                    bulkCopy.ColumnMappings.Add("room", "room");
                    bulkCopy.ColumnMappings.Add("trainerName", "trainerName");
                    bulkCopy.ColumnMappings.Add("position", "position");
                    bulkCopy.ColumnMappings.Add("date", "date");
                    bulkCopy.ColumnMappings.Add("time", "time");
                    bulkCopy.BulkCopyTimeout = 300;
                    bulkCopy.WriteToServer(dtBulk);
                }
            }

            return true;
        }

        private class TopicWLTInfo
        {
            public int Id { get; set; }     
            public string TopicName { get; set; } 
            public int TrainerId { get; set; }
            public int LevelId { get; set; }
            public bool IsActive { get; set; }
        }
        private int GetLastTranNoNumber()
        {
            string prefix = "TS";
            string query = @"SELECT MAX(CAST(SUBSTRING(tranNo, CHARINDEX('-', tranNo) + 1, LEN(tranNo)) AS INT))
                     FROM scheduleT WHERE tranNo LIKE @pattern";

            using (SqlConnection conn = new SqlConnection(strcon))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                cmd.Parameters.AddWithValue("@pattern", prefix + "-%");
                object result = cmd.ExecuteScalar();
                return result == DBNull.Value ? 0 : Convert.ToInt32(result);
            }
        }

        private Dictionary<string, int> LoadLookup(string query)
        {
            var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            using (SqlConnection con = new SqlConnection(strcon))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int id = reader.GetInt32(0);
                        string name = reader.GetString(1).Trim();
                        dict[name] = id;
                    }
                }
            }
            return dict;
        }

        private Dictionary<string, int> LoadTopicWLTDict() { 
            var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            
            using (SqlConnection con = new SqlConnection(strcon)) 
            using (SqlCommand cmd = new SqlCommand("SELECT id, topic, trainerId, traineeLevel,IsActive FROM topicWLT", con)) { 
                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader()) {
                    while (reader.Read()) {
                        int id = reader.GetInt32(0); int topicId = reader.GetInt32(1);
                        int trainerId = reader.GetInt32(2);
                        int levelId = reader.GetInt32(3); 
                        string key = $"{topicId}-{trainerId}-{levelId}";
                        dict[key] = id; 
                    } 
                } 
            } 
            return dict;
        }

        private void ShowError(string message)
        {
            ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorMsg",
                "swal('Error', '" + message.Replace("'", "\\'") + "', 'error').then(function(){ window.location=window.location.href; });", true);
        }

        protected void createBtn_Click(object sender, EventArgs e)
        {
            try
            {
                string tranNo = no.Text.Trim();
                string topicId = topicDP.SelectedValue;
                string locationName = locationDp.SelectedItem.Text.Trim();
                string date1 = date.Text.Trim();
                string time1 = timeDp.SelectedValue;

                if (string.IsNullOrEmpty(topicId) || string.IsNullOrEmpty(locationName) ||
                    string.IsNullOrEmpty(time1) || string.IsNullOrEmpty(trainerDp.SelectedValue))
                {
                    ShowAlert("Error!", "Topic, Trainer, Training Room and Time are required!", "error");
                    return;
                }

                using (SqlConnection con = new SqlConnection(strcon))
                {
                    con.Open();

                    int trainerId = Convert.ToInt32(trainerDp.SelectedValue);
                    int level = 0;
                    int topicWLTId = 0;

                    string query = @"SELECT TOP 1 id, traineeLevel 
                             FROM topicWLT 
                             WHERE topic = (SELECT topic FROM topicWLT WHERE id = @topicId)
                               AND trainerId = @trainerId";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@topicId", topicId);
                        cmd.Parameters.AddWithValue("@trainerId", trainerId);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                topicWLTId = Convert.ToInt32(reader["id"]);
                                level = Convert.ToInt32(reader["traineeLevel"]);
                            }
                            else
                            {
                                ShowAlert("Error!", "No TopicWLT mapping found for the selected Topic & Trainer!", "error");
                                return;
                            }
                        }
                    }

                    // Get locationId
                    string locationId = "";
                    string locQuery = @"SELECT id FROM locationT WHERE LOWER(name) = LOWER(@locationName)";
                    using (SqlCommand locCmd = new SqlCommand(locQuery, con))
                    {
                        locCmd.Parameters.AddWithValue("@locationName", locationName);
                        object result = locCmd.ExecuteScalar();
                        if (result != null)
                            locationId = result.ToString();
                        else
                        {
                            ShowAlert("Error!", "Invalid Training Room selected!", "error");
                            return;
                        }
                    }

                    // Insert schedule
                    string insertQuery = @"INSERT INTO scheduleT 
                                   (tranNo, topicName, room, trainerName, position, date, time)
                                   VALUES 
                                   (@tranNo, @topicName, @room, @trainerId, @position, @date, @time)";

                    using (SqlCommand insertCmd = new SqlCommand(insertQuery, con))
                    {
                        insertCmd.Parameters.AddWithValue("@tranNo", tranNo);
                        insertCmd.Parameters.AddWithValue("@topicName", topicWLTId);
                        insertCmd.Parameters.AddWithValue("@room", locationId);
                        insertCmd.Parameters.AddWithValue("@trainerId", trainerId);
                        insertCmd.Parameters.AddWithValue("@position", level);
                        insertCmd.Parameters.AddWithValue("@date", date1);
                        insertCmd.Parameters.AddWithValue("@time", time1);

                        insertCmd.ExecuteNonQuery();
                    }

                    ShowAlert("Success!", "Schedule created successfully!", "success");
                    ClearForm();
                    no.Text = GetNextTranNo();
                    tdyDate.Text = DateTime.Now.ToString("dd/MM/yyyy");
                }
            }
            catch (Exception ex)
            {
                ShowAlert("Error!", $"Failed to create schedule: {ex.Message}", "error");
            }
        }

        protected void BindTopics()
        {
            using (SqlConnection con = new SqlConnection(strcon))
            {
                con.Open();
                string query = @"SELECT w.id, tp.topicName as topic, w.trainerId, w.traineeLevel, w.IsActive,
                         tr.name as trainerName, l.name as levelName 
                         FROM topicWLT w 
                         INNER JOIN topicT tp ON w.topic = tp.id
                         INNER JOIN trainerT tr ON w.trainerId = tr.id
                         INNER JOIN levelT l ON w.traineeLevel = l.id where w.IsActive='true';";

                SqlDataAdapter da = new SqlDataAdapter(query, con);
                System.Data.DataTable dt = new System.Data.DataTable();
                da.Fill(dt);

                ViewState["TopicMapping"] = dt;

                topicDP.Items.Clear();
                topicDP.Items.Add(new ListItem("Select Topic", ""));

                var topics = dt.AsEnumerable()
                               .Where(r => r.Field<bool>("IsActive"))
                               .GroupBy(r => r.Field<string>("topic")) 
                               .Select(g => g.First())                
                               .ToList();

                foreach (var t in topics)
                    topicDP.Items.Add(new ListItem(t.Field<string>("topic"), t.Field<int>("id").ToString()));
            }
        }

        private string GetNextTranNo()
        {
            string prefix = "TS";
            string query = @"SELECT MAX(CAST(SUBSTRING(tranNo, CHARINDEX('-', tranNo) + 1, LEN(tranNo)) AS INT))
                     FROM scheduleT
                     WHERE tranNo LIKE @pattern";

            using (SqlConnection conn = new SqlConnection(strcon))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                cmd.Parameters.AddWithValue("@pattern", prefix + "-%");
                object result = cmd.ExecuteScalar();

                int lastNumber = result == DBNull.Value ? 0 : Convert.ToInt32(result);
                int nextNumber = lastNumber + 1;

                return $"{prefix}-{nextNumber}";
            }
        }

        private void ShowAlert(string title, string message, string type)
        {
            ScriptManager.RegisterStartupScript(this, GetType(), "alert",
                $"swal('{title}', '{HttpUtility.JavaScriptStringEncode(message)}', '{type}');", true);
        }

        protected void ClearForm()
        {
            topicDP.SelectedIndex = 0;
            trainerDp.Text = "";
            position.Text = "";
            date.Text = string.Empty;
            timeDp.SelectedIndex = 0;
            locationDp.Text = "";
        }

    }
}