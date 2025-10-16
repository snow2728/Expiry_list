using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Script.Services;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;



namespace Expiry_list
{
    public partial class registrationForm : System.Web.UI.Page
    {
        string strcon = ConfigurationManager.ConnectionStrings["con"].ConnectionString;

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
                clearForm();

                string staff = Session["username"] as string;
                ViewState["StaffName"] = staff;
                staffName.Text = staff;
                hiddenStaffName.Value = staff;

                no.Text = GetNextItemNo();
                storeNo.Text = string.Join(",", Common.GetLoggedInUserStoreNames());
                tdyDate.Text = DateTime.Now.ToString("dd/MM/yyyy");

                //DisplaySessionData();
                BindGridView();

            }
            else
            {
                //no.Text = GetNextItemNo();
                staffName.Text = ViewState["StaffName"] as string;
                hiddenStaffName.Value = ViewState["StaffName"] as string;
            }
        }

        protected void DisplaySessionData()
        {
            if (Session["username"] != null)
            {
                string sessionInfo = $"Username: {Session["username"]}<br/>";
                sessionInfo += $"ItemLines: {(Session["ItemLines"] != null ? "Exists" : "Does not exist")}<br/>";
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
            // Get first store name
            List<string> stores = Common.GetLoggedInUserStoreNames();
            string storeName = stores.FirstOrDefault() ?? "DEFAULT";
            int lastNumber = GetLastItemNumber(storeName);

            return $"{storeName}-{lastNumber + 1}";
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

        //Find item
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<Item> GetItems(string searchTerm)
        {
            try
            {
                var items = new Dictionary<string, Item>();
                string connectionString = ConfigurationManager.ConnectionStrings["con"].ConnectionString;

                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    using (var cmd = new SqlCommand(@"
                        SELECT i.ItemNo, i.description, i.uom, i.packingInfo, b.barcodeNo 
                        FROM Items i
                        LEFT JOIN ItemBarcode b ON i.ItemNo = b.ItemNo
                        WHERE i.ItemNo LIKE @SearchTerm 
                        OR i.description LIKE @SearchTerm 
                        OR b.barcodeNo LIKE @SearchTerm", con))
                    {
                        cmd.Parameters.AddWithValue("@SearchTerm", "%" + searchTerm + "%");

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var itemNo = reader["ItemNo"].ToString();
                                var barcode = reader["barcodeNo"]?.ToString();

                                if (!items.TryGetValue(itemNo, out var item))
                                {
                                    item = new Item
                                    {
                                        ItemNo = itemNo,
                                        ItemDescription = reader["description"].ToString(),
                                        UOM = reader["uom"].ToString(),
                                        PackingInfo = reader["packingInfo"].ToString(),
                                        Barcode = new List<string>()
                                    };
                                    items.Add(itemNo, item);
                                }

                                if (!string.IsNullOrEmpty(barcode))
                                    item.Barcode.Add(barcode);
                            }
                        }
                    }
                }

                return items.Values.ToList();
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while fetching items: " + ex.Message);
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static Item GetItemDetails(string itemNo)
        {
            return GetItems(itemNo).FirstOrDefault();
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
            public DateTime ExpiryDate { get; set; }
            public int StoreNo { get; set; }
            public string StaffName { get; set; }
            public string BatchNo { get; set; }
            public string VendorNo { get; set; }
            public string VendorName { get; set; }
            public DateTime RegeDate { get; set; }
            public int Action { get; set; }
            public string Status { get; set; }
            public string Note { get; set; }
        }

        //for select2
        private void FetchItemDetails(string itemNo)
        {
            try
            {
                string query = "SELECT * FROM Items WHERE ItemNo = @ItemNo";
                using (SqlConnection conn = new SqlConnection(strcon))
                {
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ItemNo", itemNo);
                        conn.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                desc.Text = reader["description"].ToString();
                                barcodeNo.Text = reader["barcodeNo"].ToString();
                                uom.Text = reader["uom"].ToString();
                                packingInfo.Text = reader["packingInfo"].ToString();
                            }
                            else
                            {
                                ShowAlert("Item not found.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowAlert("Error fetching item details: " + ex.Message);
            }
        }

        private void ShowAlert(string message)
        {
            string script = $"alert('{message.Replace("'", "\\'")}');";
            ClientScript.RegisterStartupScript(this.GetType(), "alert", script, true);
        }

        protected void addBtn_Click1(object sender, EventArgs e)
        {
            string newItemNo = no.Text;

            DataTable dt = Session["ItemLines"] as DataTable;
            if (dt == null)
            {
                dt = new DataTable();
                dt.Columns.Add("No", typeof(string));
                dt.Columns.Add("ItemNo", typeof(string));
                dt.Columns.Add("Description", typeof(string));
                dt.Columns.Add("Qty", typeof(int));
                dt.Columns.Add("ExpiryDate", typeof(DateTime));
                dt.Columns.Add("PackingInfo", typeof(string));
                dt.Columns.Add("UOM", typeof(string));
                dt.Columns.Add("BarcodeNo", typeof(string));
                dt.Columns.Add("BatchNo", typeof(string));
                dt.Columns.Add("StoreNo", typeof(string));
                dt.Columns.Add("StaffName", typeof(string));
                dt.Columns.Add("Note", typeof(string));
                dt.Columns.Add("registrationDate", typeof(DateTime));
                dt.Columns.Add("Remark", typeof(string));
            }

            bool itemExists = dt.AsEnumerable().Any(row =>
                row["ItemNo"].ToString() == itemNo.SelectedValue &&
                row["ExpiryDate"].ToString() == expiryDate.Text);

            if (!itemExists)
            {
                DataRow newRow = dt.NewRow();

                newRow["No"] = newItemNo;
                newRow["ItemNo"] = hiddenSelectedItem.Value;
                newRow["Description"] = hiddenDescription.Value;
                newRow["Qty"] = int.Parse(qty.Text);
                newRow["ExpiryDate"] = DateTime.Parse(expiryDate.Text);
                newRow["PackingInfo"] = hiddenPackingInfo.Value;
                newRow["UOM"] = hiddenUOM.Value;
                newRow["BarcodeNo"] = hiddenBarcodeNo.Value;
                newRow["BatchNo"] = batchNo.Text;
                newRow["StoreNo"] = Common.GetLoggedInUserStoreNames().FirstOrDefault() ?? "";
                newRow["StaffName"] = staffName.Text;
                newRow["registrationDate"] = DateTime.Now;
                newRow["Note"] = note.Text;
                newRow["Remark"] = null;

                dt.Rows.Add(newRow);
                Session["ItemLines"] = dt;

                BindGridView();
                clearForm();
            }
            else
            {
                ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
                    "swal('Error!', 'Item already exists in table!', 'error');", true);
            }
        }

        protected void BindGridView()
        {
            if (Session["username"] != null)
            {
                string currentStaffName = Session["username"].ToString();
                DataTable dt = Session["ItemLines"] as DataTable;

                if (dt == null)
                {
                    dt = new DataTable();
                    dt.Columns.Add("No", typeof(string));
                    dt.Columns.Add("ItemNo", typeof(string));
                    dt.Columns.Add("Description", typeof(string));
                    dt.Columns.Add("Qty", typeof(int));
                    dt.Columns.Add("ExpiryDate", typeof(DateTime));
                    dt.Columns.Add("PackingInfo", typeof(string));
                    dt.Columns.Add("UOM", typeof(string));
                    dt.Columns.Add("BarcodeNo", typeof(string));
                    dt.Columns.Add("BatchNo", typeof(string));
                    dt.Columns.Add("StoreNo", typeof(string));
                    dt.Columns.Add("StaffName", typeof(string));
                    dt.Columns.Add("Note", typeof(string));
                    dt.Columns.Add("Remark", typeof(string));
                    dt.Columns.Add("CompletedDate", typeof(DateTime));
                    dt.Columns.Add("registrationDate", typeof(DateTime));

                    Session["ItemLines"] = dt;
                }

                DataView dv = new DataView(dt);
                dv.RowFilter = $"StaffName = '{currentStaffName}'";
                dv.Sort = "registrationDate DESC"; 

                Session["SortedItemLines"] = dv;

                gridTable.DataSource = dv;
                gridTable.DataBind();
            }
            else
            {
                gridTable.DataSource = null;
                gridTable.DataBind();
            }
        }

        protected void clearForm()
        {
            itemNo.Text = string.Empty;
            desc.Text = string.Empty;
            barcodeNo.Text = string.Empty;
            uom.Text = string.Empty;
            packingInfo.Text = string.Empty;
            expiryDate.Text = string.Empty;
            qty.Text = string.Empty;
            batchNo.Text = string.Empty;
            note.Text = string.Empty;

            hiddenSelectedItem.Value = string.Empty;
            hiddenBarcodeNo.Value = string.Empty;
            hiddenDescription.Value = string.Empty;
            hiddenUOM.Value = string.Empty;
            hiddenPackingInfo.Value = string.Empty;

        }

        protected void itemNo_SelectedIndexChanged1(object sender, EventArgs e)
        {
            string selectedItemNo = itemNo.SelectedValue;

            if (!string.IsNullOrEmpty(selectedItemNo))
            {

                FetchItemDetails(selectedItemNo);
            }
        }

        protected void GridView_RowEditing(object sender, GridViewEditEventArgs e)
        {
            gridTable.EditIndex = e.NewEditIndex;
            BindGridView();
        }

        protected void GridView_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            gridTable.EditIndex = -1;
            BindGridView();
        }

        protected void GridView_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            DataView dv = Session["SortedItemLines"] as DataView;
            DataTable dt = dv.ToTable(); 

            if (dt != null)
            {
                DataRowView rowView = dv[e.RowIndex];
                DataRow row = rowView.Row;

                TextBox txtQty = (TextBox)gridTable.Rows[e.RowIndex].FindControl("txtQuantity");
                TextBox txtExpiryDate = (TextBox)gridTable.Rows[e.RowIndex].FindControl("txtExpiryDate");
                TextBox txtBatch = (TextBox)gridTable.Rows[e.RowIndex].FindControl("txtBatch");
                TextBox txtNote = (TextBox)gridTable.Rows[e.RowIndex].FindControl("txtNote");

                // Validate inputs
                int qtyValue;
                DateTime expiryDateValue;
                bool isValidQty = int.TryParse(txtQty.Text, out qtyValue);
                bool isValidExpiryDate = DateTime.TryParseExact(
                    txtExpiryDate.Text,
                    "yyyy-MM",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out expiryDateValue
                );

                if (isValidQty && isValidExpiryDate)
                {
                    // Update the row from the sorted DataView
                    row["Qty"] = qtyValue;
                    row["ExpiryDate"] = expiryDateValue;
                    row["BatchNo"] = txtBatch.Text.Trim();
                    row["Note"] = txtNote.Text.Trim();

                    DataTable originalDt = Session["ItemLines"] as DataTable;
                    originalDt.Rows[row.Table.Rows.IndexOf(row)] 
                        .ItemArray = row.ItemArray;

                    Session["ItemLines"] = originalDt;
                    gridTable.EditIndex = -1;
                    BindGridView();
                }
                else
                {
                    Response.Write("<script>alert('Invalid Quantity or Expiry Date!');</script>");
                }
            }
        }

        protected void GridView_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            DataView dv = Session["SortedItemLines"] as DataView; 
            DataTable dt = Session["ItemLines"] as DataTable;  

            if (dv != null && dt != null && e.RowIndex >= 0 && e.RowIndex < dv.Count)
            {
                DataRowView rowView = dv[e.RowIndex];
                DateTime registrationDate = (DateTime)rowView["registrationDate"];

                DataRow rowToDelete = dt.Rows.Cast<DataRow>()
                    .FirstOrDefault(r => (DateTime)r["registrationDate"] == registrationDate);

                if (rowToDelete != null)
                {
                    dt.Rows.Remove(rowToDelete); 
                    Session["ItemLines"] = dt;
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

        protected void btnConfirmAll_Click1(object sender, EventArgs e)
        {
            bool hasUnsavedValues =
                !string.IsNullOrEmpty(hiddenSelectedItem.Value) ||
                !string.IsNullOrEmpty(desc.Text) ||
                !string.IsNullOrEmpty(barcodeNo.Text) ||
                !string.IsNullOrEmpty(uom.Text) ||
                !string.IsNullOrEmpty(packingInfo.Text) ||
                !string.IsNullOrEmpty(expiryDate.Text) ||
                !string.IsNullOrEmpty(qty.Text) ||
                !string.IsNullOrEmpty(batchNo.Text);

            if (hasUnsavedValues)
            {
                ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
                    "swal('Warning!', 'You have unsaved values in the form!', 'warning');", true);

                desc.Text = hiddenDescription.Value;
                uom.Text = hiddenUOM.Value;
                packingInfo.Text = hiddenPackingInfo.Value;
                barcodeNo.Text = hiddenBarcodeNo.Value;
                return;
            }

            if (Session["ItemLines"] == null)
            {
                BindGridView();
                clearForm();
                ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
                    "swal('Error!', 'There are no items in the table to confirm!', 'error');", true);
                return;
            }

            DataTable dt = Session["ItemLines"] as DataTable;
            if (dt.Rows.Count == 0)
            {
                ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
                    "swal('Error!', 'There are no items in the GridView! Please add items first.', 'error');", true);
                return;
            }

            List<string> storeList = Common.GetLoggedInUserStoreNames();
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
                    string lastGeneratedNo = string.Empty;

                    foreach (string store in storeList)
                    {
                        //int lastNumber = GetLastItemNumber(store);
                        string batchNo = GetNextItemNo();

                        foreach (DataRow row in dt.Rows)
                        {
                            string vendorNo, vendorName;
                            GetVendorInfo(row["ItemNo"].ToString(), conn, transaction,
                                         out vendorNo, out vendorName);

                            DateTime expiryDate = Convert.ToDateTime(row["ExpiryDate"]);

                            string insertQuery = @"
                                INSERT INTO ItemList 
                                (no, ItemNo, Description, BarcodeNo, Qty, UOM, PackingInfo, 
                                 ExpiryDate, StoreNo, StaffName, BatchNo, VendorNo, VendorName, Note, Remark) 
                                VALUES 
                                (@no, @ItemNo, @Description, @BarcodeNo, @Qty, @UOM, @PackingInfo, 
                                 @ExpiryDate, @storeNo, @StaffName, @BatchNo, @VendorNo, @VendorName, @Note, @Remark)";

                            using (SqlCommand cmd = new SqlCommand(insertQuery, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@no", batchNo);
                                cmd.Parameters.AddWithValue("@ItemNo", row["ItemNo"]);
                                cmd.Parameters.AddWithValue("@Description", row["Description"]);
                                cmd.Parameters.AddWithValue("@BarcodeNo", row["BarcodeNo"]);
                                cmd.Parameters.AddWithValue("@Qty", Convert.ToInt32(row["Qty"]));
                                cmd.Parameters.AddWithValue("@UOM", row["UOM"]);
                                cmd.Parameters.AddWithValue("@PackingInfo", row["PackingInfo"]);
                                cmd.Parameters.AddWithValue("@ExpiryDate", expiryDate);
                                cmd.Parameters.AddWithValue("@storeNo", store);
                                cmd.Parameters.AddWithValue("@StaffName", row["StaffName"]);
                                cmd.Parameters.AddWithValue("@BatchNo", row["BatchNo"]);
                                cmd.Parameters.AddWithValue("@VendorNo", (object)vendorNo ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@VendorName", (object)vendorName ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Note", row["Note"]);
                                cmd.Parameters.AddWithValue("@Remark", row["Remark"] ?? DBNull.Value);

                                cmd.ExecuteNonQuery();
                            }
                        }
                    }

                    transaction.Commit();
                    Session["ItemLines"] = null;
                    BindGridView();
                    no.Text = GetNextItemNo();

                    ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
                        "swal('Success!', 'All items added successfully!', 'success');", true);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    // Inside catch block
                    string safeMessage = ex.Message.Replace("'", "\\'").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", " ");
                    ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
                        $"swal('Error!', '{safeMessage}', 'error');", true);
                }
            }
        }

        private int GetLastItemNumber(string storeName)
        {
            string query = @"SELECT MAX(CAST(SUBSTRING(no, CHARINDEX('-', no) + 1, LEN(no)) AS INT))
                     FROM ItemList
                     WHERE no LIKE @pattern";

            using (SqlConnection conn = new SqlConnection(strcon))
            using (SqlCommand cmd = new SqlCommand(query,conn))
            {
                conn.Open();
                cmd.Parameters.AddWithValue("@pattern", storeName + "-%");
                object result = cmd.ExecuteScalar();
                return result == DBNull.Value ? 0 : Convert.ToInt32(result);
               
            }
        }

        private void GetVendorInfo(string itemNo, SqlConnection conn, SqlTransaction transaction, out string vendorNo, out string vendorName)
        {
            // Always set default values
            vendorNo = null;
            vendorName = null;

            string query = @"SELECT TOP 1 v.VendorNo, v.VendorName 
            FROM Vendors v 
            INNER JOIN Items i ON REPLACE(v.VendorNo, ' ', '') = REPLACE(i.VendorNo, ' ', '')
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

    }
}
