using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Expiry_list
{
    public partial class regeForm : System.Web.UI.Page
    {
        string strcon = ConfigurationManager.ConnectionStrings["con"].ConnectionString;
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                BindStores();
                BindGrid();
            }
        }

        private void BindStores()
        {
            using (SqlConnection con = new SqlConnection(strcon))
            {
                string query = "SELECT id, storeNo FROM stores ORDER BY storeNo";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        storeTextBox.DataSource = reader;
                        storeTextBox.DataTextField = "storeNo";
                        storeTextBox.DataValueField = "id";
                        storeTextBox.DataBind();
                    }
                }
            }

            storeTextBox.Items.Insert(0, new ListItem("", ""));
        }

        protected void btnRegister_Click(object sender, EventArgs e)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(strcon))
                {
                    con.Open();
                    string query = "INSERT INTO users (username, password, role, storeNo) " +
                                   "VALUES (@username, @password, @role, @storeNo)";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        string role = roleTextBox.SelectedValue.ToLower();
                        cmd.Parameters.AddWithValue("@username", usernameTextBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@password", passwordTextBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@role", role);
                        cmd.Parameters.AddWithValue("@storeNo", storeTextBox.SelectedValue.ToLower());

                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            ScriptManager.RegisterStartupScript(this, this.GetType(), "alert",
                                "swal('Success!', 'User is registered successfully!', 'success');", true);

                            clearForm();
                            BindGrid();
                            UpdatePanel1.Update();
                        }
                        else
                        {
                            ScriptManager.RegisterStartupScript(this, this.GetType(), "alert",
                                "swal('Error!', 'Registration failed!', 'error');", true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Response.Write("<script>alert('An error occurred: " + ex.Message + "');</script>");
                clearForm();
            }
        }

        protected void userGridView_RowEditing(object sender, GridViewEditEventArgs e)
        {
            userGridView.EditIndex = e.NewEditIndex;
            BindGrid();
        }

        protected void userGridView_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            userGridView.EditIndex = -1;
            BindGrid();
        }

        protected void userGridView_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            try
            {
                GridViewRow row = userGridView.Rows[e.RowIndex];

                string id = userGridView.DataKeys[e.RowIndex].Value.ToString();
                string username = ((TextBox)row.FindControl("txtUsername")).Text.Trim();
                string passwordInput = ((TextBox)row.FindControl("txtPassword")).Text.Trim();
                string role = ((DropDownList)row.FindControl("ddlEditRole")).SelectedValue;
                string storeNo = ((DropDownList)row.FindControl("ddlEditStore")).SelectedValue;
                bool isEnabled = ((CheckBox)row.FindControl("chkEditIsEnabled")).Checked;

                string passwordToUpdate = passwordInput;

                if (string.IsNullOrEmpty(passwordInput))
                {
                    using (SqlConnection con = new SqlConnection(strcon))
                    {
                        con.Open();
                        string selectQuery = "SELECT password FROM users WHERE id = @id";
                        using (SqlCommand selectCmd = new SqlCommand(selectQuery, con))
                        {
                            selectCmd.Parameters.AddWithValue("@id", id);
                            object result = selectCmd.ExecuteScalar();
                            if (result != null)
                            {
                                passwordToUpdate = result.ToString();
                            }
                            else
                            {
                                ClientScript.RegisterClientScriptBlock(this.GetType(), "alert", "swal('Error!', 'User not found.', 'error');", true);
                                return;
                            }
                        }
                    }
                }

                using (SqlConnection con = new SqlConnection(strcon))
                {
                    con.Open();
                    string updateQuery = "UPDATE users SET username = @username, password = @password, role = @role, storeNo = @storeNo, IsEnabled = @isEnabled WHERE id = @id";
                    using (SqlCommand updateCmd = new SqlCommand(updateQuery, con))
                    {
                        updateCmd.Parameters.AddWithValue("@id", id);
                        updateCmd.Parameters.AddWithValue("@username", username);
                        updateCmd.Parameters.AddWithValue("@password", passwordToUpdate);
                        updateCmd.Parameters.AddWithValue("@role", role);
                        updateCmd.Parameters.AddWithValue("@storeNo", storeNo);
                        updateCmd.Parameters.AddWithValue("@isEnabled", isEnabled);

                        updateCmd.ExecuteNonQuery();
                    }
                }

                userGridView.EditIndex = -1;
                BindGrid();
            }
            catch (Exception ex)
            {
                ClientScript.RegisterClientScriptBlock(this.GetType(), "alert", "swal('Error!', 'An unexpected error occurred during update. Please try again.', 'error');", true);
            }
        }

        protected void userGridView_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            string id = userGridView.DataKeys[e.RowIndex].Value.ToString();

            string query = "DELETE FROM Users WHERE id = @id";

            using (SqlConnection con = new SqlConnection(strcon))
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@id", id);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            BindGrid();
        }

        protected void userGridView_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow && userGridView.EditIndex == e.Row.RowIndex)
            {
                DropDownList ddlStore = (DropDownList)e.Row.FindControl("ddlEditStore");
                if (ddlStore != null)
                {
                    BindStoreDropDown(ddlStore);

                    // Get the current store name from the row
                    string currentStoreName = DataBinder.Eval(e.Row.DataItem, "StoreName")?.ToString();

                    // Set the selected value
                    if (!string.IsNullOrEmpty(currentStoreName))
                    {
                        ListItem selectedItem = ddlStore.Items.FindByText(currentStoreName);
                        if (selectedItem != null)
                        {
                            ddlStore.ClearSelection();
                            selectedItem.Selected = true;
                        }
                    }
                }
            }
        }

        protected void userGridView_DataBound(object sender, EventArgs e)
        {
            if (userGridView.PageCount > 1 && userGridView.BottomPagerRow != null)
            {
                LinkButton lnkPrev = (LinkButton)userGridView.BottomPagerRow.FindControl("lnkPrev");
                LinkButton lnkNext = (LinkButton)userGridView.BottomPagerRow.FindControl("lnkNext");

                if (lnkPrev != null)
                {
                    if (userGridView.PageIndex == 0)
                    {
                        lnkPrev.Enabled = false;
                        lnkPrev.CssClass = "btn btn-secondary m-1 disabled";
                    }
                    else
                    {
                        lnkPrev.Enabled = true;
                        lnkPrev.CssClass = "btn btn-secondary m-1";
                    }
                }

                if (lnkNext != null)
                {
                    if (userGridView.PageIndex == userGridView.PageCount - 1)
                    {
                        lnkNext.Enabled = false;
                        lnkNext.CssClass = "btn btn-secondary m-1 disabled";
                    }
                    else
                    {
                        lnkNext.Enabled = true;
                        lnkNext.CssClass = "btn btn-secondary m-1";
                    }
                }

                Label lblShowing = (Label)userGridView.BottomPagerRow.FindControl("lblShowing");
                if (lblShowing != null)
                {
                    int totalRows = GetTotalUsersCount();
                    int currentPage = userGridView.PageIndex;
                    int pageSize = userGridView.PageSize;
                    int startIndex = currentPage * pageSize + 1;
                    int endIndex = Math.Min((currentPage + 1) * pageSize, totalRows);

                    lblShowing.Text = $"Showing {startIndex} to {endIndex} of {totalRows} entries";
                }

            }
        }

        private int GetTotalUsersCount()
        {
            int total = 0;
            using (SqlConnection con = new SqlConnection(strcon))
            {
                string query = "SELECT COUNT(*) FROM Users";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    total = (int)cmd.ExecuteScalar();
                }
            }
            return total;
        }

        private void BindStoreDropDown(DropDownList ddlStore)
        {
            using (SqlConnection con = new SqlConnection(strcon))
            {
                string query = "SELECT id, storeNo FROM stores ORDER BY storeNo";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        ddlStore.DataSource = reader;
                        ddlStore.DataTextField = "storeNo";
                        ddlStore.DataValueField = "id";
                        ddlStore.DataBind();
                    }
                }
            }

            ddlStore.Items.Insert(0, new ListItem("", ""));
        }


        private void clearForm()
        {
            usernameTextBox.Text = string.Empty;
            passwordTextBox.Text = string.Empty;
            storeTextBox.SelectedValue = "";
            roleTextBox.SelectedValue = "";

        }

        private void BindGrid()
        {
            string query = "SELECT u.id, u.Username, u.Password, u.Role, s.StoreName, u.IsEnabled FROM Users u JOIN Stores s ON u.StoreNo = s.id order by u.id desc";

            using (SqlConnection con = new SqlConnection(strcon))
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    ViewState["UserData"] = dt;

                    userGridView.DataSource = dt;
                    userGridView.DataBind();
                }
            }
        }

        protected void userGridView_Sorting(object sender, GridViewSortEventArgs e)
        {
            // Retrieve data from ViewState
            DataTable dt = ViewState["UserData"] as DataTable;
            if (dt != null)
            {
                DataView dv = new DataView(dt);

                // Toggle sorting direction
                if (ViewState["SortDirection"] == null || (string)ViewState["SortDirection"] == "ASC")
                {
                    dv.Sort = e.SortExpression + " DESC";
                    ViewState["SortDirection"] = "DESC";
                }
                else
                {
                    dv.Sort = e.SortExpression + " ASC";
                    ViewState["SortDirection"] = "ASC";
                }

                userGridView.DataSource = dv;
                userGridView.DataBind();
            }
        }

        protected void GridView2_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            userGridView.PageIndex = e.NewPageIndex;
            BindGrid();
        }

        protected string MaskPassword(object password)
        {
            if (password == null)
                return string.Empty;

            int length = password.ToString().Length;
            return new string('*', length);
        }
    }
}