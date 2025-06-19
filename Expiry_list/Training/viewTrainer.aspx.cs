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
                BindUserGrid();
            }
        }

        private void BindUserGrid()
        {
            using (var conn = new SqlConnection(strcon))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT t.id, t.name, t.position
                        FROM trainerT t ORDER BY t.id ASC;";

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

        protected void GridView2_RowEditing(object sender, GridViewEditEventArgs e)
        {
            GridView2.EditIndex = e.NewEditIndex;
            BindUserGrid();
        }

        protected void GridView2_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            int id = Convert.ToInt32(GridView2.DataKeys[e.RowIndex].Value);

            TextBox txtName = (TextBox)GridView2.Rows[e.RowIndex].FindControl("txtName");
            //TextBox txtPosition = (TextBox)GridView2.Rows[e.RowIndex].FindControl("txtPosition");
            DropDownList traineDp = (DropDownList)GridView2.Rows[e.RowIndex].FindControl("txtPosition");

            using (SqlConnection con = new SqlConnection(strcon))
            {
                string query = @"UPDATE trainerT 
                        SET name = @Name, 
                            position = @Position
                        WHERE id = @id";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Name", txtName.Text);
                    //cmd.Parameters.AddWithValue("@Position", txtPosition.Text);
                    cmd.Parameters.AddWithValue("@Position",
                        string.IsNullOrEmpty(traineDp.SelectedValue) ? DBNull.Value : (object)traineDp.SelectedValue);
                    cmd.Parameters.AddWithValue("@id", id);

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
                        cmd.ExecuteNonQuery();
                    }
                }

                ScriptManager.RegisterStartupScript(
                    this, GetType(), "DeleteSuccess",
                    "Swal.fire('Deleted!', 'Topic deleted successfully.', 'success');", true);

                BindUserGrid();
            }
            catch (Exception ex)
            {
                string safeMsg = HttpUtility.JavaScriptStringEncode(
                    "An error occurred while deleting the topic: " + ex.Message);

                ScriptManager.RegisterStartupScript(
                    this, GetType(), "DeleteError",
                    $"Swal.fire('Error!', '{safeMsg}', 'error');", true);
            }
        }

        protected void GridView2_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            GridView2.EditIndex = -1;
            BindUserGrid();
            Response.Redirect("viewTrainer.aspx");
        }

    }
}