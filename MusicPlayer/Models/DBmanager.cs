using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace MVC_DB_.Models
{
    public class DBmanager
    {
        private readonly string connStr = "Data Source=(localdb)\\MSSQLLocalDB;Database=MusicPlayer;User ID=QJhdatabase;Password=123456789;Trusted_Connection=True";
        public List<account> getAccounts()
        {
            List<account> accounts = new List<account>();

            SqlConnection sqlConnection = new SqlConnection(connStr);
            sqlConnection.Open();

            SqlCommand sqlCommand = new SqlCommand("SELECT * FROM Users");
            sqlCommand.Connection = sqlConnection;

            SqlDataReader reader = sqlCommand.ExecuteReader();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    account account = new account
                    {
                        ID = reader.GetInt32(reader.GetOrdinal("UserID")),
                        userName = reader.GetString(reader.GetOrdinal("UserName")),
                        passWord = reader.GetString(reader.GetOrdinal("Password")),
                        email = reader.GetString(reader.GetOrdinal("Email")),
                        createTime = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    };
                    accounts.Add(account);
                }
            }
            else
            {
                Console.WriteLine("資料庫為空！");
            }
            sqlConnection.Close();
            return accounts;
        }

        public void newAccount(account user)
        {
            SqlConnection sqlconnection = new SqlConnection(connStr);
            SqlCommand sqlcommand = new SqlCommand(@"INSERT INTO Users(UserName,Password,Email) VALUES(@username,@password,@email)");
            sqlcommand.Connection = sqlconnection;

            sqlcommand.Parameters.Add(new SqlParameter("@username", user.userName));
            sqlcommand.Parameters.Add(new SqlParameter("@password", user.passWord));
            sqlcommand.Parameters.Add(new SqlParameter("@email", user.email));

            sqlconnection.Open();
            sqlcommand.ExecuteNonQuery();
            sqlconnection.Close();
        }
        public bool CheckAccountExists(string username)
        {
            using (SqlConnection sqlconnection = new SqlConnection(connStr))
            {
                string query = "SELECT COUNT(1) FROM [dbo].[Users] WHERE UserName = @username";

                using (SqlCommand sqlcommand = new SqlCommand(query, sqlconnection))
                {
                    sqlcommand.Parameters.AddWithValue("@username", username);

                    sqlconnection.Open();
                    int count = Convert.ToInt32(sqlcommand.ExecuteScalar());

                    return count > 0; // 若 >=1 則帳號存在
                }
            }
        }
    }
}
