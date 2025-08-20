using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json;
using ClosedXML.Excel;
using System.Diagnostics;
using System.IO;

namespace Expiry_list.Training
{
    public partial class scheduleList : System.Web.UI.Page
    {
        string strcon = ConfigurationManager.ConnectionStrings["con"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Request.HttpMethod == "POST" && !string.IsNullOrEmpty(Request["draw"]))
            {
                RespondWithData();
                return;
            }

            if (!IsPostBack)
            {
                BindTopic();
            }
        }

        private void BindTopic()
        {
            using (SqlConnection con = new SqlConnection(strcon))
            {
                con.Open();
                string query = "SELECT topicName FROM topicT ORDER BY topicName";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    topicName.DataSource = cmd.ExecuteReader();
                    topicName.DataTextField = "topicName";
                    topicName.DataValueField = "topicName";
                    topicName.DataBind();
                }
                topicName.Items.Insert(0, new ListItem("Select Topic", ""));
            }
        }

        private void RespondWithData()
        {
            using (SqlConnection conn = new SqlConnection(strcon))
            {
                int draw = Convert.ToInt32(Request["draw"]);
                int start = Convert.ToInt32(Request["start"]);
                int len = Convert.ToInt32(Request["length"]);
                if (len == 0) len = 100;
                int orderCol = Convert.ToInt32(Request["order[0][column]"]);
                string orderDir = (Request["order[0][dir]"] ?? "asc").ToLower();

                // Get filter parameters from request
                string filterDate = Request.Form["filterDate"] ?? "";
                string filterTime = Request.Form["filterTime"] ?? "";
                string filterTopic = Request.Form["filterTopic"] ?? "";
                string filterLocation = Request.Form["filterLocation"] ?? "";

                string username = User.Identity.Name;
                Dictionary<string, string> perms = GetAllowedFormsByUser(username);

                if (!perms.TryGetValue("TrainingList", out string perm))
                {
                    Response.Write("{\"error\":\"Unauthorized\"}");
                    Response.End();
                    return;
                }

                // Get user's stores
                List<string> storeNos = GetLoggedInUserStoreNames();
                bool isHOUser = storeNos.Any(s => s.Equals("HO", StringComparison.OrdinalIgnoreCase));

                string searchValue = Request["search[value]"] ?? "";

                var whereClauses = new List<string>();
                var parameters = new List<SqlParameter>();

                // Apply store permissions
                if (!isHOUser && storeNos.Count > 0)
                {
                    whereClauses.Add($"room IN ({string.Join(",", storeNos.Select((s, i) => $"@store{i}"))})");
                    for (int i = 0; i < storeNos.Count; i++)
                    {
                        parameters.Add(new SqlParameter($"@store{i}", storeNos[i]));
                    }
                }

                // Apply date filter
                if (!string.IsNullOrEmpty(filterDate))
                {
                    DateTime parsedDate;
                    if (DateTime.TryParse(filterDate, out parsedDate))
                    {
                        whereClauses.Add("CAST([date] AS DATE) = @Date");
                        parameters.Add(new SqlParameter("@Date", parsedDate.Date));
                    }
                }

                // Apply time filter
                if (!string.IsNullOrEmpty(filterTime))
                {
                    TimeSpan parsedTime;
                    if (TimeSpan.TryParse(filterTime, out parsedTime))
                    {
                        // Create 1-minute window for time comparison
                        TimeSpan startTime = new TimeSpan(parsedTime.Hours, parsedTime.Minutes, 0);
                        TimeSpan endTime = startTime.Add(TimeSpan.FromMinutes(1));

                        whereClauses.Add("CAST([time] AS TIME) >= @StartTime AND CAST([time] AS TIME) < @EndTime");
                        parameters.Add(new SqlParameter("@StartTime", startTime));
                        parameters.Add(new SqlParameter("@EndTime", endTime));
                    }
                }

                // Apply topic filter
                if (!string.IsNullOrEmpty(filterTopic))
                {
                    whereClauses.Add("topicName = @TopicName");
                    parameters.Add(new SqlParameter("@TopicName", filterTopic));
                }

                // Apply location filter
                if (!string.IsNullOrEmpty(filterLocation))
                {
                    whereClauses.Add("room = @Location");
                    parameters.Add(new SqlParameter("@Location", filterLocation));
                }

                // Apply search filter
                string[] searchable = { "tranNo", "topicName", "description", "room", "trainerName", "position" };
                if (!string.IsNullOrEmpty(searchValue))
                {
                    var searchConditions = searchable.Select(c => $"{c} LIKE @Search").ToList();
                    whereClauses.Add($"({string.Join(" OR ", searchConditions)})");
                    parameters.Add(new SqlParameter("@Search", "%" + searchValue + "%"));
                }

                string where = whereClauses.Count > 0 ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";

                string[] sortable = { "tranNo", "topicName", "description", "room", "trainerName", "position", "date", "time" };
                if (orderCol < 0 || orderCol >= sortable.Length)
                {
                    orderCol = 0;
                    orderDir = "asc";
                }
                string orderBy = $"ORDER BY [{sortable[orderCol]}] {(orderDir == "desc" ? "DESC" : "ASC")}";

                string sql = $@"
                    SELECT * FROM scheduleT
                    {where}
                    {orderBy}
                    OFFSET @start ROWS FETCH NEXT @len ROWS ONLY";

                var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddRange(parameters.ToArray());
                cmd.Parameters.AddWithValue("@start", start);
                cmd.Parameters.AddWithValue("@len", len);

                string countSql = $"SELECT COUNT(*) FROM scheduleT {where}";
                var countCmd = new SqlCommand(countSql, conn);
                countCmd.Parameters.AddRange(parameters.ToArray());

                conn.Open();
                DataTable dt = new DataTable();
                new SqlDataAdapter(cmd).Fill(dt);
                int total = (int)countCmd.ExecuteScalar();
                conn.Close();

                var data = dt.AsEnumerable().Select(r => new {
                    tranNo = r["tranNo"],
                    topicName = r["topicName"],
                    description = r["Description"],
                    room = r["Room"],
                    trainerName = r["TrainerName"],
                    position = r["Position"],
                    date = r["Date"] is DBNull ? null : Convert.ToDateTime(r["Date"]).ToString("yyyy-MM-dd"),
                    time = r["Time"] is DBNull ? null : ((TimeSpan)r["Time"]).ToString(@"hh\:mm\:ss")
                }).ToList();

                var response = new
                {
                    draw,
                    recordsTotal = total,
                    recordsFiltered = total,
                    data,
                    orderColumn = orderCol,
                    orderDir
                };

                Response.ContentType = "application/json";
                Response.Write(JsonConvert.SerializeObject(response));
                Response.End();
            }
        }


        private List<string> GetLoggedInUserStoreNames()
        {
            List<string> storeNos = Session["storeListRaw"] as List<string>;
            List<string> storeNames = new List<string>();

            if (storeNos == null || storeNos.Count == 0)
                return storeNames;

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

        protected void btnExport_Click(object sender, EventArgs e)
        {
            try
            {
                DataTable dt = GetAllItems();

                if (dt.Rows.Count > 0)
                {
                    using (XLWorkbook wb = new XLWorkbook())
                    {
                        var ws = wb.Worksheets.Add(dt, "ScheduleList");

                        var header = ws.Row(1);
                        header.Style.Font.Bold = true;
                        header.Style.Fill.BackgroundColor = XLColor.Black;
                        header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        ws.Columns().AdjustToContents();

                        Response.Clear();
                        Response.Buffer = true;
                        Response.Charset = "";
                        Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        Response.AddHeader("content-disposition",
                            $"attachment;filename=NegativeItems_{DateTime.Now:yyyy/MM/dd/HH-mm-ss}.xlsx");

                        using (MemoryStream ms = new MemoryStream())
                        {
                            wb.SaveAs(ms);
                            ms.WriteTo(Response.OutputStream);
                        }

                        Response.Flush();
                        Response.End();
                    }
                }
                else
                {
                    ScriptManager.RegisterStartupScript(this, GetType(), "NoDataAlert",
                        "alert('No data to export!');", true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Export error: {ex}");
                ScriptManager.RegisterStartupScript(this, GetType(), "ExportError",
                    $"alert('Export failed: {ex.Message}');", true);
            }
        }

        private DataTable GetAllItems()
        {
            List<string> storeNos = GetLoggedInUserStoreNames();
            DataTable dt = new DataTable();
            string query;

            using (SqlConnection conn = new SqlConnection(strcon))
            {
                query = @"
                        SELECT tranNo,topicName,description,room,trainerName, position,date,time
                        FROM scheduleT order by id";


                //If "HO" exists in store list, return all data
                if (storeNos.Any(s => s.Equals("HO", StringComparison.OrdinalIgnoreCase)))
                {
                    query = @"
                        SELECT TakenDate, itemNo, Description, LocationCode, BalanceQty, UnitofMeasure, itemFamily
                        FROM negLocation
                        WHERE TakenTime = (SELECT MAX(TakenTime) FROM negLocation)";
                }
                else
                {
                    // Dynamically build parameterized IN clause
                    var whereIn = string.Join(",", storeNos.Select((s, i) => $"@store{i}"));
                    query = $@"
                    SELECT TakenDate, itemNo, Description, LocationCode, BalanceQty, UnitofMeasure, itemFamily
                    FROM negLocation
                    WHERE TakenTime = (SELECT MAX(TakenTime) FROM negLocation)
                    AND LocationCode IN ({whereIn})";
                    }

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    if (!storeNos.Any(s => s.Equals("HO", StringComparison.OrdinalIgnoreCase)))
                    {
                        for (int i = 0; i < storeNos.Count; i++)
                        {
                            cmd.Parameters.AddWithValue($"@store{i}", storeNos[i]);
                        }
                    }

                    try
                    {
                        conn.Open();
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            adapter.Fill(dt);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new ApplicationException("Error retrieving data from the database.", ex);
                    }
                }
            }

            return dt;
        }

        private Dictionary<string, string> GetAllowedFormsByUser(string username)
        {
            Dictionary<string, string> forms = new Dictionary<string, string>();

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

        protected string FormatDisplayDate(object dateValue)
        {
            if (dateValue == null || dateValue == DBNull.Value)
                return string.Empty;

            if (DateTime.TryParse(dateValue.ToString(), out DateTime date))
            {
                return date.ToString("dd-MMM-yyyy");
            }
            return dateValue.ToString();
        }

        protected string FormatDisplayTime(object timeValue)
        {
            if (timeValue == null || timeValue == DBNull.Value)
                return string.Empty;

            if (timeValue is TimeSpan timeSpan)
            {
                DateTime timeDate = DateTime.Today.Add(timeSpan);
                return timeDate.ToString("hh:mm tt");
            }

            if (TimeSpan.TryParse(timeValue.ToString(), out TimeSpan time))
            {
                DateTime timeDate = DateTime.Today.Add(time);
                return timeDate.ToString("hh:mm tt");
            }

            return timeValue.ToString();
        }
    }
}