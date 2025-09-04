using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Expiry_list.Training
{
    public partial class viewTrainer : System.Web.UI.Page
    {
        string strcon = ConfigurationManager.ConnectionStrings["con"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                GridView2.DataSource = new List<string>(); 
                GridView2.DataBind();

                BindUserGrid();
                Training.DataBind.BindPosition(trainerPosition);
            }
        }

        private void BindUserGrid()
        {
            using (var conn = new SqlConnection(strcon))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT 
                        t.id, 
                        t.name, 
                        t.position,  
                        p.position AS positionName
                    FROM trainerT t
                    INNER JOIN positionT p ON t.position = p.id
                    ORDER BY t.id ASC;";

                conn.Open();
                using (var da = new SqlDataAdapter(cmd))
                using (var dt = new DataTable())
                {
                    da.Fill(dt);
                    GridView2.DataSource = dt;
                    GridView2.DataBind();
                }
            }
        }

        protected void GridView2_Sorting(object sender, GridViewSortEventArgs e)
        {
            string sortExpression = e.SortExpression;
            string direction = ViewState["SortDirection"] as string == "ASC" ? "DESC" : "ASC";
            ViewState["SortDirection"] = direction;

            using (SqlConnection con = new SqlConnection(strcon))
            {
                string query = $@"SELECT 
                        t.id, 
                        t.name, 
                        t.position,              -- Add this FK column!
                        p.position AS positionName
                    FROM trainerT t
                    INNER JOIN positionT p ON t.position = p.id
                    ORDER BY {sortExpression} {direction}";

                SqlDataAdapter da = new SqlDataAdapter(query, con);
                DataTable dt = new DataTable();
                da.Fill(dt);

                GridView2.DataSource = dt;
                GridView2.DataBind();
            }
        }

        protected void GridView2_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow &&
                (e.Row.RowState & DataControlRowState.Edit) > 0)
            {
                DropDownList ddlPosition = (DropDownList)e.Row.FindControl("txtPosition");

                using (SqlConnection con = new SqlConnection(strcon))
                {
                    string query = "SELECT id, position FROM PositionT";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        con.Open();
                        SqlDataReader rdr = cmd.ExecuteReader();
                        ddlPosition.DataSource = rdr;
                        ddlPosition.DataTextField = "position"; 
                        ddlPosition.DataValueField = "id";     
                        ddlPosition.DataBind();
                    }
                }

                string currentValue = DataBinder.Eval(e.Row.DataItem, "position").ToString();
                if (ddlPosition.Items.FindByValue(currentValue) != null)
                {
                    ddlPosition.SelectedValue = currentValue;
                }
            }
        }

        protected void GridView2_RowEditing(object sender, GridViewEditEventArgs e)
        {
            GridView2.EditIndex = e.NewEditIndex;
            BindUserGrid();
        }

        protected void GridView2_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            int id = Convert.ToInt32(GridView2.DataKeys[e.RowIndex].Value);

            GridViewRow row = GridView2.Rows[e.RowIndex];

            string name = ((TextBox)row.FindControl("txtName")).Text;
            DropDownList ddlPosition = (DropDownList)row.FindControl("txtPosition");
            string positionId = ddlPosition.SelectedValue;

            using (SqlConnection con = new SqlConnection(strcon))
            {
                string query = "UPDATE trainerT SET name = @name, position = @position WHERE id = @id";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@position", positionId);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            GridView2.EditIndex = -1; 
            BindUserGrid(); 
        }

        protected void GridView2_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            try
            {
                int id = Convert.ToInt32(GridView2.DataKeys[e.RowIndex].Value);

                using (SqlConnection con = new SqlConnection(strcon))
                {
                    con.Open();
                    string query = "DELETE FROM trainerT WHERE id = @id";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            ScriptManager.RegisterStartupScript(
                                this, GetType(), "DeleteSuccess",
                                "Swal.fire({icon: 'success', title: 'Deleted!', text: 'The trainer has been successfully removed from the system.'});", true);
                        }
                        else
                        {
                            ScriptManager.RegisterStartupScript(
                                this, GetType(), "DeleteNotFound",
                                "Swal.fire({icon: 'warning', title: 'Not Found', text: 'The trainer you are trying to delete does not exist or has already been removed.'});", true);
                        }
                    }
                }

                BindUserGrid();
            }
            catch (SqlException)
            {
                ScriptManager.RegisterStartupScript(
                    this, GetType(), "DeleteError",
                    "Swal.fire({icon: 'error', title: 'Cannot Delete', text: 'This trainer is linked to other records and cannot be deleted. Please remove related records first.'});", true);
            }
            catch (Exception ex)
            {
                ScriptManager.RegisterStartupScript(
                    this, GetType(), "DeleteError",
                    $"Swal.fire({{icon: 'error', title: 'Error', text: '{HttpUtility.JavaScriptStringEncode(ex.Message)}'}});", true);
            }
        }

        protected void GridView2_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            GridView2.EditIndex = -1;
            BindUserGrid();
        }

        protected void btnaddTrainer_Click(object sender, EventArgs e)
        {
            try
            {
                string name = trainerName.Text.Trim();
                string position = trainerPosition.SelectedValue;
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(position))
                {
                    ShowAlert("Error!", "Trainer name and position are required!", "error");
                    return;
                }

                using (SqlConnection con = new SqlConnection(strcon))
                {
                    con.Open();

                    string checkQuery = "SELECT COUNT(*) FROM trainerT WHERE name = @name";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, con))
                    {
                        checkCmd.Parameters.AddWithValue("@name", name);
                        int existingCount = (int)checkCmd.ExecuteScalar();
                        if (existingCount > 0)
                        {
                            ShowAlert("Error!", "A trainer with this name already exists!", "error");
                            return;
                        }
                    }

                    using (SqlTransaction tran = con.BeginTransaction())
                    {
                        try
                        {
                            string query = @"INSERT INTO trainerT (name, position) VALUES (@name, @position)";
                            using (SqlCommand cmd = new SqlCommand(query, con, tran))
                            {
                                cmd.Parameters.AddWithValue("@name", name);
                                cmd.Parameters.AddWithValue("@position", position);
                                cmd.ExecuteNonQuery();
                            }

                            tran.Commit();
                            ShowAlert("Success!", "Trainer registered successfully!", "success");
                            ClearForm();

                            GridView2.EditIndex = -1;
                            BindUserGrid();
                        }
                        catch (Exception ex)
                        {
                            tran.Rollback();
                            ShowAlert("Error!", $"Registration failed: {ex.Message}", "error");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowAlert("Error!", $"Unexpected error: {ex.Message}", "error");
            }
        }

        private void ClearForm()
        {
            trainerName.Text = "";
            trainerPosition.SelectedIndex = 0;
            trainerName.Focus();
        }

        private void ShowAlert(string title, string message, string type)
        {
            ScriptManager.RegisterStartupScript(this, GetType(), "alert",
                $"swal('{title}', '{HttpUtility.JavaScriptStringEncode(message)}', '{type}');", true);
        }
    }
}