using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using static Expiry_list.regeForm1;
using static Expiry_list.Training.scheduleList;
using Control = System.Web.UI.Control;

namespace Expiry_list.StoreDailyStatement
{
    public partial class dailyRegistration : System.Web.UI.Page
    {
        string strcon = ConfigurationManager.ConnectionStrings["con"].ConnectionString;
        int storeId = 0;
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
                string staff = Session["username"] as string;
                ViewState["StaffName"] = staff;

                var stores = GetStore();
                storeId = Convert.ToInt16(stores[0]);
                ViewState["storeId"] = storeId;
                no.Text = stores[1];
                name.Text = stores[2];
                tdyDate.Text = DateTime.Now.ToString("dd/MM/yyyy");
                List<PayType> PayType = GetPaymentTypes(storeId);
                showPayments(PayType);
            }
        }
        protected List<string> GetStore()
        {
            List<string> stores = GetLoggedInUserStoreNames();
            return stores;
        }

        protected string GetNextItemNo()
        {
            List<string> stores = GetLoggedInUserStoreNames();
            string storeName = stores[1] ?? "DEFAULT";
            int lastNumber = GetLastItemNumber(storeName);

            return $"{storeName}-{lastNumber + 1}";
        }

        private int GetLastItemNumber(string storeName)
        {
            string query = @"SELECT MAX(CAST(SUBSTRING(sdsId, CHARINDEX('-', sdsId) + 1, LEN(sdsId)) AS INT))
                     FROM dailyStmH
                     WHERE sdsId LIKE @pattern";

            using (SqlConnection conn = new SqlConnection(strcon))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                cmd.Parameters.AddWithValue("@pattern", storeName + "-%");
                object result = cmd.ExecuteScalar();
                return result == DBNull.Value ? 0 : Convert.ToInt32(result);
            }
        }

        private List<PayType> GetPaymentTypes(int storeId)
        {
            List<PayType> payTypes = new List<PayType>();

            using (SqlConnection conn = new SqlConnection(strcon))
            {
                string query = @"
                    select c.id, c.name from storeCollector stc join collector c on c.id= stc.collectorId
                    where stc.storeId = @storeId";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@storeId", storeId);
                conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        payTypes.Add(new PayType { Type = "Collector", Id = Convert.ToInt16(reader["id"]), Name = reader["name"].ToString() });
                    }
                }

                query = @"
                    select b.id, b.name from storeBankPos stb join bankPos b on b.id= stb.bankPosId
                    where stb.storeId = @storeId";

                cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@storeId", storeId);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        payTypes.Add(new PayType { Type = "Card", Id = Convert.ToInt16(reader["id"]), Name = reader["name"].ToString() });
                    }
                }

                query = @"
                    select m.id, m.name from storeMmqr smm join mmqr m on m.id= smm.storeMmqrId
                    where smm.storeId = @storeId";

                cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@storeId", storeId);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        payTypes.Add(new PayType { Type = "Mmqr", Id = Convert.ToInt16(reader["id"]), Name = reader["name"].ToString() });
                    }
                }
            }

            return payTypes;
        }

        private List<string> GetLoggedInUserStoreNames()
        {
            List<string> storeNos = Session["storeListRaw"] as List<string>;
            List<string> storeNames = new List<string>();

            if (storeNos == null || storeNos.Count == 0)
                return storeNames;

            string query = $"SELECT id,storeNo,storeName FROM Stores WHERE storeNo IN ({string.Join(",", storeNos.Select((s, i) => $"@store{i}"))})";

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
                        storeNames.Add(reader["id"].ToString());
                        storeNames.Add(reader["storeNo"].ToString());
                        storeNames.Add(reader["storeName"].ToString());
                    }
                }
            }
            return storeNames;
        }

        public class PayType
        {
            public string Type { get; set; }
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class PaymentModel
        {
            public decimal totalSalesAmt { get; set; }
            public decimal submitAmt { get; set; }
            public decimal advPayShweAmt { get; set; }
            public decimal advPayABankAmt { get; set; }
            public decimal advPayKbzAmt { get; set; }
            public decimal advPayUabAmt { get; set; }
            public decimal dailySalesShweAmt { get; set; }
            public decimal dailySalesABankAmt { get; set; }
            public decimal dailySalesKbzAmt { get; set; }
            public decimal dailySalesUabAmt { get; set; }
            public decimal pettyCash { get; set; }
            public decimal extraAmt { get; set; }
            public decimal mmqr1Amt { get; set; }
            public decimal mmqr2Amt { get; set; }
            public decimal mmqr3Amt { get; set; }
            public decimal mmqr4Amt { get; set; }
            public decimal payTotalAmt { get; set; }
            public decimal cardABankAmt { get; set; }
            public decimal cardAyaAmt { get; set; }
            public decimal cardUabAmt { get; set; }
            public decimal deliPayAmt { get; set; }
            public decimal deliCodAmt { get; set; }
            public decimal netAmt { get; set; }
        }
        private void showPayments(List<PayType> paytype)
        {
            var collectors = paytype.Where(x => x.Type == "Collector");
            if (collectors.Count() > 0)
            {
                div_advpay1.Style["display"] = (collectors.Where(x => x.Id == 4).Count() > 0 ? "flex" : "none");
                div_advpay2.Style["display"] = (collectors.Where(x => x.Id == 1).Count() > 0 ? "flex" : "none");
                div_advpay3.Style["display"] = (collectors.Where(x => x.Id == 2).Count() > 0 ? "flex" : "none");
                div_advpay4.Style["display"] = (collectors.Where(x => x.Id == 3).Count() > 0 ? "flex" : "none");
                div_dailysales1.Style["display"] = (collectors.Where(x => x.Id == 4).Count() > 0 ? "flex" : "none");
                div_dailysales2.Style["display"] = (collectors.Where(x => x.Id == 1).Count() > 0 ? "flex" : "none");
                div_dailysales3.Style["display"] = (collectors.Where(x => x.Id == 2).Count() > 0 ? "flex" : "none");
                div_dailysales4.Style["display"] = (collectors.Where(x => x.Id == 3).Count() > 0 ? "flex" : "none");
            }

            var mmqr = paytype.Where(x => x.Type == "Mmqr");
            if (mmqr.Count() > 0)
            {
                div_mmqr1.Style["display"] = (mmqr.Where(x => x.Id == 1).Count() > 0 ? "flex" : "none");
                div_mmqr2.Style["display"] = (mmqr.Where(x => x.Id == 2).Count() > 0 ? "flex" : "none");
                div_mmqr3.Style["display"] = (mmqr.Where(x => x.Id == 3).Count() > 0 ? "flex" : "none");
            }
            var cardPayment = paytype.Where(x => x.Type == "Card");
            if (cardPayment.Count() > 0)
            {
                div_cardpay1.Style["display"] = (cardPayment.Where(x => x.Id == 1).Count() > 0 ? "flex" : "none");
                div_cardpay2.Style["display"] = (cardPayment.Where(x => x.Id == 2).Count() > 0 ? "flex" : "none");
                div_cardpay3.Style["display"] = (cardPayment.Where(x => x.Id == 3).Count() > 0 ? "flex" : "none");
            }

        }

        private decimal ParseAmount(TextBox textBox)
        {
            string raw = textBox.Text.Trim();
            if (raw.StartsWith("(") && raw.EndsWith(")"))
                raw = "-" + raw.Trim('(', ')');

            return string.IsNullOrWhiteSpace(raw) ? 0 : Convert.ToDecimal(raw);
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            var payment = new PaymentModel();

            payment.totalSalesAmt = string.IsNullOrWhiteSpace(totalSalesAmt.Text) ? 0 : ParseAmount(totalSalesAmt);
            //payment.submitAmt = string.IsNullOrWhiteSpace(submitAmt.Text) ? 0 : Convert.ToDecimal(submitAmt.Text);            
            payment.advPayShweAmt = string.IsNullOrWhiteSpace(advPayShweAmt.Text) ? 0 : ParseAmount(advPayShweAmt);
            payment.advPayABankAmt = string.IsNullOrWhiteSpace(advPayABankAmt.Text) ? 0 : ParseAmount(advPayABankAmt);
            payment.advPayKbzAmt = string.IsNullOrWhiteSpace(advPayKbzAmt.Text) ? 0 : ParseAmount(advPayKbzAmt);
            payment.advPayUabAmt = string.IsNullOrWhiteSpace(advPayUabAmt.Text) ? 0 : ParseAmount(advPayUabAmt);
            payment.dailySalesShweAmt = string.IsNullOrWhiteSpace(dailySalesShweAmt.Text) ? 0 : ParseAmount(dailySalesShweAmt);
            payment.dailySalesABankAmt = string.IsNullOrWhiteSpace(dailySalesABankAmt.Text) ? 0 : ParseAmount(dailySalesABankAmt);
            payment.dailySalesKbzAmt = string.IsNullOrWhiteSpace(dailySalesKbzAmt.Text) ? 0 : ParseAmount(dailySalesKbzAmt);
            payment.dailySalesUabAmt = string.IsNullOrWhiteSpace(dailySalesUabAmt.Text) ? 0 : ParseAmount(dailySalesUabAmt);
            payment.pettyCash = string.IsNullOrWhiteSpace(pettyCash.Text) ? 0 : ParseAmount(pettyCash);
            payment.extraAmt = string.IsNullOrWhiteSpace(extraAmt.Text) ? 0 : ParseAmount(extraAmt);
            payment.mmqr1Amt = string.IsNullOrWhiteSpace(mmqr1Amt.Text) ? 0 : ParseAmount(mmqr1Amt);
            payment.mmqr2Amt = string.IsNullOrWhiteSpace(mmqr2Amt.Text) ? 0 : ParseAmount(mmqr2Amt);
            payment.mmqr3Amt = string.IsNullOrWhiteSpace(mmqr3Amt.Text) ? 0 : ParseAmount(mmqr3Amt);
            payment.mmqr4Amt = string.IsNullOrWhiteSpace(mmqr4Amt.Text) ? 0 : ParseAmount(mmqr4Amt);
            payment.payTotalAmt = string.IsNullOrWhiteSpace(payTotalAmt.Text) ? 0 : ParseAmount(payTotalAmt);
            payment.cardABankAmt = string.IsNullOrWhiteSpace(cardABankAmt.Text) ? 0 : ParseAmount(cardABankAmt);
            payment.cardAyaAmt = string.IsNullOrWhiteSpace(cardAyaAmt.Text) ? 0 : ParseAmount(cardAyaAmt);
            payment.cardUabAmt = string.IsNullOrWhiteSpace(cardUabAmt.Text) ? 0 : ParseAmount(cardUabAmt);
            payment.deliPayAmt = string.IsNullOrWhiteSpace(deliPayAmt.Text) ? 0 : ParseAmount(deliPayAmt);
            payment.deliCodAmt = string.IsNullOrWhiteSpace(deliCodAmt.Text) ? 0 : ParseAmount(deliCodAmt);
            payment.submitAmt = typeof(PaymentModel)
                            .GetProperties()
                            .Where(p => p.PropertyType == typeof(decimal) && p.Name != nameof(PaymentModel.submitAmt) && p.Name != nameof(PaymentModel.netAmt) && p.Name != nameof(PaymentModel.advPayShweAmt)
                                        && p.Name != nameof(PaymentModel.advPayABankAmt) && p.Name != nameof(PaymentModel.advPayKbzAmt) && p.Name != nameof(PaymentModel.advPayUabAmt)
                                        && p.Name != nameof(PaymentModel.deliPayAmt) && p.Name != nameof(PaymentModel.deliCodAmt))
                            .Sum(p => (decimal)p.GetValue(payment));
            payment.netAmt = typeof(PaymentModel)
                            .GetProperties()
                            .Where(p => p.PropertyType == typeof(decimal) && p.Name != nameof(PaymentModel.submitAmt) && p.Name != nameof(PaymentModel.netAmt) && p.Name != nameof(PaymentModel.advPayShweAmt)
                                        && p.Name != nameof(PaymentModel.advPayABankAmt) && p.Name != nameof(PaymentModel.advPayKbzAmt) && p.Name != nameof(PaymentModel.advPayUabAmt)
                                        && p.Name != nameof(PaymentModel.deliPayAmt) && p.Name != nameof(PaymentModel.deliCodAmt))
                            .Sum(p => (decimal)p.GetValue(payment));

            try
            {
                var sdsId = GetNextItemNo();

                using (SqlConnection con = new SqlConnection(strcon))
                {
                    string query = "INSERT INTO dailyStmH VALUES (@sdsId, @storeId, @storeNo, @totalSales, @totalSubmit, @createdDate)";
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@sdsId", sdsId);
                        cmd.Parameters.AddWithValue("@storeId", ViewState["storeId"]);
                        cmd.Parameters.AddWithValue("@storeNo", no.Text);
                        cmd.Parameters.AddWithValue("@totalSales", payment.totalSalesAmt);
                        cmd.Parameters.AddWithValue("@totalSubmit", payment.submitAmt);
                        cmd.Parameters.AddWithValue("@createdDate", DateTime.Now);
                        cmd.ExecuteNonQuery();
                    }

                    query = "INSERT INTO dailyStmD(sdsId,advPayShweAmt,advPayABankAmt,advPayKbzAmt,advPayUabAmt,dailySalesShweAmt,dailySalesABankAmt,dailySalesKbzAmt,dailySalesUabAmt,pettyCash,extraAmt," +
                        "mmqr1Amt,mmqr2Amt,mmqr3Amt,mmqr4Amt,payKpayTolAmt,cardABankAmt,cardAyaAmt,cardUabAmt,flashDeliPayAmt,flashDeliCodAmt,checkedUserId)" +
                        " VALUES (@sdsId,@advPayShweAmt,@advPayABankAmt,@advPayKbzAmt,@advPayUabAmt,@dailySalesShweAmt,@dailySalesABankAmt,@dailySalesKbzAmt,@dailySalesUabAmt,@pettyCash,@extraAmt," +
                        "@mmqr1Amt,@mmqr2Amt,@mmqr3Amt,@mmqr4Amt,@payKpayTolAmt,@cardABankAmt,@cardAyaAmt,@cardUabAmt,@flashDeliPayAmt,@flashDeliCodAmt,@checkedUserId)";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@sdsId", sdsId);
                        cmd.Parameters.AddWithValue("@advPayShweAmt", payment.advPayShweAmt);
                        cmd.Parameters.AddWithValue("@advPayABankAmt", payment.advPayABankAmt);
                        cmd.Parameters.AddWithValue("@advPayKbzAmt", payment.advPayKbzAmt);
                        cmd.Parameters.AddWithValue("@advPayUabAmt", payment.advPayUabAmt);
                        cmd.Parameters.AddWithValue("@dailySalesShweAmt", payment.dailySalesShweAmt);
                        cmd.Parameters.AddWithValue("@dailySalesABankAmt", payment.dailySalesABankAmt);
                        cmd.Parameters.AddWithValue("@dailySalesKbzAmt", payment.dailySalesKbzAmt);
                        cmd.Parameters.AddWithValue("@dailySalesUabAmt", payment.dailySalesUabAmt);
                        cmd.Parameters.AddWithValue("@pettyCash", payment.pettyCash);
                        cmd.Parameters.AddWithValue("@extraAmt", payment.extraAmt);
                        cmd.Parameters.AddWithValue("@mmqr1Amt", payment.mmqr1Amt);
                        cmd.Parameters.AddWithValue("@mmqr2Amt", payment.mmqr2Amt);
                        cmd.Parameters.AddWithValue("@mmqr3Amt", payment.mmqr3Amt);
                        cmd.Parameters.AddWithValue("@mmqr4Amt", payment.mmqr4Amt);
                        cmd.Parameters.AddWithValue("@payKpayTolAmt", payment.payTotalAmt);
                        cmd.Parameters.AddWithValue("@cardABankAmt", payment.cardABankAmt);
                        cmd.Parameters.AddWithValue("@cardAyaAmt", payment.cardAyaAmt);
                        cmd.Parameters.AddWithValue("@cardUabAmt", payment.cardUabAmt);
                        cmd.Parameters.AddWithValue("@flashDeliPayAmt", payment.deliPayAmt);
                        cmd.Parameters.AddWithValue("@flashDeliCodAmt", payment.deliCodAmt);
                        cmd.Parameters.AddWithValue("@checkedUserId", Session["id"]);
                        cmd.ExecuteNonQuery();
                    }
                }

                ShowMessage("Store Daliy Statement added successfully!", "success");
                ClearAllTextBoxes(div_invAmt);
                ClearAllTextBoxes(UpdatePanel1);
            }
            catch (Exception ex)
            {
                ShowMessage("Error saving statement: " + ex.Message, "error");
            }
        }

        private void ShowMessage(string message, string type)
        {
            string safeMessage = HttpUtility.JavaScriptStringEncode(message);
            string script = $"swal('{type.ToUpper()}', '{safeMessage}', '{type}');";

            ScriptManager.RegisterStartupScript(this, GetType(), "showMessage", script, true);
        }

        private void ClearAllTextBoxes(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is TextBox txt)
                {
                    txt.Text = string.Empty;
                }
                else if (c.HasControls())
                {
                    ClearAllTextBoxes(c);
                }
            }
        }

    }

}