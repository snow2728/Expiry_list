using DocumentFormat.OpenXml.Office.Word;
using Newtonsoft.Json;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Web;
using System.Web.Script.Services;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;
using static Expiry_list.ReorderForm.rege1;

namespace Expiry_list.ConsignItem
{
    public partial class rege1 : System.Web.UI.Page
    {
        string strcon = ConfigurationManager.ConnectionStrings["con"].ConnectionString;
        public string selectedVendorText = string.Empty;

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Cache.SetExpires(DateTime.Now.AddSeconds(-1));
            Response.Cache.SetSlidingExpiration(false);

            if (Session["username"] == null)
            {
                Response.Redirect("~/loginPage.aspx");
            }

            if (!IsPostBack)
            {
                Session["Consign"] = null;

                string staff = Session["username"] as string;
                ViewState["StaffName"] = staff;

                no.Text = GetNextItemNo();
                tdyDate.Text = DateTime.Now.ToString("dd/MM/yyyy");
                BindGridView();
            }
            else
            {
                if (!string.IsNullOrEmpty(hiddenVendorNo.Value))
                {
                    var existingItem = vendorNo.Items.FindByValue(hiddenVendorNo.Value);
                    if (existingItem == null)
                    {
                        vendorNo.Items.Add(new ListItem(hiddenVendorText.Value, hiddenVendorNo.Value));
                    }

                    vendorNo.SelectedValue = hiddenVendorNo.Value;
                }
            }
        }

        protected void DisplaySessionData()
        {
            if (Session["username"] != null)
            {
                string sessionInfo = $"Username: {Session["username"]}<br/>";
                sessionInfo += $"Consign: {(Session["Consign"] != null ? "Exists" : "Does not exist")}<br/>";
                sessionInfo += $"Session ID: {Session.SessionID}<br/>";

                sessionDataLiteral.Text = sessionInfo;
            }
            else
            {
                sessionDataLiteral.Text = "No session data available.";
            }
        }

        protected string GetNextItemNo()
        {
            List<string> stores = GetLoggedInUserStoreNames();
            string storeName = stores.FirstOrDefault() ?? "DEFAULT";
            int lastNumber = GetLastItemNumber(storeName);

            return $"{storeName}-{lastNumber + 1}";
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

        private int GetLastItemNumber(string storeName)
        {
            string query = @"SELECT MAX(CAST(SUBSTRING(no, CHARINDEX('-', no) + 1, LEN(no)) AS INT))
                     FROM ItemListC
                     WHERE no LIKE @pattern";

            using (SqlConnection conn = new SqlConnection(strcon))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                cmd.Parameters.AddWithValue("@pattern", storeName + "-%");
                object result = cmd.ExecuteScalar();
                return result == DBNull.Value ? 0 : Convert.ToInt32(result);

            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<Vendor> GetVendors(string searchTerm)
        {
            try
            {
                List<Vendor> vendors = new List<Vendor>();
                string connectionString = ConfigurationManager.ConnectionStrings["con"].ConnectionString;

                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    using (var cmd = new SqlCommand(@"SELECT VendorNo, VendorName FROM Vendors 
                       WHERE VendorNo like 'CSG%' and (VendorName LIKE @SearchTerm OR VendorNo LIKE @SearchTerm)", con))
                    {
                        cmd.Parameters.AddWithValue("@SearchTerm", "%" + searchTerm + "%");

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                vendors.Add(new Vendor
                                {
                                    vendorNo = reader["VendorNo"].ToString(),
                                    vendorName = reader["VendorName"].ToString()
                                });
                            }
                        }
                    }
                }
                return vendors;
            }
            catch (Exception ex)
            {
                return new List<Vendor> {
                    new Vendor {
                        vendorNo = "ERROR",
                        vendorName = "An error occurred: " + ex.Message
                    }
                };
            }
        }

        // Add this new DTO class
        public class ItemDTO
        {
            public string ItemNo { get; set; }
            public string ItemDescription { get; set; }
            public string UOM { get; set; }
            public string PackingInfo { get; set; }
            public List<string> Barcode { get; set; }
        }

        public class GetItemsRequest
        {
            public string vendorNo { get; set; }
            public string searchTerm { get; set; }
        }
        
        public class Vendor
        {
            public string vendorNo { get; set; }
            public string vendorName { get; set; }
        }

        public class Item
        {
            public int Id { get; set; }
            public string No { get; set; }
            public string ItemNo { get; set; }
            public string ItemDescription { get; set; }
            public List<string> Barcode { get; set; }
            public string UOM { get; set; }
            public string PackingInfo { get; set; }
            public int Qty { get; set; }
            public int StoreNo { get; set; }
            public string StaffName { get; set; }
            public string VendorNo { get; set; }
            public string VendorName { get; set; }
            public DateTime RegeDate { get; set; }
            public string Status { get; set; }
            public string Note { get; set; }
        }

        protected void BindGridView(int pageNumber = 1, int pageSize = 100)
        {
            if (Session["username"] != null)
            {
                string currentStaffName = Session["username"].ToString();
                DataTable dt = Session["Consign"] as DataTable;

                if (dt == null)
                {
                    dt = new DataTable();
                    dt.Columns.Add("No", typeof(string));
                    dt.Columns.Add("ItemNo", typeof(string));
                    dt.Columns.Add("Description", typeof(string));
                    dt.Columns.Add("Qty", typeof(int));
                    dt.Columns.Add("PackingInfo", typeof(string));
                    dt.Columns.Add("UOM", typeof(string));
                    dt.Columns.Add("BarcodeNo", typeof(string));
                    dt.Columns.Add("StoreNo", typeof(string));
                    dt.Columns.Add("StaffName", typeof(string));                   
                    dt.Columns.Add("Note", typeof(string));
                    dt.Columns.Add("CompletedDate", typeof(DateTime));
                    dt.Columns.Add("registrationDate", typeof(DateTime));

                    Session["Consign"] = dt;
                }
                DataView dv = new DataView(dt);
                dv.RowFilter = $"StaffName = '{currentStaffName}'";
                //dv.Sort = "registrationDate DESC";

                Session["SortedConsign"] = dv;

                GridView.DataSource = dv;
                GridView.DataBind();
            }
            else
            {
                GridView.DataSource = null;
                GridView.DataBind();
            }
        }

        protected void GridView_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            GridView.PageIndex = e.NewPageIndex;
            BindGridView();
        }

