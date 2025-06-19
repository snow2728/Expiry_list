using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Newtonsoft.Json;
using OfficeOpenXml;
using OfficeOpenXml.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.DynamicData;
using System.Web.UI;
using System.Web.UI.WebControls;
using static System.Windows.Forms.LinkLabel;

namespace Expiry_list
{
    public partial class balanceQty : System.Web.UI.Page
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
                BindGrid();  
            }

        }

        protected void GridView1_Sorting(object sender, GridViewSortEventArgs e)
        {
            string sortExpression = e.SortExpression;
            string sortDirection = GetSortDirection(sortExpression);

            ViewState["SortExpression"] = sortExpression;
            ViewState["SortDirection"] = sortDirection;

        }

        protected void GridView1_RowCreated(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.Header)
            {
                if (ViewState["SortExpression"] != null && ViewState["SortDirection"] != null)
                {
                    string sortExpression = ViewState["SortExpression"].ToString();
                    string sortDirection = ViewState["SortDirection"].ToString();

                    for (int i = 0; i < GridView2.Columns.Count; i++)
                    {
                        if (GridView2.Columns[i].SortExpression == sortExpression)
                        {
                            LinkButton lb = (LinkButton)e.Row.Cells[i].Controls[0];
                            string iconHtml = sortDirection == "ASC" ? " ▲" : " ▼";
                            lb.Text += iconHtml;

                        }
                    }
                }
            }
        }

        private string GetLoggedInUserStoreName()
        {
            int storeId = Convert.ToInt32(Session["storeNo"] ?? 0);

            if (storeId == 0 || Session["storeNo"] == null)
            {
                Response.Redirect("login.aspx");
                return null;
            }

            string storeName = null;
            string query = "SELECT StoreNo FROM Stores WHERE id = @StoreId";

            using (SqlConnection conn = new SqlConnection(strcon))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@StoreId", storeId);
                conn.Open();

                storeName = cmd.ExecuteScalar()?.ToString();
            }

            if (string.IsNullOrEmpty(storeName))
            {
                Response.Write("Error: StoreName not found.<br>");
            }

            return storeName;
        }

        private void RespondWithData()
        {
            using (SqlConnection conn = new SqlConnection(strcon))
            {
                int draw = Convert.ToInt32(Request["draw"]);
                int start = Convert.ToInt32(Request["start"]);
                int length = Convert.ToInt32(Request["length"]);
                int orderColumnIndex = Convert.ToInt32(Request["order[0][column]"]);
                string orderDir = Request["order[0][dir]"] ?? "asc";

                if (length == 0) length = 100;

                string[] columns = { "srno", "ItemNo", "Description", "LocationCode", "BalanceQty", "TakenDate", "TakenTime", "UnitofMeasure", "QtyperUnitOfMeasure" };

                string[] sortableColumns = {
                    "srno", "ItemNo",
                    "Description", "LocationCode", "BalanceQty", "TakenDate", "TakenTime",
                    "UnitofMeasure", "QtyperUnitofMeasure"
                };

                string[] searchableColumns = { "srno","ItemNo", "Description", "LocationCode", "BalanceQty", "TakenDate", "TakenTime", "UnitofMeasure", "QtyperUnitOfMeasure" };

                string searchValue = Request["search"] ?? "";
                string userRole = Session["role"] as string;
                string storeNo = GetLoggedInUserStoreName();

                StringBuilder whereClause = new StringBuilder("TakenTime = (SELECT MAX(TakenTime) FROM negLocation)");

                if (userRole == "user")
                {
                    whereClause.Append(" AND LocationCode = @StoreNo");
                }

                // Add search filter
                if (!string.IsNullOrEmpty(searchValue))
                {
                    whereClause.Append(" AND (");
                    foreach (string col in searchableColumns)
                    {
                        whereClause.Append($"{col} LIKE '%' + @SearchValue + '%' OR ");
                    }
                    whereClause.Remove(whereClause.Length - 4, 4);
                    whereClause.Append(")");
                }

                string orderByClause = "";
                int serverColumnIndex = orderColumnIndex;

                if (serverColumnIndex < 0 || serverColumnIndex >= sortableColumns.Length)
                {
                    serverColumnIndex = 0;
                    orderDir = "asc";
                }

                string orderBy = $"[{sortableColumns[serverColumnIndex]}]";
                orderDir = orderDir.ToLower() == "desc" ? "DESC" : "ASC";
                orderByClause = $" ORDER BY {orderBy} {orderDir}";

                string query = $@"
                    SELECT * 
                    FROM negLocation 
                    WHERE {whereClause}
                    {orderByClause}
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Offset", start);
                cmd.Parameters.AddWithValue("@SearchValue", searchValue);
                cmd.Parameters.AddWithValue("@PageSize", length);

                if (userRole == "user")
                {
                    cmd.Parameters.AddWithValue("@StoreNo", storeNo);
                }

                conn.Open();
                DataTable dt = new DataTable();
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
                conn.Close();

                string countQuery = $@"
                   SELECT COUNT(*) 
                   FROM negLocation 
                   WHERE {whereClause}";

                SqlCommand countCmd = new SqlCommand(countQuery, conn);
                countCmd.Parameters.AddWithValue("@SearchValue", searchValue);

                if (userRole == "user")
                {
                    countCmd.Parameters.AddWithValue("@StoreNo", storeNo);
                }

                conn.Open();
                int totalRecords = (int)countCmd.ExecuteScalar();
                conn.Close();

                var data = dt.AsEnumerable().Select(row => new
                {
                    srno = row["srno"],
                    ItemNo = row["itemNo"],
                    Description = row["Description"],
                    LocationCode = row["LocationCode"],
                    BalanceQty = row["BalanceQty"],
                    TakenDate = Convert.ToDateTime(row["TakenDate"]).ToString("yyyy-MM-dd"),
                    TakenTime = Convert.ToDateTime(row["TakenTime"]).ToString("yyyy-MM-dd HH:mm:ss"),
                    UnitofMeasure = row["UnitofMeasure"],
                    QtyperUnitofMeasure = row["QtyperUnitofMeasure"] ,
                    ItemFamily = row["ItemFamily"]
                }).ToList();

                var response = new
                {
                    draw = draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = totalRecords,
                    data = data,
                    orderColumn = orderColumnIndex,
                    orderDir = orderDir
                };

                // Return JSON response
                string jsonResponse = JsonConvert.SerializeObject(response);
                Response.ContentType = "application/json";
                Response.Write(jsonResponse);
                Response.End();
            }
        }


        private string GetSortDirection(string sortExpression)
        {
            if (ViewState["SortExpression"] != null && ViewState["SortExpression"].ToString() == sortExpression)
            {
                if (ViewState["SortDirection"] != null && ViewState["SortDirection"].ToString() == "ASC")
                {
                    return "DESC";
                }
                else
                {
                    return "ASC";
                }
            }
            else
            {
                return "ASC";
            }
        }

        protected void GridView1_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            GridView2.PageIndex = e.NewPageIndex;
            BindGrid();
        }

        private void BindGrid(int pageNumber = 1, int pageSize = 100)
        {
            using (SqlConnection conn = new SqlConnection(strcon))
            {
                string orderBy = string.IsNullOrEmpty(hfSelectedIDs.Value)
                    ? "ORDER BY srno"
                    : $"ORDER BY CASE WHEN srno = '{hfSelectedIDs.Value}' THEN 0 ELSE 1 END, srno";

                string userRole = Session["role"] as string;
                string storeNo = GetLoggedInUserStoreName();

                string query;

                if (userRole == "user")
                {
                    query = $@"SELECT * FROM negLocation 
                        WHERE TakenTime = (SELECT MAX(TakenTime) FROM negLocation) 
                          AND LocationCode = @storeNo
                        {orderBy}
                        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
                }
                else
                {
                    query = $@"SELECT * FROM negLocation 
                        WHERE TakenTime = (SELECT MAX(TakenTime) FROM negLocation) 
                        {orderBy}
                        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
                }

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Offset", (pageNumber - 1) * pageSize);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);

                if (userRole == "user")
                {
                    cmd.Parameters.AddWithValue("@storeNo", storeNo);
                }

                conn.Open();
                DataTable dt = new DataTable();
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
                conn.Close();

                GridView2.DataSource = dt;
                GridView2.PageIndex = 0;
                GridView2.DataBind();
            }
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
                        var ws = wb.Worksheets.Add(dt, "NegativeItems");

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
            string storeNo = GetLoggedInUserStoreName();
            string query;
            DataTable dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(strcon))
            {
                if (storeNo.Equals("HO", StringComparison.OrdinalIgnoreCase))
                {
                    query = @"
                SELECT TakenDate, itemNo, Description, LocationCode, BalanceQty, UnitofMeasure, itemFamily
                FROM negLocation
                WHERE TakenTime = (SELECT MAX(TakenTime) FROM negLocation)";
                }
                else
                {
                    query = @"
                SELECT TakenDate, itemNo, Description, LocationCode, BalanceQty, UnitofMeasure, itemFamily
                FROM negLocation
                WHERE TakenTime = (SELECT MAX(TakenTime) FROM negLocation)
                AND LocationCode = @storeNo";
                }

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    if (!storeNo.Equals("HO", StringComparison.OrdinalIgnoreCase))
                    {
                        cmd.Parameters.AddWithValue("@storeNo", storeNo);
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

    }
}

