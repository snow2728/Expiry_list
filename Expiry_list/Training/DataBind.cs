using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using static Expiry_list.Training.scheduleList;

namespace Expiry_list.Training
{
    public static class DataBind
    {
        private static string strcon = System.Configuration.ConfigurationManager
        .ConnectionStrings["con"].ConnectionString;

        public static void BindTopic(DropDownList ddlTopic)
        {
            using (SqlConnection con = new SqlConnection(strcon))
            {
                con.Open();
                string query = @"
                    SELECT t.id, t.topicName, tr.name AS trainerName
                    FROM topicT t 
                    INNER JOIN trainerT tr ON t.trainerId = tr.id
                     where t.IsActive = 'true' ORDER BY t.topicName";

                using (SqlCommand cmd = new SqlCommand(query, con))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    ddlTopic.Items.Clear();
                    ddlTopic.Items.Insert(0, new ListItem("Select Topic", ""));

                    while (reader.Read())
                    {
                        ListItem item = new ListItem(reader["topicName"].ToString(),
                                                     reader["id"].ToString());
                        item.Attributes["data-trainer"] = reader["trainerName"].ToString();
                        ddlTopic.Items.Add(item);
                    }
                }
            }
        }

        public static void BindStore(DropDownList ddlTopic)
        {
            using (SqlConnection con = new SqlConnection(strcon))
            {
                con.Open();
                string query = @"
            SELECT t.id, t.storeNo as store
            FROM stores t
            ORDER BY t.storeNo";

                using (SqlCommand cmd = new SqlCommand(query, con))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    ddlTopic.Items.Clear();
                    ddlTopic.Items.Insert(0, new ListItem("Select Stores", ""));

                    while (reader.Read())
                    {
                        ListItem item = new ListItem(reader["store"].ToString(),
                                                     reader["id"].ToString());
                        ddlTopic.Items.Add(item);
                    }
                }
            }
        }

        public static void BindLevel(DropDownList ddlLevel)
        {
            using (SqlConnection con = new SqlConnection(strcon))
            {
                con.Open();
                string query = "SELECT id, name from levelT;";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        ddlLevel.Items.Clear();
                        while (reader.Read())
                        {
                            ListItem item = new ListItem(reader["name"].ToString(), reader["id"].ToString());
                            //item.Attributes["data-trainer"] = reader["trainerName"].ToString();
                            ddlLevel.Items.Add(item);
                        }
                    }
                }
                ddlLevel.Items.Insert(0, new ListItem("Select Level", ""));
            }
        }

        public static void BindPosition(DropDownList ddlPosition)
        {
            using (SqlConnection con = new SqlConnection(strcon))
            {
                con.Open();
                string query = "SELECT id, position from positionT;";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        ddlPosition.Items.Clear();
                        while (reader.Read())
                        {
                            ListItem item = new ListItem(reader["position"].ToString(), reader["id"].ToString());
                            //item.Attributes["data-trainer"] = reader["trainerName"].ToString();
                            ddlPosition.Items.Add(item);
                        }
                    }
                }
                ddlPosition.Items.Insert(0, new ListItem("Select Position", ""));
            }
        }

        public static void BindTopicDropdown(DropDownList dropdown)
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

        public static void BindTrainer(DropDownList traineDp)
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

        public static void BindRoom(DropDownList roomDp)
        {
            using (SqlConnection con = new SqlConnection(strcon))
            {
                con.Open();
                string query = "SELECT id, name FROM locationT ORDER BY name";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        roomDp.DataSource = reader;
                        roomDp.DataTextField = "name";
                        roomDp.DataValueField = "id";
                        roomDp.DataBind();
                    }
                }
                roomDp.Items.Insert(0, new ListItem("Select Training Room", ""));
            }
        }
    }
}