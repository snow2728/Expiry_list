using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI;
public static class Common
{
    public static void ShowMessage(Page page, string message)
    {
        ScriptManager.RegisterStartupScript(page, page.GetType(), "alert", $"alert('{message}');", true);
    }

    public static string FormatCurrency(decimal amount)
    {
        return amount.ToString("N2");
    }

    public static bool IsValidDecimal(string value)
    {
        return decimal.TryParse(value, out _);
    }
    private static string strcon = ConfigurationManager.ConnectionStrings["con"].ConnectionString;
    public static List<string> GetLoggedInUserStoreNames()
    {
        List<string> storeNos = HttpContext.Current.Session["storeListRaw"] as List<string>;
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

    public static Dictionary<string, string> GetAllowedFormsByUser(string username)
    {
        Dictionary<string, string> forms = new Dictionary<string, string>();

        using (SqlConnection conn = new SqlConnection(strcon))
        {
            string query = @"
            SELECT f.name AS FormName,
                   CASE up.permission_level
                        WHEN 1 THEN 'view'
                        WHEN 2 THEN 'edit'
                        WHEN 3 THEN 'admin'
                        WHEN 4 THEN 'super'
                        WHEN 5 THEN 'super1'
                        ELSE 'none'
                   END AS Permission
                    FROM UserPermissions up
                    INNER JOIN Forms f ON up.form_id = f.id
                    INNER JOIN Users u ON up.user_id = u.id
                    WHERE u.username = @Username";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Username", username);
            conn.Open();

            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    string form = reader["FormName"].ToString();
                    string permission = reader["Permission"].ToString();
                    forms[form] = permission;
                }
            }
        }
        return forms;
    }
}
