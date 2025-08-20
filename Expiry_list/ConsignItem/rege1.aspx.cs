using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Script.Services;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Diagnostics;
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
                clearForm();

                string staff = Session["username"] as string;
                ViewState["StaffName"] = staff;
                staffName.Text = staff;
                hiddenStaffName.Value = staff;

                no.Text = GetNextItemNo();
                storeNo.Text = string.Join(",", GetLoggedInUserStoreNames());
                tdyDate.Text = DateTime.Now.ToString("dd/MM/yyyy");

                BindGridView();
            }
            else
            {
                staffName.Text = ViewState["StaffName"] as string;
                hiddenStaffName.Value = ViewState["StaffName"] as string;

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
                       WHERE VendorName LIKE @SearchTerm OR VendorNo LIKE @SearchTerm", con))
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

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<ItemDTO> GetItems(GetItemsRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.vendorNo))
                {
                    return new List<ItemDTO>();
                }

                var items = new Dictionary<string, ItemDTO>();
                string connectionString = ConfigurationManager.ConnectionStrings["con"].ConnectionString;

                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    using (var cmd = new SqlCommand(@"
                    SELECT i.ItemNo, i.description, i.uom, i.packingInfo, b.barcodeNo 
                    FROM Items i
                    LEFT JOIN ItemBarcode b ON i.ItemNo = b.ItemNo
                    WHERE i.vendorNo = @vendorNo AND 
                        (@SearchTerm = '' OR 
                        i.ItemNo LIKE @SearchTerm OR 
                        i.description LIKE @SearchTerm OR 
                        b.barcodeNo LIKE @SearchTerm)", con))
                    {
                        cmd.Parameters.AddWithValue("@vendorNo", request.vendorNo);
                        cmd.Parameters.AddWithValue("@SearchTerm", "%" + (request.searchTerm ?? "") + "%");

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

                return items.Values.ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"GetItems error: {ex}");
                return new List<ItemDTO> {
                    new ItemDTO {
                        ItemNo = "ERROR",
                        ItemDescription = "An error occurred: " + ex.Message
                    }
                };
            }
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
                dt.Columns.Add("VendorNo", typeof(string)); // Add this
                dt.Columns.Add("VendorName", typeof(string)); // Add this
            }
            else
            {
                // Check if VendorNo column exists, add if not
                if (!dt.Columns.Contains("VendorNo"))
                {
                    dt.Columns.Add("VendorNo", typeof(string));
                }

                // Check if VendorName column exists, add if not
                if (!dt.Columns.Contains("VendorName"))
                {
                    dt.Columns.Add("VendorName", typeof(string));
                }
            }

            // Check if item already exists in the DataTable
            bool itemExists = dt.AsEnumerable().Any(row =>
                row["ItemNo"].ToString() == itemNo.SelectedValue);

            if (!itemExists)
            {
                DataRow newRow = dt.NewRow();

                newRow["No"] = newItemNo;
                newRow["ItemNo"] = hiddenSelectedItem.Value;
                newRow["Description"] = hiddenDescription.Value;
                newRow["Qty"] = int.Parse(qty.Text);
                newRow["PackingInfo"] = hiddenPackingInfo.Value;
                newRow["UOM"] = hiddenUOM.Value;
                newRow["BarcodeNo"] = hiddenBarcodeNo.Value;
                newRow["StoreNo"] = GetLoggedInUserStoreNames();
                newRow["StaffName"] = staffName.Text;
                newRow["registrationDate"] = DateTime.Now;
                newRow["Note"] = note.Text;
                newRow["VendorNo"] = vendorNo.SelectedValue; // Add vendor info
                newRow["VendorName"] = vendorNo.SelectedItem?.Text ?? "";

                dt.Rows.Add(newRow);
                Session["Consign"] = dt;

                BindGridView();
                clearForm();

                hiddenVendorNo.Value = vendorNo.SelectedValue;
                selectedVendorText = vendorNo.SelectedItem?.Text ??
                                    (vendorNo.Items.FindByValue(vendorNo.SelectedValue)?.Text ?? "");
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
                dv.Sort = "registrationDate DESC";

                Session["SortedConsign"] = dv;

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
            qty.Text = string.Empty;
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
            DataView dv = Session["SortedConsign"] as DataView;
            DataTable dt = dv.ToTable();

            if (dt != null)
            {
                DataRowView rowView = dv[e.RowIndex];
                DataRow row = rowView.Row;

                TextBox txtQty = (TextBox)gridTable.Rows[e.RowIndex].FindControl("txtQuantity");
                TextBox txtNote = (TextBox)gridTable.Rows[e.RowIndex].FindControl("txtNote");

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

        protected void btnConfirmAll_Click1(object sender, EventArgs e)
        {
            bool hasUnsavedValues =
                !string.IsNullOrEmpty(hiddenSelectedItem.Value) ||
                !string.IsNullOrEmpty(desc.Text) ||
                !string.IsNullOrEmpty(barcodeNo.Text) ||
                !string.IsNullOrEmpty(uom.Text) ||
                !string.IsNullOrEmpty(packingInfo.Text) ||
                !string.IsNullOrEmpty(qty.Text);

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

            if (Session["Consign"] == null)
            {
                BindGridView();
                clearForm();
                ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
                    "swal('Error!', 'There are no items in the table to confirm!', 'error');", true);
                return;
            }

            DataTable dt = Session["Consign"] as DataTable;
            if (dt.Rows.Count == 0)
            {
                ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
                    "swal('Error!', 'There are no items in the GridView! Please add items first.', 'error');", true);
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

                        foreach (DataRow row in dt.Rows)
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

            // Clean up and reset form UI
            Session["Consign"] = null;
            BindGridView();
            no.Text = GetNextItemNo();
            clearForm();

            vendorNo.Items.Clear();
            vendorNo.Items.Add(new ListItem("", ""));
            vendorNo.SelectedValue = "";
            hiddenVendorNo.Value = "";
            selectedVendorText = "";

            ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
                "swal('Success!', 'All items added successfully!', 'success');", true);

            //ScriptManager.RegisterStartupScript(this, this.GetType(), "Reload", "setTimeout(function(){ window.location = window.location.href; }, 500);", true);
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

    }
}