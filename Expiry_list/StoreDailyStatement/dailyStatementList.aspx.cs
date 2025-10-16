using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using static System.Windows.Forms.LinkLabel;
using Table = System.Web.UI.WebControls.Table;

namespace Expiry_list
{
    public partial class dailyStatementList : System.Web.UI.Page
    {
        string strcon = ConfigurationManager.ConnectionStrings["con"].ConnectionString;
        string storeNo = null;
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                storeNo = string.Join(",", Common.GetLoggedInUserStoreNames());
                if(storeNo != "HO")
                {
                    storeFilterGroup.Visible = false;                    
                }
                else
                {
                    storeFilterGroup.Visible = true;
                    BindStores();
                }
                    BindGrid(); 
            }            
        }
        protected void GridView2_Sorting(object sender, GridViewSortEventArgs e)
        {
            string sortExpression = e.SortExpression;
            string sortDirection = GetSortDirection(sortExpression);

            ViewState["SortExpression"] = sortExpression;
            ViewState["SortDirection"] = sortDirection;
        }
        protected void GridView2_RowCreated(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.Header)
            {
                // --- First header row (group headers) ---
                GridViewRow headerRow = new GridViewRow(0, 0,
                    DataControlRowType.Header, DataControlRowState.Insert);

                // First 5 columns (rowspan = 2)
                TableHeaderCell h1 = new TableHeaderCell { Text = "Date", RowSpan = 2 };
                headerRow.Cells.Add(h1);

                TableHeaderCell h2 = new TableHeaderCell { Text = "Store No", RowSpan = 2 };
                headerRow.Cells.Add(h2);

                TableHeaderCell h3 = new TableHeaderCell { Text = "Store Name", RowSpan = 2 };
                headerRow.Cells.Add(h3);

                TableHeaderCell h4 = new TableHeaderCell { Text = "Total Sales", RowSpan = 2 };
                headerRow.Cells.Add(h4);

                TableHeaderCell h5 = new TableHeaderCell { Text = "Petty Cash", RowSpan = 2 };
                headerRow.Cells.Add(h5);

                // Group headers
                TableHeaderCell advPay = new TableHeaderCell { Text = "ယခင်နေ့လက်ကျန်အပ်ငွေ", ColumnSpan = 4 };
                headerRow.Cells.Add(advPay);

                TableHeaderCell dailySales = new TableHeaderCell { Text = "Daily Salesအပ်ငွေ", ColumnSpan = 4 };
                headerRow.Cells.Add(dailySales);

                TableHeaderCell mmqr = new TableHeaderCell { Text = "Pay Total MMQR", ColumnSpan = 3 };
                headerRow.Cells.Add(mmqr);

                TableHeaderCell kpay = new TableHeaderCell { Text = "Pay Total Kpay", RowSpan = 2 };
                headerRow.Cells.Add(kpay);

                TableHeaderCell card = new TableHeaderCell { Text = "Card Payment", ColumnSpan = 3 };
                headerRow.Cells.Add(card);

                TableHeaderCell extra = new TableHeaderCell { Text = "ပိုငွေ/လိုငွေ", RowSpan = 2 };
                headerRow.Cells.Add(extra);

                TableHeaderCell deliPay = new TableHeaderCell { Text = "Flash Deli Pay", RowSpan = 2 };
                headerRow.Cells.Add(deliPay);

                TableHeaderCell deliCOD = new TableHeaderCell { Text = "Flash Deli COD", RowSpan = 2 };
                headerRow.Cells.Add(deliCOD);

                TableHeaderCell net = new TableHeaderCell { Text = "အပ်ငွေ", RowSpan = 2 };
                headerRow.Cells.Add(net);

                // Insert custom header row before the default header
                GridView2.Controls[0].Controls.AddAt(0, headerRow);


                // --- Second header row (sub-columns) ---
                GridViewRow subHeaderRow = new GridViewRow(1, 0,
                    DataControlRowType.Header, DataControlRowState.Insert);

                // Advance Payments (4)
                subHeaderRow.Cells.Add(new TableHeaderCell { Text = "U Shwe Myint" });
                subHeaderRow.Cells.Add(new TableHeaderCell { Text = "ABank" });
                subHeaderRow.Cells.Add(new TableHeaderCell { Text = "KBZ" });
                subHeaderRow.Cells.Add(new TableHeaderCell { Text = "UAB" });

                // Daily Sales (4)
                subHeaderRow.Cells.Add(new TableHeaderCell { Text = "U Shwe Myint" });
                subHeaderRow.Cells.Add(new TableHeaderCell { Text = "ABank" });
                subHeaderRow.Cells.Add(new TableHeaderCell { Text = "KBZ" });
                subHeaderRow.Cells.Add(new TableHeaderCell { Text = "UAB" });

                // MMQR (3)
                subHeaderRow.Cells.Add(new TableHeaderCell { Text = "A Plus" });
                subHeaderRow.Cells.Add(new TableHeaderCell { Text = "MMQR-86" });
                subHeaderRow.Cells.Add(new TableHeaderCell { Text = "MMQR-62" });

                // Card (3)
                subHeaderRow.Cells.Add(new TableHeaderCell { Text = "ABank" });
                subHeaderRow.Cells.Add(new TableHeaderCell { Text = "AYA" });
                subHeaderRow.Cells.Add(new TableHeaderCell { Text = "UAB" });

                // Add the second header row
                GridView2.Controls[0].Controls.AddAt(1, subHeaderRow);
            }
        }
        protected void GridView2_PreRender(object sender, EventArgs e)
        {
            Table table = (Table)GridView2.Controls[0];

            if (table.Rows.Count > 1)
            {
                // --- Group header row ---
                TableRow headerRow = table.Rows[0];
                headerRow.TableSection = TableRowSection.TableHeader;
                for (int i = 0; i < headerRow.Cells.Count; i++)
                {
                    TableCell cell = headerRow.Cells[i];
                    cell.BackColor = System.Drawing.ColorTranslator.FromHtml("#38678F");
                    cell.ForeColor = System.Drawing.Color.White;
                    cell.HorizontalAlign = HorizontalAlign.Center;
                    cell.VerticalAlign = VerticalAlign.Middle;
                    cell.Style.Add("text-align", "center");

                    // Set widths
                    if (i == 2) // column index 2 (storeName)
                    {
                        cell.Width = Unit.Pixel(250);
                        cell.Style.Add("min-width", "250px");
                    }
                    else if (i == 5 || i == 6 || i == 7 || i == 9) 
                    {
                        cell.Width = Unit.Pixel(700);
                        cell.Style.Add("min-width", "700px");
                    }
                    else if (i == 3 || i == 8 || i > 9)
                    {
                        cell.Width = Unit.Pixel(170);
                        cell.Style.Add("min-width", "170px");
                    }
                    else
                    {
                        cell.Width = Unit.Pixel(100);
                        cell.Style.Add("min-width", "100px");
                    }
                }

                // --- Sub-header row ---
                TableRow subHeaderRow = table.Rows[1];
                subHeaderRow.TableSection = TableRowSection.TableHeader;
                for (int i = 0; i < subHeaderRow.Cells.Count; i++)
                {
                    TableCell cell = subHeaderRow.Cells[i];
                    cell.BackColor = System.Drawing.ColorTranslator.FromHtml("#38678F");
                    cell.ForeColor = System.Drawing.Color.White;
                    cell.HorizontalAlign = HorizontalAlign.Center;
                }

                // --- Data rows ---
                for (int i = 2; i < table.Rows.Count; i++)
                {
                    TableRow dataRow = table.Rows[i];
                    for (int j = 0; j < dataRow.Cells.Count; j++)
                    {
                        TableCell cell = dataRow.Cells[j];
                        if (j > 2)
                        {
                            cell.HorizontalAlign = HorizontalAlign.Right;
                        }
                        if (j == 2) // storeName
                        {
                            cell.Width = Unit.Pixel(250);
                            cell.Style.Add("min-width", "250px");
                        }
                        else
                        {
                            cell.Width = Unit.Pixel(100);
                            cell.Style.Add("min-width", "100px");
                        }
                    }
                }
            }
        }
        protected void GridView1_RowCreated(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.Header)
            {
                // first header row (group headers)
                GridViewRow headerRow = new GridViewRow(0, 0,
                    DataControlRowType.Header, DataControlRowState.Insert);

                // First 5 columns
                for (int i = 0; i < 5; i++)
                {
                    TableHeaderCell cell = new TableHeaderCell();
                    cell.RowSpan = 2;
                    cell.Text = GridView2.Columns[i].HeaderText;
                    cell.BackColor = System.Drawing.ColorTranslator.FromHtml("#2c3e50");
                    cell.ForeColor = System.Drawing.Color.White;
                    cell.HorizontalAlign = HorizontalAlign.Center;
                    headerRow.Cells.Add(cell);
                }

                // Group headers
                headerRow.Cells.Add(new TableHeaderCell { Text = "Advance Payments", ColumnSpan = 4 });
                headerRow.Cells.Add(new TableHeaderCell { Text = "Daily Sales", ColumnSpan = 4 });
                headerRow.Cells.Add(new TableHeaderCell { Text = "MMQR", ColumnSpan = 3 });
                headerRow.Cells.Add(new TableHeaderCell { Text = "Card", ColumnSpan = 3 });

                for (int i = GridView2.Columns.Count - 4; i < GridView2.Columns.Count; i++)
                {
                    TableHeaderCell cell = new TableHeaderCell();
                    cell.RowSpan = 2;
                    cell.Text = GridView2.Columns[i].HeaderText;
                    cell.BackColor = System.Drawing.ColorTranslator.FromHtml("#34495e");
                    cell.ForeColor = System.Drawing.Color.White;
                    cell.HorizontalAlign = HorizontalAlign.Center;
                    headerRow.Cells.Add(cell);
                }
                GridViewRow subHeaderRow = new GridViewRow(1, 0,
                    DataControlRowType.Header, DataControlRowState.Insert);

                subHeaderRow.Cells.Add(new TableHeaderCell { Text = "Shwe" });
                subHeaderRow.Cells.Add(new TableHeaderCell { Text = "ABank" });
                subHeaderRow.Cells.Add(new TableHeaderCell { Text = "KBZ" });
                subHeaderRow.Cells.Add(new TableHeaderCell { Text = "UAB" });

                subHeaderRow.Cells.Add(new TableHeaderCell { Text = "Shwe" });
                subHeaderRow.Cells.Add(new TableHeaderCell { Text = "ABank" });
                subHeaderRow.Cells.Add(new TableHeaderCell { Text = "KBZ" });
                subHeaderRow.Cells.Add(new TableHeaderCell { Text = "UAB" });

                subHeaderRow.Cells.Add(new TableHeaderCell { Text = "A+" });
                subHeaderRow.Cells.Add(new TableHeaderCell { Text = "MMQR-86" });
                subHeaderRow.Cells.Add(new TableHeaderCell { Text = "MMQR-62" });

                subHeaderRow.Cells.Add(new TableHeaderCell { Text = "ABank" });
                subHeaderRow.Cells.Add(new TableHeaderCell { Text = "AYA" });
                subHeaderRow.Cells.Add(new TableHeaderCell { Text = "UAB" });

                Table table = (Table)GridView2.Controls[0];
                table.Controls.AddAt(0, subHeaderRow);
                table.Controls.AddAt(0, headerRow);
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
        
        //private List<string> GetLoggedInUserStoreNames()
        //{
        //    List<string> storeNos = Session["storeListRaw"] as List<string>;
        //    List<string> storeNames = new List<string>();

        //    if (storeNos == null || storeNos.Count == 0)
        //        return storeNames;

        //    string query = $"SELECT storeNo FROM Stores WHERE storeNo IN ({string.Join(",", storeNos.Select((s, i) => $"@store{i}"))})";

        //    using (SqlConnection conn = new SqlConnection(strcon))
        //    using (SqlCommand cmd = new SqlCommand(query, conn))
        //    {
        //        for (int i = 0; i < storeNos.Count; i++)
        //        {
        //            cmd.Parameters.AddWithValue($"@store{i}", storeNos[i]);
        //        }

        //        conn.Open();
        //        using (SqlDataReader reader = cmd.ExecuteReader())
        //        {
        //            while (reader.Read())
        //            {
        //                storeNames.Add(reader["storeNo"].ToString());
        //            }
        //        }
        //    }

        //    return storeNames;
        //}
        private void BindStores()
        {
            try
            {
                lstStoreFilter.Items.Clear();
                lstStoreFilter.Items.Add(new ListItem("All Stores", "all"));

                var uniqueStores = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                using (SqlConnection con = new SqlConnection(strcon))
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand(@"SELECT DISTINCT LTRIM(RTRIM(storeNo)) AS storeNo 
                                                   FROM stores 
                                                   WHERE storeNo IS NOT NULL 
                                                   AND storeNo <> '' AND storeNo <> 'HO'", con))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string storeNo = reader["storeNo"].ToString().Trim();
                            if (!string.IsNullOrEmpty(storeNo) && uniqueStores.Add(storeNo))
                            {
                                lstStoreFilter.Items.Add(new ListItem(storeNo, storeNo));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ScriptManager.RegisterStartupScript(this, GetType(), "alert",
                    $"alert('Error loading stores: {ex.Message.Replace("'", "''")}');", true);
            }
        }
        private void BindGrid(int pageNumber = 1, int pageSize = 100)
        {
            using (SqlConnection conn = new SqlConnection(strcon))
            {
                string orderBy = " ORDER BY dsh.createdDate";

                string username = User.Identity.Name;
                var permissions = Common.GetAllowedFormsByUser(username);
                Session["formPermissions"] = permissions;
                Session["activeModule"] = "DailyStatement";

                if (!permissions.TryGetValue("DailyStatement", out string perm))
                {
                    ShowAlert("Unauthorized", "You do not have permission to access Daily Statement", "error");
                    return;
                }
                List<string> storeList = Session["storeListRaw"] as List<string>;
                storeNo = string.Join(",", Common.GetLoggedInUserStoreNames());

                StringBuilder whereClause = new StringBuilder();                

                bool showAllStores = storeList != null &&
                                     storeList.Contains("HO", StringComparer.OrdinalIgnoreCase);

                List<string> storeConditions = new List<string>();

                if (!showAllStores && storeList != null && storeList.Count > 0)
                {
                    for (int i = 0; i < storeList.Count; i++)
                    {
                        storeConditions.Add($"dsh.storeNo = @store{i}");
                    }

                    whereClause.Append(" where (" + string.Join(" OR ", storeConditions) + ")");
                }

                string query;

                query = $@"select dsh.sdsId,dsh.createdDate,dsh.storeNo,st.storeName,CONVERT(varchar, CAST(dsh.totalSales AS money), 1) as totalSalesAmt,CONVERT(varchar, CAST(dsd.pettyCash AS money), 1) as pettyCash,
                            CONVERT(varchar, CAST(dsd.advPayShweAmt AS money), 1) as advPayShweAmt,CONVERT(varchar, CAST(dsd.advPayABankAmt AS money), 1) as advPayABankAmt,CONVERT(varchar, CAST(dsd.advPayKbzAmt AS money), 1) as advPayKbzAmt,
                            CONVERT(varchar, CAST(dsd.advPayUabAmt AS money), 1) as advPayUabAmt,CONVERT(varchar, CAST(dsd.dailySalesShweAmt AS money), 1) as dailySalesShweAmt,CONVERT(varchar, CAST(dsd.dailySalesABankAmt AS money), 1) as dailySalesABankAmt,
                            CONVERT(varchar, CAST(dsd.dailySalesKbzAmt AS money), 1) as dailySalesKbzAmt,CONVERT(varchar, CAST(dsd.dailySalesUabAmt AS money), 1) as dailySalesUabAmt,CONVERT(varchar, CAST(dsd.mmqr1Amt AS money), 1) as mmqr1Amt,
                            CONVERT(varchar, CAST(dsd.mmqr2Amt AS money), 1) as mmqr2Amt,CONVERT(varchar, CAST(dsd.mmqr3Amt AS money), 1) as mmqr3Amt,CONVERT(varchar, CAST(dsd.mmqr4Amt AS money), 1) as mmqr4Amt,CONVERT(varchar, CAST(dsd.payKpayTolAmt AS money), 1) as payTotalAmt,
                            CONVERT(varchar, CAST(dsd.cardABankAmt AS money), 1) as cardABankAmt,CONVERT(varchar, CAST(dsd.cardAyaAmt AS money), 1) as cardAyaAmt,CONVERT(varchar, CAST(dsd.cardUabAmt AS money), 1) as cardUabAmt,
                            CONVERT(varchar, CAST(dsd.extraAmt AS money), 1) as extraAmt,CONVERT(varchar, CAST(dsd.flashDeliPayAmt AS money), 1) as deliPayAmt,CONVERT(varchar, CAST(dsd.flashDeliCodAmt AS money), 1) as deliCodAmt,
                            CONVERT(varchar, CAST(dsh.totalSubmit AS money), 1) as netAmt
                            from dailyStmH dsh join stores st on dsh.storeId = st.id
                            join dailyStmD dsd on dsh.sdsId = dsd.sdsId 
                            {whereClause}
                          {orderBy} 
                         OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";                

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Offset", (pageNumber - 1) * pageSize);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);
                
                if (!showAllStores && storeList != null)
                {
                    for (int i = 0; i < storeList.Count; i++)
                    {
                        cmd.Parameters.AddWithValue($"@store{i}", storeList[i]);
                    }
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
        //private Dictionary<string, string> GetAllowedFormsByUser(string username)
        //{
        //    Dictionary<string, string> forms = new Dictionary<string, string>();

        //    using (SqlConnection conn = new SqlConnection(strcon))
        //    {
        //        string query = @"
        //    SELECT f.name AS FormName,
        //           CASE up.permission_level
        //                WHEN 1 THEN 'view'
        //                WHEN 2 THEN 'edit'
        //                WHEN 3 THEN 'admin'
        //                WHEN 4 THEN 'super'
        //                WHEN 5 THEN 'super1'
        //                ELSE 'none'
        //           END AS Permission
        //            FROM UserPermissions up
        //            INNER JOIN Forms f ON up.form_id = f.id
        //            INNER JOIN Users u ON up.user_id = u.id
        //            WHERE u.username = @Username";

        //        SqlCommand cmd = new SqlCommand(query, conn);
        //        cmd.Parameters.AddWithValue("@Username", username);
        //        conn.Open();

        //        using (SqlDataReader reader = cmd.ExecuteReader())
        //        {
        //            while (reader.Read())
        //            {
        //                string form = reader["FormName"].ToString();
        //                string permission = reader["Permission"].ToString();
        //                forms[form] = permission;
        //            }
        //        }
        //    }
        //    return forms;
        //}

        private void ShowAlert(string title, string message, string type)
        {
            ScriptManager.RegisterStartupScript(this, GetType(), "alert",
                $"swal('{title}', '{HttpUtility.JavaScriptStringEncode(message)}', '{type}');", true);
        }
    }
}

