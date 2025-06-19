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
using static Expiry_list.ConsignItem.rege1;

namespace Expiry_list.ReorderForm
{
    public partial class rege1 : System.Web.UI.Page
    {
        string strcon = ConfigurationManager.ConnectionStrings["con"].ConnectionString;

        private string storeName;

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
                storeName = string.Join(",", GetLoggedInUserStoreNames());

                string staff = Session["username"] as string;
                ViewState["StaffName"] = staff;
                staffName.Text = staff;
                hiddenStaffName.Value = staff;

                no.Text = GetNextItemNo();
                storeNo.Text = storeName;
                tdyDate.Text = DateTime.Now.ToString("dd/MM/yyyy");

                //DisplaySessionData();
                BindGridView();

            }
            else
            {
                //no.Text = GetNextItemNo();
                staffName.Text = ViewState["StaffName"] as string;
                hiddenStaffName.Value = ViewState["StaffName"] as string;

                if (!string.IsNullOrEmpty(hiddenUOM.Value))
                {
                    uom.SelectedValue = hiddenUOM.Value;
                }
            }
        }

        protected void DisplaySessionData()
        {
            if (Session["username"] != null)
            {
                string sessionInfo = $"Username: {Session["username"]}<br/>";
                sessionInfo += $"Reorder: {(Session["Reorder"] != null ? "Exists" : "Does not exist")}<br/>";
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
                     FROM ItemListR
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
                          SELECT i.ItemNo, i.description, i.packingInfo, b.barcodeNo, 
                                    i.divisionCode, i.vendorNo, v.vendorName
                             FROM Items i
                             LEFT JOIN ItemBarcode b ON i.ItemNo = b.ItemNo
                             LEFT JOIN Vendors v ON REPLACE(REPLACE(i.vendorNo, CHAR(13), ''), CHAR(10), '') = v.vendorNo
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
                                var divisionCode = reader["divisionCode"]?.ToString();
                                var vendorNo = reader["vendorNo"]?.ToString();
                                var vendorName = reader["vendorName"]?.ToString();

                                if (!items.TryGetValue(itemNo, out var item))
                                {
                                    item = new Item
                                    {
                                        ItemNo = itemNo,
                                        ItemDescription = reader["description"].ToString(),
                                        PackingInfo = reader["packingInfo"].ToString(),
                                        Barcode = new List<string>(),
                                        UOMList = new List<string>(),
                                        DivisionCode = divisionCode,
                                        VendorNo = vendorNo,
                                        VendorName = vendorName
                                    };
                                    items.Add(itemNo, item);
                                }

                                if (!string.IsNullOrEmpty(barcode))
                                    item.Barcode.Add(barcode);
                            }
                        }
                    }

                    if (items.Count > 0)
                    {
                        using (var createCmd = new SqlCommand(
                            "CREATE TABLE #TempItems (ItemNo NVARCHAR(50) COLLATE DATABASE_DEFAULT)",
                            con))
                        {
                            createCmd.ExecuteNonQuery();
                        }

                        const int bulkSize = 1000;
                        var itemKeys = items.Keys.ToList();

                        for (int i = 0; i < itemKeys.Count; i += bulkSize)
                        {
                            var chunk = itemKeys.Skip(i).Take(bulkSize);
                            string insertQuery = "INSERT INTO #TempItems (ItemNo) VALUES " +
                                                 string.Join(",", chunk.Select((_, idx) => $"(@item{i + idx})"));

                            using (var insertCmd = new SqlCommand(insertQuery, con))
                            {
                                for (int j = 0; j < chunk.Count(); j++)
                                {
                                    insertCmd.Parameters.AddWithValue($"@item{i + j}", chunk.ElementAt(j));
                                }
                                insertCmd.ExecuteNonQuery();
                            }
                        }

                        using (var uomCmd = new SqlCommand(
                            @"SELECT iu.ItemNo, iu.uoms 
                              FROM ItemUOMs iu
                              INNER JOIN #TempItems t 
                              ON iu.ItemNo COLLATE DATABASE_DEFAULT = t.ItemNo",
                            con))

                        using (var reader = uomCmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string itemNo = reader["ItemNo"].ToString();
                                string uom = reader["uoms"].ToString();

                                if (items.TryGetValue(itemNo, out var item))
                                {
                                    item.UOMList.Add(uom);
                                }
                            }
                        }
                    }
                }

                return items.Values.ToList();
            }
            catch (Exception ex)
            {
                // Add logging here
                throw new Exception("An error occurred while fetching items: " + ex.Message);
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static Item GetItemDetails(string itemNo)
        {
            return GetItems(itemNo).FirstOrDefault();
        }


        public class Vendor
        {
            public string VendorID { get; set; }
            public string VendorName { get; set; }
        }

        public class Item
        {
            public int Id { get; set; }
            public string No { get; set; }
            public string ItemNo { get; set; }
            public string ItemDescription { get; set; }
            public List<string> Barcode { get; set; }
            public List<string> UOMList { get; set; }
            public string PackingInfo { get; set; }
            public int Qty { get; set; }
            public int StoreNo { get; set; }
            public string StaffName { get; set; }
            public string VendorNo { get; set; }
            public string VendorName { get; set; }
            public DateTime RegeDate { get; set; }
            public int Action { get; set; }
            public string Status { get; set; }
            public string Note { get; set; }
            public string DivisionCode { get; set; }
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
                                //uom.Text = reader["uom"].ToString();
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

            DataTable dt = Session["Reorder"] as DataTable;
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
                dt.Columns.Add("Remark", typeof(string));
                dt.Columns.Add("DivisionCode", typeof(string));
                dt.Columns.Add("VendorNo", typeof(string));
                dt.Columns.Add("VendorName", typeof(string));
            }

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
                newRow["StoreNo"] = string.Join(",", GetLoggedInUserStoreNames());
                newRow["StaffName"] = staffName.Text;
                newRow["registrationDate"] = DateTime.Now;
                newRow["Note"] = note.Text;
                newRow["Remark"] = null;
                newRow["DivisionCode"] = hiddenDivisionCode.Value;
                newRow["VendorNo"] = hiddenVendorNo.Value;
                newRow["VendorName"] = hiddenVendorName.Value;

                dt.Rows.Add(newRow);
                Session["Reorder"] = dt;
                Debug.WriteLine($"Adding to DataTable - VendorNo: {hiddenVendorNo.Value}, VendorName: {hiddenVendorName.Value}");
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
                DataTable dt = Session["Reorder"] as DataTable;

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
                    dt.Columns.Add("Remark", typeof(string));
                    dt.Columns.Add("CompletedDate", typeof(DateTime));
                    dt.Columns.Add("registrationDate", typeof(DateTime));
                    dt.Columns.Add("DivisionCode", typeof(string));
                    dt.Columns.Add("VendorNo", typeof(string));
                    dt.Columns.Add("VendorName", typeof(string));

                    Session["Reorder"] = dt;
                }

                // Filter rows by current staff name
                DataView dv = new DataView(dt)
                {
                    RowFilter = $"StaffName = '{currentStaffName}'",
                    Sort = "registrationDate DESC"
                };

                Session["SortedReorder"] = dv;
                gridTable.DataSource = dv;
                gridTable.DataBind();
                foreach (DataRow row in dt.Rows)
                {
                    System.Diagnostics.Debug.WriteLine("Row UOM: " + row["UOM"].ToString());
                }

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
            hiddenDivisionCode.Value = string.Empty;
            hiddenVendorNo.Value = string.Empty;
            hiddenVendorName.Value = string.Empty;
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
            DataView dv = Session["SortedReorder"] as DataView; 
            DataTable dt = dv.ToTable();

            if (dt != null)
            {
                DataRowView rowView = dv[e.RowIndex];
                DataRow row = rowView.Row;

                TextBox txtQty = (TextBox)gridTable.Rows[e.RowIndex].FindControl("txtQuantity");
                TextBox txtNote = (TextBox)gridTable.Rows[e.RowIndex].FindControl("txtNote");
                DropDownList ddlUoms = (DropDownList)gridTable.Rows[e.RowIndex].FindControl("ddlUom");

                int qtyValue;
                bool isValidQty = int.TryParse(txtQty.Text, out qtyValue);

                if (isValidQty)
                {
                    row["Qty"] = qtyValue;
                    row["Note"] = txtNote.Text.Trim();
                    row["UOM"] = ddlUoms.SelectedValue;

                    DataTable originalDt = Session["Reorder"] as DataTable;
                    originalDt.Rows[row.Table.Rows.IndexOf(row)] 
                        .ItemArray = row.ItemArray;

                    Session["Reorder"] = originalDt;
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
            DataView dv = Session["SortedReorder"] as DataView; 
            DataTable dt = Session["Reorder"] as DataTable;  

            if (dv != null && dt != null && e.RowIndex >= 0 && e.RowIndex < dv.Count)
            {
                DataRowView rowView = dv[e.RowIndex];

                DateTime registrationDate = (DateTime)rowView["registrationDate"];

                DataRow rowToDelete = dt.Rows.Cast<DataRow>()
                    .FirstOrDefault(r => (DateTime)r["registrationDate"] == registrationDate);

                if (rowToDelete != null)
                {
                    dt.Rows.Remove(rowToDelete);
                    Session["Reorder"] = dt;
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

        protected void gridTable_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow && gridTable.EditIndex == e.Row.RowIndex)
            {
                DropDownList ddlUOM = (DropDownList)e.Row.FindControl("ddlUom");
                Label lblItemNo = (Label)e.Row.FindControl("lblItemNo");

                string itemNo = DataBinder.Eval(e.Row.DataItem, "ItemNo").ToString();

                if (ddlUOM != null && !string.IsNullOrEmpty(itemNo))
                {
                    List<string> uomList = GetUOMsByItemNo(itemNo);

                    ddlUOM.DataSource = uomList;
                    ddlUOM.DataBind();

                    string currentUOM = DataBinder.Eval(e.Row.DataItem, "uom").ToString();
                    if (ddlUOM.Items.FindByValue(currentUOM) != null)
                    {
                        ddlUOM.SelectedValue = currentUOM;
                    }
                }
            }
        }

        private List<string> GetUOMsByItemNo(string itemNo)
        {
            List<string> uoms = new List<string>();
            string connectionString = ConfigurationManager.ConnectionStrings["con"].ConnectionString;

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT uoms FROM ItemUOMs WHERE ItemNo = @ItemNo", con))
                {
                    cmd.Parameters.AddWithValue("@ItemNo", itemNo);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            uoms.Add(reader["uoms"].ToString());
                        }
                    }
                }
            }

            return uoms;
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

            if (Session["Reorder"] == null)
            {
                BindGridView();
                clearForm();
                ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
                    "swal('Error!', 'There are no items in the table to confirm!', 'error');", true);
                return;
            }

            DataTable dt = Session["Reorder"] as DataTable;
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
                    string lastGeneratedNo = string.Empty;

                    foreach (string store in storeList)
                    {
                        string batchNo = GetNextItemNo();

                        foreach (DataRow row in dt.Rows)
                        {
                            //string vendorNo = DBNull.Value.ToString();
                            //string vendorName = DBNull.Value.ToString();

                            //GetVendorInfo(row["ItemNo"].ToString(), conn, transaction, out vendorNo, out vendorName);
                            //Debug.WriteLine($"Inserting - Item: {desc.Text}, VendorNo: {vendorNo ?? "null"}, VendorName: {vendorName ?? "null"}");

                            string itemNo = row["ItemNo"].ToString();
                            string description = row["Description"].ToString();
                            string quantity = row["Qty"].ToString();
                            string packingInfo = row["PackingInfo"].ToString();
                            string uom = row["UOM"].ToString();
                            string barcodeNo = row["BarcodeNo"].ToString();
                            string staffName = row["StaffName"].ToString();
                            string division = row["DivisionCode"].ToString();
                            string note = row["Note"].ToString();
                            string vendorNo = row["VendorNo"].ToString();
                            string vendorName = row["VendorName"].ToString();
                            string remark = row.Table.Columns.Contains("Remark") ? row["Remark"].ToString() : string.Empty;

                            Debug.WriteLine($"Inserting to DB - VendorNo: {vendorNo}, VendorName: {vendorName}");

                            string insertQuery = @"
                                INSERT INTO ItemListR 
                                (no, ItemNo, Description, BarcodeNo, Qty, UOM, PackingInfo, StoreNo, StaffName, vendorNo, vendorName, divisionCode, regeDate, Note, Remark) 
                                VALUES 
                                (@no, @ItemNo, @Description, @BarcodeNo, @Qty, @UOM, @PackingInfo, @storeNo, @StaffName, @VendorNo, @VendorName, @DivisionCode, @rege, @Note, @Remark)";

                            using (SqlCommand cmd = new SqlCommand(insertQuery, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@no", batchNo);
                                cmd.Parameters.AddWithValue("@ItemNo", itemNo);
                                cmd.Parameters.AddWithValue("@Description", description);
                                cmd.Parameters.AddWithValue("@Qty", quantity);
                                cmd.Parameters.AddWithValue("@PackingInfo", packingInfo);
                                cmd.Parameters.AddWithValue("@UOM", uom);
                                cmd.Parameters.AddWithValue("@BarcodeNo", barcodeNo);
                                cmd.Parameters.AddWithValue("@storeNo", store);
                                cmd.Parameters.AddWithValue("@StaffName", staffName);
                                cmd.Parameters.AddWithValue("@Note", note);
                                cmd.Parameters.AddWithValue("@VendorNo", string.IsNullOrEmpty(vendorNo) ? (object)DBNull.Value : vendorNo);
                                cmd.Parameters.AddWithValue("@VendorName", string.IsNullOrEmpty(vendorName) ? (object)DBNull.Value : vendorName);
                                cmd.Parameters.AddWithValue("@DivisionCode", division);
                                cmd.Parameters.AddWithValue("@rege", DateTime.Now.Date);
                                cmd.Parameters.AddWithValue("@Remark", string.IsNullOrEmpty(remark) ? (object)DBNull.Value : remark);

                                cmd.ExecuteNonQuery();
                            }
                        }
                    }

                    transaction.Commit();
                    Session["Reorder"] = null;
                    BindGridView();

                    no.Text = GetNextItemNo();

                    ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
                        "swal('Success!', 'All items added successfully!', 'success');", true);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    string safeMessage = ex.Message.Replace("'", "\\'").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", " ");
                    ClientScript.RegisterClientScriptBlock(this.GetType(), "alert",
                        $"swal('Error!', '{safeMessage}', 'error');", true);
                }
            }
        }

        private void GetVendorInfo(string itemNo, SqlConnection conn, SqlTransaction transaction, out string vendorNo, out string vendorName)
        {
            vendorNo = null;
            vendorName = null;

            // Modified query with explicit column references and proper trimming
            string query = @"SELECT TOP 1 LTRIM(RTRIM(v.VendorNo)) AS VendorNo, 
                            LTRIM(RTRIM(v.VendorName)) AS VendorName 
                     FROM Vendors v 
                     INNER JOIN Items i ON LTRIM(RTRIM(v.VendorNo)) = LTRIM(RTRIM(i.vendorNo))
                     WHERE i.ItemNo = @ItemNo";

            try
            {
                using (SqlCommand cmd = new SqlCommand(query, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@ItemNo", itemNo.Trim());
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            vendorNo = reader["VendorNo"] is DBNull ? null : reader["VendorNo"].ToString();
                            vendorName = reader["VendorName"] is DBNull ? null : reader["VendorName"].ToString();

                            // Debug output to verify we're getting vendor data
                            Debug.WriteLine($"Vendor found for {itemNo}: {vendorNo} - {vendorName}");
                        }
                        else
                        {
                            Debug.WriteLine($"No vendor found for item: {itemNo}");
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