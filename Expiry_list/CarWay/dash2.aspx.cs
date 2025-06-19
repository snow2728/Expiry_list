using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using Newtonsoft.Json;

namespace Expiry_list.CarWay
{
    public partial class dash2 : Page
    {
        string strcon = ConfigurationManager.ConnectionStrings["con"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                //ddlWayType.SelectedValue = "0";
                //txtDepDate.Text = "";
                //txtDepTime.Text = "";
                //txtFLocation.Text = "";

                BindStoreList();
                BindToteBox();
                BindCarList();
                txtWayNo.Text = GenerateWaybillNumber();
                Session["ActiveToteBoxes"] = new List<string>();
            }
        }

        // Add this class
        public class ToteBoxItem
        {
            public string Value { get; set; }
            public string Text { get; set; }
        }

        public List<ToteBoxItem> GetAvailableToteBoxes()
        {
            var toteBoxes = new List<ToteBoxItem>();
            string query = "SELECT tbid, tboxNo FROM TblToteBox WHERE IsAvailable = 1";

            using (var con = new SqlConnection(strcon))
            {
                using (var cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        toteBoxes.Add(new ToteBoxItem
                        {
                            Value = reader["tbid"].ToString(),
                            Text = reader["tboxNo"].ToString()
                        });
                    }
                }
            }
            return toteBoxes;
        }

        [WebMethod]
        public static object UpdateToteBoxStatus(List<string> toteBoxIds)
        {
            try
            {
                string strcon = ConfigurationManager.ConnectionStrings["con"].ConnectionString;

                using (var con = new SqlConnection(strcon))
                {
                    con.Open();
                    using (var tran = con.BeginTransaction())
                    {
                        try
                        {
                            const string sql = @"
                                UPDATE TblToteBox 
                                SET Status = 'Busy', IsAvailable = 0 
                                WHERE tbid = @tbid";

                            foreach (var tbid in toteBoxIds)
                            {
                                using (var cmd = new SqlCommand(sql, con, tran))
                                {
                                    cmd.Parameters.AddWithValue("@tbid", tbid);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            tran.Commit();

                            // Add to session
                            var session = HttpContext.Current.Session;
                            var activeBoxes = session["ActiveToteBoxes"] as List<string> ?? new List<string>();
                            activeBoxes.AddRange(toteBoxIds);
                            session["ActiveToteBoxes"] = activeBoxes.Distinct().ToList();

                            return new
                            {
                                success = true,
                                message = $"Marked {toteBoxIds.Count} tote-box(es) Busy."
                            };
                        }
                        catch (Exception ex)
                        {
                            tran.Rollback();
                            return new
                            {
                                success = false,
                                message = "Error: " + ex.Message
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    message = "System error: " + ex.Message
                };
            }
        }

        [WebMethod]
        public static object RevertToteBoxStatus(List<string> toteBoxIds)
        {
            try
            {
                string strcon = ConfigurationManager.ConnectionStrings["con"].ConnectionString;

                using (var con = new SqlConnection(strcon))
                {
                    con.Open();
                    using (var tran = con.BeginTransaction())
                    {
                        try
                        {
                            const string sql = @"
                                UPDATE TblToteBox 
                                SET Status = 'Available', IsAvailable = 1 
                                WHERE tbid = @tbid";

                            foreach (var tbid in toteBoxIds)
                            {
                                using (var cmd = new SqlCommand(sql, con, tran))
                                {
                                    cmd.Parameters.AddWithValue("@tbid", tbid);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            tran.Commit();

                            // Remove from session
                            var session = HttpContext.Current.Session;
                            var activeBoxes = session["ActiveToteBoxes"] as List<string> ?? new List<string>();
                            activeBoxes = activeBoxes.Except(toteBoxIds).ToList();
                            session["ActiveToteBoxes"] = activeBoxes;

                            return new
                            {
                                success = true,
                                message = $"Reverted {toteBoxIds.Count} tote-box(es) to Available."
                            };
                        }
                        catch (Exception ex)
                        {
                            tran.Rollback();
                            return new
                            {
                                success = false,
                                message = "Error: " + ex.Message
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    message = "System error: " + ex.Message
                };
            }
        }

        [WebMethod]
        public static void RevertAllActiveToteBoxes()
        {
            try
            {
                var session = HttpContext.Current.Session;
                var activeBoxes = session["ActiveToteBoxes"] as List<string> ?? new List<string>();

                if (activeBoxes.Count > 0)
                {
                    string strcon = ConfigurationManager.ConnectionStrings["con"].ConnectionString;

                    using (var con = new SqlConnection(strcon))
                    {
                        con.Open();
                        using (var tran = con.BeginTransaction())
                        {
                            try
                            {
                                const string sql = @"
                                    UPDATE TblToteBox 
                                    SET Status = 'Available', IsAvailable = 1 
                                    WHERE tbid = @tbid";

                                foreach (var tbid in activeBoxes)
                                {
                                    using (var cmd = new SqlCommand(sql, con, tran))
                                    {
                                        cmd.Parameters.AddWithValue("@tbid", tbid);
                                        cmd.ExecuteNonQuery();
                                    }
                                }

                                tran.Commit();
                                session["ActiveToteBoxes"] = new List<string>();
                            }
                            catch
                            {
                                tran.Rollback();
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            if (!ValidateDetailLinesExist() && ddlPackingType.SelectedValue != "1")
            {
                lblStatus.InnerText = "Cannot save: Please add at least one valid detail line";
                lblStatus.Attributes["class"] = "alert alert-danger";
                return;
            }

            if (!ValidateToteBoxCommitment() && ddlPackingType.SelectedValue == "1")
            {
                lblStatus.InnerText = "Cannot save: Uncommitted Tote Box lines exist";
                lblStatus.Attributes["class"] = "alert alert-danger";
                return;
            }

            string waybillNumber = txtWayNo.Text; 

            using (SqlConnection con = new SqlConnection(strcon))
            {
                con.Open();
                SqlTransaction tran = con.BeginTransaction();

                try
                {
                    int waybillId = InsertWayHeader(con, tran, waybillNumber); 
                    InsertWayLines(con, tran, waybillId);
                    tran.Commit();

                    lblStatus.InnerText = "Waybill saved successfully!";
                    lblStatus.Attributes["class"] = "alert alert-success";

                    txtWayNo.Text = GenerateWaybillNumber();
                    cleareInput();
                    BindCarList();
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    lblStatus.InnerText = "Error saving waybill: " + ex.Message;
                    lblStatus.Attributes["class"] = "alert alert-danger";
                }
            }
        }

        private bool ValidateDetailLinesExist()
        {
            string gridDataJson = hdnGridData.Value;
            if (string.IsNullOrWhiteSpace(gridDataJson))
                return false;

            try
            {
                var gridData = JsonConvert.DeserializeObject<List<GridRowData>>(gridDataJson);
                return gridData.Any(row =>
                    row.PackingType != "0" ||
                    (row.Stores != null && row.Stores.Count > 0) ||
                    row.Quantity > 0);
            }
            catch
            {
                return false;
            }
        }

        private bool ValidateToteBoxCommitment()
        {
            string gridDataJson = hdnGridData.Value;
            if (string.IsNullOrWhiteSpace(gridDataJson))
                return true;

            try
            {
                var gridData = JsonConvert.DeserializeObject<List<GridRowData>>(gridDataJson);
                return !gridData.Any(row => row.PackingType == "1" && !row.IsCommitted);
            }
            catch
            {
                return false;
            }
        }

        [WebMethod]
        public static List<ToteBoxItem> GetAvailableToteBoxesForClient()
        {
            return new dash2().GetAvailableToteBoxes();
        }

        //protected void btnSet_Click(object sender, EventArgs e)
        //{
        //    ListBox lstTote = ddlToteBox1; 

        //    if (lstTote == null)
        //    {
        //        lstTote = FindControlRecursive(Page, "ddlToteBox1") as ListBox;
        //    }

        //    if (lstTote == null)
        //    {
        //        lblStatus.InnerText = "Tote-box control not found.";
        //        lblStatus.Attributes["class"] = "alert alert-danger";
        //        return;
        //    }

        //    var selectedToteIds = lstTote.Items.Cast<ListItem>()
        //        .Where(i => i.Selected)
        //        .Select(i => i.Value)
        //        .ToList();

        //    if (selectedToteIds.Count == 0)
        //    {
        //        lblStatus.InnerText = "No tote-box selected.";
        //        lblStatus.Attributes["class"] = "alert alert-warning";
        //        return;
        //    }

        //    using (var con = new SqlConnection(strcon))
        //    {
        //        con.Open();
        //        using (var tran = con.BeginTransaction())
        //        {
        //            try
        //            {
        //                const string sql = @"
        //        UPDATE TblToteBox 
        //        SET Status = 'Busy', IsAvailable = 0 
        //        WHERE tbid = @tbid";

        //                foreach (var tbid in selectedToteIds)
        //                {
        //                    using (var cmd = new SqlCommand(sql, con, tran))
        //                    {
        //                        cmd.Parameters.AddWithValue("@tbid", tbid);
        //                        cmd.ExecuteNonQuery();
        //                    }
        //                }

        //                tran.Commit();
        //                lblStatus.InnerText = $"Marked {selectedToteIds.Count} tote-box(es) Busy.";
        //                lblStatus.Attributes["class"] = "alert alert-success";
        //            }
        //            catch (Exception ex)
        //            {
        //                tran.Rollback();
        //                lblStatus.InnerText = "Error updating tote-box status: " + ex.Message;
        //                lblStatus.Attributes["class"] = "alert alert-danger";
        //            }
        //        }
        //    }
        //}

        private Control FindControlRecursive(Control root, string id)
        {
            if (root.ID == id) return root;
            return root.Controls
                .Cast<Control>()
                .Select(c => FindControlRecursive(c, id))
                .FirstOrDefault(c => c != null);
        }

        private string GenerateWaybillNumber()
        {
            int nextNumber = 1;

            using (SqlConnection con = new SqlConnection(strcon))
            {
                con.Open();

                string query = @"SELECT TOP 1 WayNo FROM TblHeader WHERE WayNo LIKE 'CW-%' ORDER BY Id DESC";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    object result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        string lastWayNo = result.ToString();
                        string numberPart = lastWayNo.Substring(3); 

                        if (int.TryParse(numberPart, out int lastNumber))
                        {
                            nextNumber = lastNumber + 1;
                        }
                    }
                }
            }

            return "CW-" + nextNumber.ToString("D5"); 
        }

        private void BindStoreList()
        {
            using (SqlConnection con = new SqlConnection(strcon))
            {
                string query = "SELECT DISTINCT LTRIM(RTRIM(storeNo)) AS storeNo, id FROM stores WHERE storeNo IS NOT NULL AND storeNo <> '' AND storeNo <> 'HO'";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    lstStoreFilter1.DataSource = cmd.ExecuteReader();
                    lstStoreFilter1.DataTextField = "StoreNo";
                    lstStoreFilter1.DataValueField = "id";
                    lstStoreFilter1.DataBind();
                }
            }
        }

        private void BindCarList()
        {
            using (SqlConnection con = new SqlConnection(strcon))
            {
                string query = "SELECT DISTINCT LTRIM(RTRIM(carNo)) AS carNo, carId FROM TblCar WHERE carNo IS NOT NULL AND carNo <> '' AND status <> 'Busy' AND IsAvailable <> 0;";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    ddlCarNo.DataSource = cmd.ExecuteReader();
                    ddlCarNo.DataTextField = "carNo";
                    ddlCarNo.DataValueField = "carId";
                    ddlCarNo.DataBind();

                    ddlCarNo.Items.Insert(0, new ListItem("-- Select Car No --", "0"));
                }
            }
        }

        private void BindToteBox()
        {
            using (SqlConnection con = new SqlConnection(strcon))
            {
                string query = "SELECT DISTINCT LTRIM(RTRIM(tboxNo)) AS tboxNo, tbid, status, IsAvailable FROM TblToteBox WHERE tboxNo IS NOT NULL AND tboxNo <> '' AND IsAvailable <>0 AND status<>'Busy'";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    ddlToteBox1.DataSource = cmd.ExecuteReader();
                    ddlToteBox1.DataTextField = "tboxNo";
                    ddlToteBox1.DataValueField = "tbid";
                    ddlToteBox1.DataBind();
                }
            }
        }

        private int InsertWayHeader(SqlConnection con, SqlTransaction tran, string wayNo)
        {
            string sql = @"
            INSERT INTO TblHeader (WayNo, WayType, CarNo, CreatedDate, DriverName, DepDate, DepTime, FLocation, Transit) OUTPUT INSERTED.id
            VALUES (@WayNo, @WayType, @CarNo, @CreatedDate, @DriverName, @DepDate, @DepTime, @FLocation, @Transit)";

            int insertedId;

            using (SqlCommand cmd = new SqlCommand(sql, con, tran))
            {
                cmd.Parameters.AddWithValue("@WayNo", wayNo);
                cmd.Parameters.AddWithValue("@WayType", ddlWayType.SelectedValue);
                cmd.Parameters.AddWithValue("@CarNo", ddlCarNo.SelectedValue);
                cmd.Parameters.AddWithValue("@CreatedDate", DateTime.Parse(txtCreatedDate.Text));
                cmd.Parameters.AddWithValue("@DriverName", txtDriverName.Text.Trim());
                cmd.Parameters.AddWithValue("@DepDate", DateTime.Parse(txtDepDate.Text).Date);
                cmd.Parameters.AddWithValue("@DepTime", TimeSpan.Parse(txtDepTime.Text));
                cmd.Parameters.AddWithValue("@FLocation", txtFLocation.Text.Trim());
                cmd.Parameters.AddWithValue("@Transit", chkTransit.Checked);

                insertedId = (int)cmd.ExecuteScalar();
            }

            string updateSql = "UPDATE TblCar SET Status = 'Busy', IsAvailable = 0 WHERE carId = @carId";

            using (SqlCommand updateCmd = new SqlCommand(updateSql, con, tran))
            {
                updateCmd.Parameters.AddWithValue("@carId", ddlCarNo.SelectedValue);
                updateCmd.ExecuteNonQuery();
            }

            return insertedId;
        }

        private void InsertWayLines(SqlConnection con, SqlTransaction tran, int waybillId)
        {
            string gridDataJson = hdnGridData.Value;
            if (string.IsNullOrWhiteSpace(gridDataJson)) return;

            var lines = JsonConvert.DeserializeObject<List<WayLine>>(gridDataJson);

            foreach (var line in lines)
            {
                int detailId = InsertWayLine(con, tran, waybillId, line);
                InsertWayLineStores(con, tran, detailId, line.Stores);
                InsertWayLineToteBoxes(con, tran, detailId, line.ToteBoxNo);
            }
        }

        private int InsertWayLine(SqlConnection con, SqlTransaction tran, int waybillId, WayLine line)
        {
            string sql = @"
                INSERT INTO TblDetail (HId, PackingType, Qty)
                OUTPUT INSERTED.Did
                VALUES (@HId, @PackingType, @Qty)";

            using (SqlCommand cmd = new SqlCommand(sql, con, tran))
            {
                cmd.Parameters.Add("@HId", SqlDbType.Int).Value = waybillId;
                cmd.Parameters.Add("@PackingType", SqlDbType.NVarChar).Value = line.PackingType;
                cmd.Parameters.Add("@Qty", SqlDbType.Int).Value = line.Quantity;

                return (int)cmd.ExecuteScalar();
            }
        }

        private void InsertWayLineStores(SqlConnection con, SqlTransaction tran, int detailId, List<string> stores)
        {
            if (stores == null || stores.Count == 0) return;

            string sql = "INSERT INTO TblDetailStore (dId, Storeid) VALUES (@dId, @Storeid)";

            foreach (string store in stores)
            {
                using (SqlCommand cmd = new SqlCommand(sql, con, tran))
                {
                    cmd.Parameters.Add("@dId", SqlDbType.Int).Value = detailId;
                    cmd.Parameters.Add("@Storeid", SqlDbType.NVarChar).Value = store.Trim();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void InsertWayLineToteBoxes(SqlConnection con, SqlTransaction tran, int detailId, List<string> ToteBoxNo)
        {
            if (ToteBoxNo == null || ToteBoxNo.Count == 0) return;

            string insertSql = "INSERT INTO TblDetailToteBox (dId, tbid) VALUES (@dId, @tbid)";

            foreach (string tbox in ToteBoxNo)
            {
                using (SqlCommand insertCmd = new SqlCommand(insertSql, con, tran))
                {
                    insertCmd.Parameters.Add("@dId", SqlDbType.Int).Value = detailId;
                    insertCmd.Parameters.Add("@tbid", SqlDbType.NVarChar).Value = tbox.Trim();
                    insertCmd.ExecuteNonQuery();
                }
                
            }
        }

        [Serializable]
        public class GridRowData
        {
            public int LineNumber { get; set; }
            public string PackingType { get; set; }
            public List<string> Stores { get; set; }
            public int Quantity { get; set; }
            public List<string> ToteBoxNo { get; set; }
            public bool IsCommitted { get; set; }
        }
        public class WayLine
        {
            public int LineNumber { get; set; }
            public string PackingType { get; set; }
            public List<string> Stores { get; set; }
            public int Quantity { get; set; }
            public List<string> ToteBoxNo { get; set; }
        }

        private void cleareInput()
        {
            ddlWayType.SelectedIndex = 0;
            ddlCarNo.SelectedIndex = 0;
            txtDriverName.Text = "";
            txtDepDate.Text = "";
            txtDepTime.Text = "";
            txtFLocation.Text = "";
            chkTransit.Checked = false;

            ddlPackingType.SelectedIndex = 0;
            lstStoreFilter1.SelectedIndex = 0;
            txtQuantity.Text = string.Empty;
            ddlToteBox1.SelectedIndex = 0;

        }

    }
}