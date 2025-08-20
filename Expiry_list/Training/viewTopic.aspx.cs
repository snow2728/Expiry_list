using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Script.Services;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Expiry_list.Training
{
    public partial class viewTopic : System.Web.UI.Page
    {
        string strcon = ConfigurationManager.ConnectionStrings["con"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {

            if (!IsPostBack)
            {
                BindUserGrid();
                BindTrainer();
            }
        }

        private void BindUserGrid()
        {
            using (var conn = new SqlConnection(strcon))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
            SELECT t.id,
                   t.topicName,
                   t.description,
                   t.trainerId,
                   ISNULL(tr.name, '') AS trainerName
            FROM topicT t
            LEFT JOIN trainerT tr ON t.trainerId = tr.id
            ORDER BY t.id ASC;
        ";

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

        private void BindTopicDropdown(DropDownList dropdown)
        {
            using (SqlConnection con = new SqlConnection(strcon))
            {
                con.Open();
                string query = "SELECT id, name FROM trainerT ORDER BY name";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        dropdown.DataSource = reader;
                        dropdown.DataTextField = "name";
                        dropdown.DataValueField = "id";
                        dropdown.DataBind();
                    }
                }
                dropdown.Items.Insert(0, new ListItem("Select Topic", ""));
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

            TextBox txtTopicName = (TextBox)GridView2.Rows[e.RowIndex].FindControl("txtTopicName");
            TextBox txtDescription = (TextBox)GridView2.Rows[e.RowIndex].FindControl("txtDescription");
            DropDownList traineDp = (DropDownList)GridView2.Rows[e.RowIndex].FindControl("traineDp");

            using (SqlConnection con = new SqlConnection(strcon))
            {
                string query = @"UPDATE topicT 
                        SET topicName = @topicName, 
                            description = @description,
                            trainerId = @trainerId
                        WHERE id = @id";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@topicName", txtTopicName.Text);
                    cmd.Parameters.AddWithValue("@description", txtDescription.Text);
                    cmd.Parameters.AddWithValue("@trainerId",
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

                    string query = "DELETE FROM topicT WHERE id = @id";

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
            Response.Redirect("viewTopic.aspx");
        }

        protected void GridView2_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                if ((e.Row.RowState & DataControlRowState.Edit) == DataControlRowState.Edit)
                {
                    DropDownList traineDp = (DropDownList)e.Row.FindControl("traineDp");

                    if (traineDp != null)
                    {
                        BindTopicDropdown(traineDp);

                        DataRowView rowView = (DataRowView)e.Row.DataItem;
                        if (rowView["trainerId"] != DBNull.Value)
                        {
                            traineDp.SelectedValue = rowView["trainerId"].ToString();
                        }
                    }
                }
            }
        }

        private void BindTrainer()
        {
            using (SqlConnection con = new SqlConnection(strcon))
            {
                con.Open();
                string query = "SELECT id, name FROM trainerT ORDER BY name";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        traineDp.DataSource = reader;
                        traineDp.DataTextField = "name";
                        traineDp.DataValueField = "id";
                        traineDp.DataBind();
                    }
                }
                traineDp.Items.Insert(0, new ListItem("Select Trainer", ""));
            }
        }

        protected void btnaddTopic_Click(object sender, EventArgs e)
        {
            try
            {
                string name = topicName.Text.Trim();
                string desc = topicdesc.Text.Trim();
                string trainerId = traineDp.SelectedValue;

                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(trainerId))
                {
                    ShowAlert("Error!", "Topic name and trainer are required!", "error");
                    return;
                }

                using (SqlConnection con = new SqlConnection(strcon))
                {
                    con.Open();
                    string checkQuery = "SELECT COUNT(*) FROM topicT WHERE topicname = @name";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, con))
                    {
                        checkCmd.Parameters.AddWithValue("@name", name);
                        int existingCount = (int)checkCmd.ExecuteScalar();
                        if (existingCount > 0)
                        {
                            ShowAlert("Error!", "A topic with this name already exists!", "error");
                            return;
                        }
                    }

                    SqlTransaction tran = con.BeginTransaction();

                    try
                    {
                        string insertQuery = @"INSERT INTO topicT (topicName, description, trainerId)
                                      VALUES (@name, @description, @trainer_id)";

                        using (SqlCommand cmd = new SqlCommand(insertQuery, con, tran))
                        {
                            cmd.Parameters.AddWithValue("@name", name);
                            cmd.Parameters.AddWithValue("@description", desc);
                            cmd.Parameters.AddWithValue("@trainer_id", trainerId);
                            cmd.ExecuteNonQuery();
                        }

                        tran.Commit();
                        ShowAlert("Success!", "Topic added successfully!", "success");
                        ClearForm();
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        ShowAlert("Error!", $"Insert failed: {ex.Message}", "error");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowAlert("Error!", $"Unexpected error: {ex.Message}", "error");
            }
        }

        private void ShowAlert(string title, string message, string type)
        {
            ScriptManager.RegisterStartupScript(this, GetType(), "alert",
                $"swal('{title}', '{HttpUtility.JavaScriptStringEncode(message)}', '{type}');", true);
        }

        private void ClearForm()
        {
            topicName.Text = "";
            topicdesc.Text = "";
            traineDp.SelectedIndex = 0;
        }
    }
}