        protected void GridView_RowEditing(object sender, GridViewEditEventArgs e)
        {
            GridView.EditIndex = e.NewEditIndex;
            BindGridView();
        }

        protected void GridView_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            GridView.EditIndex = -1;
            BindGridView();
        }

        protected void GridView_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            DataView dv = Session["SortedConsign"] as DataView;
            DataTable dt = dv.ToTable();

            if (dt != null)
            {
                DataRowView rowView = dv[e.RowIndex];
                DataRow row = rowView.Row;

                TextBox txtQty = (TextBox)GridView.Rows[e.RowIndex].FindControl("txtQuantity");
                TextBox txtNote = (TextBox)GridView.Rows[e.RowIndex].FindControl("txtNote");

                // Validate inputs
                int qtyValue;
                bool isValidQty = int.TryParse(txtQty.Text, out qtyValue);

                if (isValidQty)
                {
                    row["Qty"] = qtyValue;
                    row["Note"] = txtNote.Text.Trim();

                    // Update the original DataTable
                    DataTable originalDt = Session["Consign"] as DataTable;
                    originalDt.Rows[row.Table.Rows.IndexOf(row)]
                        .ItemArray = row.ItemArray;

                    Session["Consign"] = originalDt;
                    GridView.EditIndex = -1;
                    BindGridView();
                }
                else
                {
                    Response.Write("<script>alert('Invalid Quantity or Expiry Date!');</script>");
                }
            }
        }

        //protected void GridView_RowUpdating(object sender, GridViewUpdateEventArgs e)
        //{
        //    DataView dv = Session["SortedConsign"] as DataView;
        //    DataTable dt = dv.ToTable();

        //    if (dt != null)
        //    {
        //        DataRowView rowView = dv[e.RowIndex];
        //        DataRow row = rowView.Row;

        //        TextBox txtQty = (TextBox)GridView.Rows[e.RowIndex].FindControl("txtQuantity");
        //        TextBox txtNote = (TextBox)GridView.Rows[e.RowIndex].FindControl("txtNote");

        //        // Validate inputs
        //        int qtyValue;
        //        bool isValidQty = int.TryParse(txtQty.Text, out qtyValue);

        //        if (isValidQty)
        //        {
        //            row["Qty"] = qtyValue;
        //            row["Note"] = txtNote.Text.Trim();

        //            // Update the original DataTable
        //            DataTable originalDt = Session["Consign"] as DataTable;
        //            originalDt.Rows[row.Table.Rows.IndexOf(row)]
        //                .ItemArray = row.ItemArray;

        //            Session["Consign"] = originalDt;
        //            GridView.EditIndex = -1;

        //            List<string> storeList = GetLoggedInUserStoreNames();
        //            if (storeList == null || storeList.Count == 0)
        //            {
        //                ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
        //                    "swal('Error!', 'No store access assigned for this user!', 'error');", true);
        //                return;
        //            }
        //            var store = storeList[0];

        //            using (SqlConnection conn = new SqlConnection(strcon))
        //            {
        //                conn.Open();
        //                SqlTransaction transaction = conn.BeginTransaction();

        //                try
        //                {                            
        //                        string batchNo = GetNextItemNo();
        //                        string vendorNoVal = row["VendorNo"].ToString();
        //                        string vendorName = row["VendorName"].ToString();
        //                        string no = row["No"].ToString();
        //                        string itemNo = row["ItemNo"].ToString();
        //                        string description = row["Description"].ToString();
        //                        string quantity = row["Qty"].ToString();
        //                        string packing = row["PackingInfo"].ToString();
        //                        string unit = row["UOM"].ToString();
        //                        string barcode = row["BarcodeNo"].ToString();
        //                        string staff = row["StaffName"].ToString();
        //                        string note = row["Note"].ToString();

        //                        string insertQuery = @"
        //                                    INSERT INTO itemListC 
        //                                    (no, ItemNo, Description, BarcodeNo, Qty, UOM, PackingInfo, StoreNo, StaffName, VendorNo, VendorName, Note) 
        //                                    VALUES 
        //                                    (@no, @ItemNo, @Description, @BarcodeNo, @Qty, @UOM, @PackingInfo, @storeNo, @StaffName, @VendorNo, @VendorName, @Note)";
        //                        using (SqlCommand cmd = new SqlCommand(insertQuery, conn, transaction))
        //                        {
        //                            cmd.Parameters.AddWithValue("@no", batchNo);
        //                            cmd.Parameters.AddWithValue("@ItemNo", itemNo);
        //                            cmd.Parameters.AddWithValue("@Description", description);
        //                            cmd.Parameters.AddWithValue("@BarcodeNo", barcode);
        //                            cmd.Parameters.AddWithValue("@Qty", quantity);
        //                            cmd.Parameters.AddWithValue("@UOM", unit);
        //                            cmd.Parameters.AddWithValue("@PackingInfo", packing);
        //                            cmd.Parameters.AddWithValue("@storeNo", store);
        //                            cmd.Parameters.AddWithValue("@StaffName", staff);
        //                            cmd.Parameters.AddWithValue("@VendorNo", string.IsNullOrEmpty(vendorNoVal) ? DBNull.Value : (object)vendorNoVal);
        //                            cmd.Parameters.AddWithValue("@VendorName", string.IsNullOrEmpty(vendorName) ? DBNull.Value : (object)vendorName);
        //                            cmd.Parameters.AddWithValue("@Note", note);

        //                            cmd.ExecuteNonQuery();
        //                        }


        //                    transaction.Commit();
        //                }
        //                catch (Exception ex)
        //                {
        //                    try { transaction.Rollback(); } catch { }

        //                    hiddenVendorNo.Value = vendorNo.SelectedValue;
        //                    string safeMessage = ex.Message.Replace("'", "\\'").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", " ");
        //                    ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
        //                        $"swal('Error!', '{safeMessage}', 'error');", true);
        //                    return;
        //                }
        //            }

        //            BindGridView();
        //        }
        //        else
        //        {
        //            Response.Write("<script>alert('Invalid Quantity or Expiry Date!');</script>");
        //        }
        //    }
        //}

        protected void btnConfirmConsign_Click(object sender, EventArgs e)
        {            
            if (Session["Consign"] == null)
            {
                BindGridView();
                ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
                    "swal('Error!', 'There is no item in the table to confirm!', 'error');", true);
                return;
            }

            DataTable dt = Session["Consign"] as DataTable;
            var rows = dt.AsEnumerable().Where(r => r.Field<int?>("qty") !=null);
            if (rows.Count() == 0)
            {
                ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
                    "swal('Error!', 'There is no updated item! Please edit items first.', 'error');", true);
                return;
            }
            else if (rows.Count() != (Session["Consign"] as DataTable).Rows.Count)
            {
                ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
                    "swal('Error!', 'Some items’ quantities were not updated! Please edit items first.', 'error');", true);
                return;
            }


            List<string> storeList = GetLoggedInUserStoreNames();
            if (storeList == null || storeList.Count == 0)
            {
                ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
                    "swal('Error!', 'No store access assigned for this user!', 'error');", true);
                return;
            }

            using (SqlConnection conn = new SqlConnection(strcon))
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    foreach (string store in storeList)
                    {
                        string batchNo = GetNextItemNo();

                        foreach (DataRow row in rows)
                        {
                            string vendorNoVal = row["VendorNo"].ToString();
                            string vendorName = row["VendorName"].ToString();

                            GetVendorInfo(row["ItemNo"].ToString(), conn, transaction,
                                         out vendorNoVal, out vendorName);

                            string itemNo = row["ItemNo"].ToString();
                            string description = row["Description"].ToString();
                            string quantity = row["Qty"].ToString();
                            string packing = row["PackingInfo"].ToString();
                            string unit = row["UOM"].ToString();
                            string barcode = row["BarcodeNo"].ToString();
                            string staff = row["StaffName"].ToString();
                            string note = row["Note"].ToString();

                            string insertQuery = @"
                                INSERT INTO itemListC 
                                (no, ItemNo, Description, BarcodeNo, Qty, UOM, PackingInfo, StoreNo, StaffName, VendorNo, VendorName, Note) 
                                VALUES 
                                (@no, @ItemNo, @Description, @BarcodeNo, @Qty, @UOM, @PackingInfo, @storeNo, @StaffName, @VendorNo, @VendorName, @Note)";
                            using (SqlCommand cmd = new SqlCommand(insertQuery, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@no", batchNo);
                                cmd.Parameters.AddWithValue("@ItemNo", itemNo);
                                cmd.Parameters.AddWithValue("@Description", description);
                                cmd.Parameters.AddWithValue("@BarcodeNo", barcode);
                                cmd.Parameters.AddWithValue("@Qty", quantity);
                                cmd.Parameters.AddWithValue("@UOM", unit);
                                cmd.Parameters.AddWithValue("@PackingInfo", packing);
                                cmd.Parameters.AddWithValue("@storeNo", store);
                                cmd.Parameters.AddWithValue("@StaffName", staff);
                                cmd.Parameters.AddWithValue("@VendorNo", string.IsNullOrEmpty(vendorNoVal) ? DBNull.Value : (object)vendorNoVal);
                                cmd.Parameters.AddWithValue("@VendorName", string.IsNullOrEmpty(vendorName) ? DBNull.Value : (object)vendorName);
                                cmd.Parameters.AddWithValue("@Note", note);

                                cmd.ExecuteNonQuery();
                            }
                        }
                    }

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    try { transaction.Rollback(); } catch { }

                    hiddenVendorNo.Value = vendorNo.SelectedValue;
                    string safeMessage = ex.Message.Replace("'", "\\'").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", " ");
                    ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
                        $"swal('Error!', '{safeMessage}', 'error');", true);
                    return;
                }
            }
            ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
                "swal('Success!', 'Updated items added successfully!', 'success');", true);
            // Clean up and reset form UI
            Session["Consign"] = null;
            BindGridView();
            no.Text = GetNextItemNo();

            vendorNo.Items.Clear();
            vendorNo.Items.Add(new ListItem("", ""));
            vendorNo.SelectedValue = "";
            hiddenVendorNo.Value = "";
            selectedVendorText = "";            
        }


        protected void GridView_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            DataView dv = Session["SortedConsign"] as DataView;
            DataTable dt = Session["Consign"] as DataTable;

            if (dv != null && dt != null && e.RowIndex >= 0 && e.RowIndex < dv.Count)
            {
                DataRowView rowView = dv[e.RowIndex];

                DateTime registrationDate = (DateTime)rowView["registrationDate"];

                DataRow rowToDelete = dt.Rows.Cast<DataRow>()
                    .FirstOrDefault(r => (DateTime)r["registrationDate"] == registrationDate);

                if (rowToDelete != null)
                {
                    dt.Rows.Remove(rowToDelete);
                    Session["Consign"] = dt;
                    BindGridView();
                }
                else
                {
                    Response.Write("<script>alert('Row not found!');</script>");
                }
            }
            else
            {
                Response.Write("<script>alert('There is no data!');</script>");
            }
        }
        
        private void GetVendorInfo(string itemNo, SqlConnection conn, SqlTransaction transaction, out string vendorNo, out string vendorName)
        {
            vendorNo = null;
            vendorName = null;

            string query = @"SELECT TOP 1 v.VendorNo, v.VendorName 
                    FROM Vendors v 
                    INNER JOIN Items i ON v.VendorNo = i.VendorNo
                    WHERE i.ItemNo = @ItemNo";
            try
            {
                using (SqlCommand cmd = new SqlCommand(query, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@ItemNo", itemNo);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            vendorNo = reader["VendorNo"]?.ToString() ?? "";
                            vendorName = reader["VendorName"]?.ToString() ?? "";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Vendor lookup failed for item {itemNo}: {ex.Message}");
            }
        }

        protected void btnClearVendor_Click(object sender, EventArgs e)
        {
            if (Session["Consign"] != null && (Session["Consign"] as DataTable).Rows.Count > 0)
            {
                string script = @"Swal.fire({
                                    title: 'Are you sure to clear vendor?',
                                    html: '<div class=""swal-text-left"">Do you want to reset vendor selection and all consign items of that vendor?</div>',
                                    icon: 'question',
                                    showCancelButton: true,
                                    confirmButtonText: 'Ok',
                                    cancelButtonText: 'Cancel'
                                }).then((result) => {
                                    if (result.isConfirmed) {
                                       __doPostBack('" + btnHiddenOk.UniqueID + @"', '');
                                    }
                                });
                            ";

                ScriptManager.RegisterStartupScript(this, this.GetType(), "SwalConfirm", script, true);
            }
        }
        protected void btnGetItems_Click(object sender, EventArgs e)
        {
            string vendorNum = vendorNo.SelectedValue;
            if (Session["Consign"] != null)
            {
                Session["Consign"] = null;
            }
            GetConsignItems(vendorNum);            
        }

        protected void btnExportExcel_Click(object sender, EventArgs e)
        {
            if(Session["Consign"]!= null && (Session["Consign"] as DataTable).Rows.Count > 0)
            {
                ExportToExcel(Session["Consign"] as DataTable);
            }           
        }

        protected void btnOk_Click(object sender, EventArgs e)
        {
            vendorNo.ClearSelection();
            vendorNo.SelectedIndex = 0;
            vendorNo.SelectedValue = "";
            hiddenVendorNo.Value = null;
            hiddenVendorText.Value = null;
            if (Session["Consign"] != null)
            {
                Session["Consign"] = null;
            }
            BindGridView();
        }
        public void GetConsignItems(string vendorNum)
        {
            try
            {
                if (string.IsNullOrEmpty(vendorNum))
                {
                    return;
                }

                var items = new Dictionary<string, ItemDTO>();
                string connectionString = ConfigurationManager.ConnectionStrings["con"].ConnectionString;

                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    using (var cmd = new SqlCommand(@"
                    SELECT i.ItemNo, i.description, i.uom, i.packingInfo, i.barcodeNo 
                    FROM Items i                    
                    WHERE i.vendorNo = @vendorNo ", con)) //LEFT JOIN ItemBarcode b ON i.ItemNo = b.ItemNo
                    {
                        cmd.Parameters.AddWithValue("@vendorNo", vendorNum);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var itemNo = reader["ItemNo"].ToString();
                                var barcode = reader["barcodeNo"]?.ToString();

                                if (!items.TryGetValue(itemNo, out var item))
                                {
                                    item = new ItemDTO
                                    {
                                        ItemNo = itemNo,
                                        ItemDescription = reader["description"].ToString(),
                                        UOM = reader["uom"].ToString(),
                                        PackingInfo = reader["packingInfo"].ToString(),
                                        Barcode = new List<string>()
                                    };
                                    items.Add(itemNo, item);
                                }

                                if (!string.IsNullOrEmpty(barcode) && !item.Barcode.Contains(barcode))
                                {
                                    item.Barcode.Add(barcode);
                                }
                            }
                        }
                    }
                }
                string newItemNo = no.Text;

                DataTable dt = Session["Consign"] as DataTable;
                if (dt == null)
                {
                    dt = new DataTable();
                    dt.Columns.Add("No", typeof(string));
                    dt.Columns.Add("ItemNo", typeof(string));
                    dt.Columns.Add("Description", typeof(string));
                    dt.Columns.Add("Qty", typeof(int));
                    dt.Columns.Add("PackingInfo", typeof(string));
                    dt.Columns.Add("UOM", typeof(string));
                    dt.Columns.Add("BarcodeNo", typeof(string));
                    dt.Columns.Add("StoreNo", typeof(string));
                    dt.Columns.Add("StaffName", typeof(string));
                    dt.Columns.Add("Note", typeof(string));
                    dt.Columns.Add("registrationDate", typeof(DateTime));
                    dt.Columns.Add("VendorNo", typeof(string)); 
                    dt.Columns.Add("VendorName", typeof(string)); 
                }
                else
                {
                    if (!dt.Columns.Contains("VendorNo"))
                    {
                        dt.Columns.Add("VendorNo", typeof(string));
                    }

                    if (!dt.Columns.Contains("VendorName"))
                    {
                        dt.Columns.Add("VendorName", typeof(string));
                    }
                }

                foreach (ItemDTO item in items.Values)
                {
                    DataRow newRow = dt.NewRow();

                    newRow["No"] = newItemNo;
                    newRow["ItemNo"] = item.ItemNo;
                    newRow["Description"] = item.ItemDescription;
                    //newRow["Qty"] = int.Parse(qty.Text);
                    newRow["PackingInfo"] = item.PackingInfo;
                    newRow["UOM"] = item.UOM;
                    newRow["BarcodeNo"] = item.Barcode.Count > 0 ?item.Barcode[0]: null ;
                    newRow["StoreNo"] = GetLoggedInUserStoreNames();
                    newRow["StaffName"] = Session["username"].ToString(); ;
                    newRow["registrationDate"] = DateTime.Now;
                    //newRow["Note"] = note.Text;
                    newRow["VendorNo"] = vendorNo.SelectedValue; // Add vendor info
                    newRow["VendorName"] = vendorNo.SelectedItem?.Text ?? "";

                    dt.Rows.Add(newRow);
                }
                Session["Consign"] = dt;
                if(dt.Rows.Count > 0)
                {
                    BindGridView();
                } 
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"GetItems error: {ex}");
                //return new List<ItemDTO> {
                //    new ItemDTO {
                //        ItemNo = "ERROR",
                //        ItemDescription = "An error occurred: " + ex.Message
                //    }
                //};
            }
        }

        private void ExportToExcel(DataTable dt)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            Response.Clear();
            Response.Buffer = true;

            DataTable exportDt = dt.Copy();
            exportDt.Columns.Remove("StaffName");
            exportDt.Columns.Remove("BarcodeNo");
            exportDt.Columns.Remove("registrationDate");
            exportDt.Columns.Remove("storeNo");
            exportDt.Columns.Remove("vendorNo");
            exportDt.Columns.Remove("vendorName");

            using (ExcelPackage pck = new ExcelPackage())
            {
                ExcelWorksheet ws = pck.Workbook.Worksheets.Add("ConsignItems");

                ws.Cells["A1"].LoadFromDataTable(exportDt, true);
                
                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                using (ExcelRange header = ws.Cells[1, 1, 1, exportDt.Columns.Count])
                {
                    header.Style.Font.Bold = true;
                    header.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    header.Style.Fill.BackgroundColor.SetColor(color: System.Drawing.Color.LightGray);
                }

                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", "attachment; filename=ConsignItems.xlsx");
                Response.BinaryWrite(pck.GetAsByteArray());
                Response.Flush();
                Response.End();
            }
        }
        
    }
}