using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
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

<<<<<<< HEAD
=======
        private string storeName;

>>>>>>> dd28a8dd26355ac93475b3760a0023853d81994b
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Cache.SetExpires(DateTime.Now.AddSeconds(-1));
            Response.Cache.SetSlidingExpiration(false);

            if (Session["username"] == null)
            {
                Response.Redirect("loginPage.aspx");
            }

            if (!IsPostBack)
            {
                clearForm();
<<<<<<< HEAD
=======
                storeName = GetLoggedInUserStoreName();
>>>>>>> dd28a8dd26355ac93475b3760a0023853d81994b

                string staff = Session["username"] as string;
                ViewState["StaffName"] = staff;
                staffName.Text = staff;
                hiddenStaffName.Value = staff;

                no.Text = GetNextItemNo();
<<<<<<< HEAD
                storeNo.Text = string.Join(",", GetLoggedInUserStoreNames()); ;
=======
                storeNo.Text = storeName;
>>>>>>> dd28a8dd26355ac93475b3760a0023853d81994b
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
<<<<<<< HEAD
            string store1 = string.Join(",", GetLoggedInUserStoreNames());
            string lastItemNo = GetLastItemNo(store1);

=======
            string store1 = GetLoggedInUserStoreName();

            string lastItemNo = GetLastItemNo(store1);
>>>>>>> dd28a8dd26355ac93475b3760a0023853d81994b
            int lastNumber = 0;

            if (!string.IsNullOrEmpty(lastItemNo))
            {
                string[] parts = lastItemNo.Split('-');
                if (parts.Length == 2 && int.TryParse(parts[1], out lastNumber))
                {
<<<<<<< HEAD
                    lastNumber++;
=======
                    lastNumber++; 
>>>>>>> dd28a8dd26355ac93475b3760a0023853d81994b
                }
            }

            return $"{store1}-{lastNumber}";
        }

        private string GetLastItemNo(string storeName)
        {
            string query = @"SELECT TOP 1 No FROM itemList WHERE No LIKE @StoreNo ORDER BY CAST(SUBSTRING(No, CHARINDEX('-', No) + 1, LEN(No)) AS INT) DESC";

            string lastItemNo = null;
            using (SqlConnection conn = new SqlConnection(strcon))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@StoreNo", storeName + "-%");

                conn.Open();

                object result = cmd.ExecuteScalar();

                if (result != null)
                {
                    lastItemNo = result.ToString();
                }
            }
            return lastItemNo;
        }

        // get the storeName of logged in user's storeNo
<<<<<<< HEAD
        private List<string> GetLoggedInUserStoreNames()
        {
            List<string> storeNos = Session["storeListRaw"] as List<string>;
            List<string> storeNames = new List<string>();

            if (storeNos == null || storeNos.Count == 0)
                return storeNames;

            string query = $"SELECT storeNo FROM Stores WHERE storeNo IN ({string.Join(",", storeNos.Select((s, i) => $"@store{i}"))})";
=======
        private string GetLoggedInUserStoreName()
        {
            int storeNo = Convert.ToInt32(Session["storeNo"] ?? 0);

            if (storeNo == 0)
            {
                return null;
            }

            string storeName = null;
            string query = "SELECT StoreNo FROM Stores WHERE id = @StoreNo";
>>>>>>> dd28a8dd26355ac93475b3760a0023853d81994b

            using (SqlConnection conn = new SqlConnection(strcon))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
<<<<<<< HEAD
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
=======
                cmd.Parameters.AddWithValue("@StoreNo", storeNo);
                conn.Open();

                storeName = cmd.ExecuteScalar()?.ToString();
            }

            return storeName;
>>>>>>> dd28a8dd26355ac93475b3760a0023853d81994b
        }

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

        //get the item details
        //[WebMethod]
        //[ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        //public static Item GetItemDetails(string itemNo)
        //{
        //    Item itemDetails = new Item();

        //    string connectionString = ConfigurationManager.ConnectionStrings["con"].ConnectionString;
        //    using (SqlConnection conn = new SqlConnection(connectionString))
        //    {
        //        // Corrected SQL query with proper JOIN condition
        //        string query = @"
        //    SELECT i.ItemNo, i.description, i.uom, i.packingInfo, b.barcodeNo 
        //    FROM Items i
        //    LEFT JOIN ItemBarcode b ON i.ItemNo = b.ItemNo
        //    WHERE i.ItemNo = @ItemNo";

        //        using (SqlCommand cmd = new SqlCommand(query, conn))
        //        {
        //            cmd.Parameters.AddWithValue("@ItemNo", itemNo);
        //            conn.Open();

        //            using (SqlDataReader reader = cmd.ExecuteReader())
        //            {
        //                while (reader.Read())
        //                {
        //                    if (itemDetails.ItemNo == null)
        //                    {
        //                        itemDetails = new Item
        //                        {
        //                            ItemNo = reader["ItemNo"].ToString(),
        //                            ItemDescription = reader["description"].ToString(),
        //                            Barcode = new List<string>(),
        //                            UOM = reader["uom"].ToString(),
        //                            PackingInfo = reader["packingInfo"].ToString()
        //                        };
        //                    }

        //                    if (reader["barcodeNo"] != DBNull.Value)
        //                    {
        //                        itemDetails.Barcode.Add(reader["barcodeNo"].ToString());
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    return itemDetails;
        //}

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
<<<<<<< HEAD
              string newItemNo = no.Text;
=======
            string newItemNo = no.Text;
>>>>>>> dd28a8dd26355ac93475b3760a0023853d81994b

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

            // Check if item already exists in the DataTable
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
<<<<<<< HEAD
                newRow["StoreNo"] = GetLoggedInUserStoreNames();
=======
                newRow["StoreNo"] = GetLoggedInUserStoreName();
>>>>>>> dd28a8dd26355ac93475b3760a0023853d81994b
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
                    dt.Columns.Add("registrationDate", typeof(DateTime)); // Add this column

                    Session["ItemLines"] = dt;
                }

                DataView dv = new DataView(dt);
                dv.RowFilter = $"StaffName = '{currentStaffName}'";
                dv.Sort = "registrationDate DESC"; // Sort by registrationDate descending

                // Store the sorted DataView in Session
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
            DataView dv = Session["SortedItemLines"] as DataView; // Retrieve sorted DataView
            DataTable dt = dv.ToTable(); // Convert back to DataTable

            if (dt != null)
            {
                // Get the row from the sorted DataView
                DataRowView rowView = dv[e.RowIndex];
                DataRow row = rowView.Row;

                // Get control values from the GridView row
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

                    // Update the original DataTable
                    DataTable originalDt = Session["ItemLines"] as DataTable;
                    originalDt.Rows[row.Table.Rows.IndexOf(row)] // Map back to original DataTable
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

<<<<<<< HEAD
=======
        //protected void barcodeNo_SelectedIndexChanged(object sender, EventArgs e)
        //{
        //    string selectedBarcodeNo = barcodeNo.Text;

        //    if (!string.IsNullOrEmpty(selectedBarcodeNo))
        //    {
        //        // Auto-fill other fields based on barcode
        //        FetchItemDetailsByBarcode(selectedBarcodeNo);
        //    }
        //}

        //private void FetchItemDetailsByBarcode(string barcodeNo)
        //{
        //    Item itemDetails = GetItemDetailsByBarcode(barcodeNo);

        //    if (itemDetails != null)
        //    {
        //        desc.Text = itemDetails.ItemDescription;
        //        uom.Text = itemDetails.UOM;
        //        packingInfo.Text = itemDetails.PackingInfo;

        //        // Populate the itemNo dropdown with items related to this barcode
        //        BindItemDropdownByBarcode(barcodeNo);
        //    }
        //}

        //protected void BindItemDropdownByBarcode(string barcode)
        //{
        //    List<Item> items = GetItemsByBarcode(barcode);

        //    itemNo.Items.Clear();
        //    if (items.Count > 0)
        //    {
        //        foreach (var item in items)
        //        {
        //            ListItem newItem = new ListItem(item.ItemNo + " - " + item.ItemDescription, item.ItemNo);
        //            itemNo.Items.Add(newItem);
        //        }
        //    }
        //}

        //[WebMethod]
        //[ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        //public static List<Item> GetItemsByBarcode(string searchBarcode)
        //{
        //    List<Item> items = new List<Item>();

        //    try
        //    {
        //        string connectionString = ConfigurationManager.ConnectionStrings["con"].ConnectionString;
        //        using (SqlConnection con = new SqlConnection(connectionString))
        //        {
        //            con.Open();
        //            using (SqlCommand cmd = new SqlCommand("SELECT * FROM Items WHERE barcodeNo LIKE @SearchBarcode", con))
        //            {
        //                cmd.Parameters.AddWithValue("@SearchBarcode", "%" + searchBarcode + "%");

        //                using (SqlDataReader reader = cmd.ExecuteReader())
        //                {
        //                    while (reader.Read())
        //                    {
        //                        items.Add(new Item
        //                        {
        //                            ItemNo = reader["ItemNo"].ToString(),
        //                            ItemDescription = reader["description"].ToString(),
        //                            Barcode = reader["barcodeNo"].ToString(),
        //                            UOM = reader["uom"].ToString(),
        //                            PackingInfo = reader["packingInfo"].ToString()
        //                        });
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        HttpContext.Current.Response.Write(ex.Message);
        //        throw new Exception("Error fetching items by barcode: " + ex.Message);
        //    }

        //    return items;
        //}


        //[WebMethod]
        //public static Item GetItemDetailsByBarcode(string barcodeNo)
        //{
        //    Item itemDetails = new Item();

        //    string connectionString = ConfigurationManager.ConnectionStrings["con"].ConnectionString;
        //    using (SqlConnection conn = new SqlConnection(connectionString))
        //    {
        //        string query = "SELECT * FROM Items WHERE barcodeNo = @BarcodeNo";
        //        using (SqlCommand cmd = new SqlCommand(query, conn))
        //        {
        //            cmd.Parameters.AddWithValue("@BarcodeNo", barcodeNo);
        //            conn.Open();

        //            using (SqlDataReader reader = cmd.ExecuteReader())
        //            {
        //                if (reader.Read())
        //                {
        //                    itemDetails = new Item
        //                    {
        //                        ItemNo = reader["ItemNo"].ToString(),
        //                        ItemDescription = reader["description"].ToString(),
        //                        Barcode = reader["barcodeNo"].ToString(),
        //                        UOM = reader["uom"].ToString(),
        //                        PackingInfo = reader["packingInfo"].ToString()
        //                    };
        //                }
        //            }
        //        }
        //    }
        //    return itemDetails;
        //}

>>>>>>> dd28a8dd26355ac93475b3760a0023853d81994b
        protected void GridView_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            DataView dv = Session["SortedItemLines"] as DataView; // Sorted DataView
            DataTable dt = Session["ItemLines"] as DataTable;     // Original DataTable

            if (dv != null && dt != null && e.RowIndex >= 0 && e.RowIndex < dv.Count)
            {
                // Get the row from the sorted DataView (latest item first)
                DataRowView rowView = dv[e.RowIndex];

                // Extract the unique registrationDate from the row
                DateTime registrationDate = (DateTime)rowView["registrationDate"];

                // Find the row in the original DataTable by registrationDate
                DataRow rowToDelete = dt.Rows.Cast<DataRow>()
                    .FirstOrDefault(r => (DateTime)r["registrationDate"] == registrationDate);

                if (rowToDelete != null)
                {
                    dt.Rows.Remove(rowToDelete); // Remove the correct row
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

        //add the item lines to database
        protected void btnConfirmAll_Click1(object sender, EventArgs e)
        {
            bool hasUnsavedValues =
<<<<<<< HEAD
                !string.IsNullOrEmpty(hiddenSelectedItem.Value) ||
                !string.IsNullOrEmpty(desc.Text) ||
                !string.IsNullOrEmpty(barcodeNo.Text) ||
                !string.IsNullOrEmpty(uom.Text) ||
                !string.IsNullOrEmpty(packingInfo.Text) ||
                !string.IsNullOrEmpty(expiryDate.Text) ||
                !string.IsNullOrEmpty(qty.Text) ||
                !string.IsNullOrEmpty(batchNo.Text);
=======
             !string.IsNullOrEmpty(hiddenSelectedItem.Value) ||
             !string.IsNullOrEmpty(desc.Text) ||
             !string.IsNullOrEmpty(barcodeNo.Text) ||
             !string.IsNullOrEmpty(uom.Text) ||
             !string.IsNullOrEmpty(packingInfo.Text) ||
             !string.IsNullOrEmpty(expiryDate.Text) ||
             !string.IsNullOrEmpty(qty.Text) ||
             !string.IsNullOrEmpty(batchNo.Text);
             //!string.IsNullOrEmpty(note.Text); // Include note.Text
>>>>>>> dd28a8dd26355ac93475b3760a0023853d81994b

            if (hasUnsavedValues)
            {
                ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
                    "swal('Warning!', 'You have unsaved values in the form!', 'warning');", true);

                desc.Text = hiddenDescription.Value;
                uom.Text = hiddenUOM.Value;
                packingInfo.Text = hiddenPackingInfo.Value;
                barcodeNo.Text = hiddenBarcodeNo.Value;
<<<<<<< HEAD
=======
                //clearForm();
>>>>>>> dd28a8dd26355ac93475b3760a0023853d81994b
                return;
            }

            if (gridTable.Rows.Count == 0)
            {
                ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
                    "swal('Error!', 'There are no items in the GridView! Please add items first.', 'error');", true);
                return;
            }

<<<<<<< HEAD
            List<string> storeList = GetLoggedInUserStoreNames();
            if (storeList == null || storeList.Count == 0)
            {
                ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
                    "swal('Error!', 'No store access assigned for this user!', 'error');", true);
                return;
=======
            string store1 = GetLoggedInUserStoreName();

            string lastItemNo = GetLastItemNo(store1);
            int lastNumber = 0;

            if (!string.IsNullOrEmpty(lastItemNo))
            {
                string[] parts = lastItemNo.Split('-');
                if (parts.Length == 2 && int.TryParse(parts[1], out lastNumber))
                {
                    lastNumber++;
                }
>>>>>>> dd28a8dd26355ac93475b3760a0023853d81994b
            }

            if (Session["ItemLines"] != null)
            {
                DataTable dt = Session["ItemLines"] as DataTable;
<<<<<<< HEAD

                foreach (string storeNo in storeList)
                {
                    string lastItemNo = GetLastItemNo(storeNo);
                    int lastNumber = 0;

                    if (!string.IsNullOrEmpty(lastItemNo))
                    {
                        string[] parts = lastItemNo.Split('-');
                        if (parts.Length == 2 && int.TryParse(parts[1], out lastNumber))
                        {
                            lastNumber++;
                        }
                    }

                    string newItemNo = $"{storeNo}-{lastNumber}";

                    foreach (DataRow row in dt.Rows)
                    {
                        string itemNo = row["ItemNo"].ToString();
                        string description = row["Description"].ToString();
                        string quantity = row["Qty"].ToString();
                        string expiryDate = row["ExpiryDate"].ToString();
                        string packingInfo = row["PackingInfo"].ToString();
                        string uom = row["UOM"].ToString();
                        string barcodeNo = row["BarcodeNo"].ToString();
                        string batchNo = row["BatchNo"].ToString();
                        string staffName = row["StaffName"].ToString();
                        string note = row["Note"].ToString();
                        string remark = row["Remark"].ToString();

                        string query = @"SELECT v.VendorNo, v.VendorName 
                                 FROM Vendors v 
                                 JOIN Items i ON v.vendorNo = i.VendorNo 
                                 WHERE i.ItemNo = @ItemNo";

                        using (SqlCommand cmdVendor = new SqlCommand(query))
                        {
                            cmdVendor.Parameters.AddWithValue("@ItemNo", itemNo);
                            DataTable vendorData = GetData(cmdVendor);

                            if (vendorData.Rows.Count > 0)
                            {
                                string vendorNo = vendorData.Rows[0]["VendorNo"].ToString();
                                string vendorName = vendorData.Rows[0]["VendorName"].ToString();

                                string insertQuery = @"
                            INSERT INTO ItemList 
                            (no, ItemNo, Description, BarcodeNo, Qty, UOM, PackingInfo, ExpiryDate, StoreNo, StaffName, BatchNo, VendorNo, VendorName, Note, Remark) 
                            VALUES 
                            (@no, @ItemNo, @Description, @BarcodeNo, @Qty, @UOM, @PackingInfo, @ExpiryDate, @storeNo, @StaffName, @BatchNo, @VendorNo, @VendorName, @Note, @Remark)";

                                using (SqlCommand cmd = new SqlCommand(insertQuery))
                                {
                                    cmd.Parameters.AddWithValue("@no", newItemNo);
                                    cmd.Parameters.AddWithValue("@ItemNo", itemNo);
                                    cmd.Parameters.AddWithValue("@Description", description);
                                    cmd.Parameters.AddWithValue("@BarcodeNo", barcodeNo);
                                    cmd.Parameters.AddWithValue("@Qty", quantity);
                                    cmd.Parameters.AddWithValue("@UOM", uom);
                                    cmd.Parameters.AddWithValue("@PackingInfo", packingInfo);
                                    cmd.Parameters.AddWithValue("@ExpiryDate", expiryDate);
                                    cmd.Parameters.AddWithValue("@storeNo", storeNo);
                                    cmd.Parameters.AddWithValue("@StaffName", staffName);
                                    cmd.Parameters.AddWithValue("@BatchNo", batchNo);
                                    cmd.Parameters.AddWithValue("@VendorNo", vendorNo);
                                    cmd.Parameters.AddWithValue("@VendorName", vendorName);
                                    cmd.Parameters.AddWithValue("@Note", note);
                                    cmd.Parameters.AddWithValue("@Remark", remark);

                                    ExecuteQuery(cmd);
                                }
                            }
                        }

                        lastNumber++;
                    }

                    no.Text = $"{storeNo}-{lastNumber}";
                }

                Session["ItemLines"] = null;
                BindGridView();

                ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
                    "swal('Success!', 'All items were added to the database for all stores!', 'success');", true);
=======
                string newItemNo = $"{store1}-{lastNumber}";

                foreach (DataRow row in dt.Rows)
                {
                    string itemNo = row["ItemNo"].ToString();
                    string description = row["Description"].ToString();
                    string quantity = row["Qty"].ToString();
                    string expiryDate = row["ExpiryDate"].ToString();
                    string packingInfo = row["PackingInfo"].ToString();
                    string uom = row["UOM"].ToString();
                    string barcodeNo = row["BarcodeNo"].ToString();
                    string batchNo = row["BatchNo"].ToString();
                    string store = row["StoreNo"].ToString();
                    string staffName = row["StaffName"].ToString();
                    string note = row["Note"].ToString();
                    string remark = row["Remark"].ToString();

                    string query = @"SELECT v.VendorNo, v.VendorName 
                             FROM Vendors v 
                             JOIN Items i ON v.id = i.VendorNo 
                             WHERE i.ItemNo = @ItemNo";
                    using (SqlCommand cmdVendor = new SqlCommand(query))
                    {
                        cmdVendor.Parameters.AddWithValue("@ItemNo", itemNo);
                        DataTable vendorData = GetData(cmdVendor);

                        if (vendorData.Rows.Count > 0)
                        {
                            string vendorNo = vendorData.Rows[0]["VendorNo"].ToString();
                            string vendorName = vendorData.Rows[0]["VendorName"].ToString();

                            // Insert the item into the ItemList table
                            string insertQuery = @"
                                INSERT INTO ItemList 
                                (no, ItemNo, Description, BarcodeNo, Qty, UOM, PackingInfo, ExpiryDate, StoreNo, StaffName, BatchNo, VendorNo, VendorName, Note, Remark) 
                                VALUES 
                                (@no, @ItemNo, @Description, @BarcodeNo, @Qty, @UOM, @PackingInfo, @ExpiryDate, @storeNo, @StaffName, @BatchNo, @VendorNo, @VendorName, @Note, @Remark)";

                            using (SqlCommand cmd = new SqlCommand(insertQuery))
                            {
                                // Insert the generated 'No' for all lines
                                cmd.Parameters.AddWithValue("@no", newItemNo);
                                cmd.Parameters.AddWithValue("@ItemNo", itemNo);
                                cmd.Parameters.AddWithValue("@Description", description);
                                cmd.Parameters.AddWithValue("@Qty", quantity);
                                cmd.Parameters.AddWithValue("@ExpiryDate", expiryDate);
                                cmd.Parameters.AddWithValue("@PackingInfo", packingInfo);
                                cmd.Parameters.AddWithValue("@UOM", uom);
                                cmd.Parameters.AddWithValue("@BarcodeNo", barcodeNo);
                                cmd.Parameters.AddWithValue("@BatchNo", batchNo);
                                cmd.Parameters.AddWithValue("@storeNo", store);
                                cmd.Parameters.AddWithValue("@StaffName", staffName);
                                cmd.Parameters.AddWithValue("@Note", note);
                                cmd.Parameters.AddWithValue("@VendorNo", vendorNo);
                                cmd.Parameters.AddWithValue("@VendorName", vendorName);
                                cmd.Parameters.AddWithValue("@Remark", remark);

                                ExecuteQuery(cmd);
                            }
                        }
                    }
                    lastNumber++;
                }
                Session["ItemLines"] = null;
                BindGridView();
                //clearForm();

                no.Text = $"{store1}-{lastNumber}";
                ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
                       "swal('Success!', 'All items were added to the database!', 'success');", true);
>>>>>>> dd28a8dd26355ac93475b3760a0023853d81994b
            }
            else
            {
                BindGridView();
                clearForm();
                ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
                    "swal('Error!', 'There are no items in the table to confirm!', 'error');", true);
            }
        }

        private DataTable GetData(SqlCommand cmd)
        {
            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(strcon))
            {
                using (SqlDataAdapter da = new SqlDataAdapter())
                {
                    cmd.Connection = conn;
                    da.SelectCommand = cmd;
                    da.Fill(dt);
                }
            }
            return dt;
        }

        private void ExecuteQuery(SqlCommand cmd)
        {
            using (SqlConnection conn = new SqlConnection(strcon))
            {
                cmd.Connection = conn;
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
<<<<<<< HEAD
}
=======
}
>>>>>>> dd28a8dd26355ac93475b3760a0023853d81994b
