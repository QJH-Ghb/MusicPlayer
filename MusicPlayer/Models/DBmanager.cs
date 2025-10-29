using Microsoft.Data.SqlClient;
using MusicPlayer.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

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
                        userID = reader.GetInt32(reader.GetOrdinal("UserID")),
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
            SqlCommand sqlcommand = new SqlCommand(@"INSERT INTO Users(UserName,Password,Email,avatar) VALUES(@username,@password,@email,@avatar)");
            sqlcommand.Connection = sqlconnection;

            sqlcommand.Parameters.Add(new SqlParameter("@username", user.userName));
            sqlcommand.Parameters.Add(new SqlParameter("@password", user.passWord));
            sqlcommand.Parameters.Add(new SqlParameter("@email", user.email));
            
            sqlconnection.Open();
            sqlcommand.ExecuteNonQuery();
            sqlconnection.Close();
        }
        // 取得播放清單
        public Dictionary<int,List<int>> getPlaylists()
        {
            Dictionary<int, List<int>> userPlaylist = new Dictionary<int, List<int>>();

            SqlConnection sqlConnection = new SqlConnection(connStr);
            sqlConnection.Open();

            SqlCommand sqlCommand = new SqlCommand("SELECT * FROM Playlist");
            sqlCommand.Connection = sqlConnection;

            SqlDataReader reader = sqlCommand.ExecuteReader();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    Playlist playlist = new Playlist
                    {
                        SongID = reader.GetInt32(reader.GetOrdinal("SongID")),
                        UserID = reader.GetInt32(reader.GetOrdinal("UserID")),
                    };
                    if (!userPlaylist.ContainsKey(playlist.UserID))
                    {
                        userPlaylist[playlist.UserID] = new List<int>();
                    }
                    userPlaylist[playlist.UserID].Add(playlist.SongID);
                }
            }
            else
            {
                Console.WriteLine("資料庫為空！");
            }
            sqlConnection.Close();
            return userPlaylist;
        }
        // 取得歌曲資訊
        public List<Song> GetAllSongs()
        {
            List<Song> songs = new List<Song>();

            SqlConnection sqlConnection = new SqlConnection(connStr);
            sqlConnection.Open();

            SqlCommand sqlCommand = new SqlCommand("SELECT * FROM SongList");
            sqlCommand.Connection = sqlConnection;

            SqlDataReader reader = sqlCommand.ExecuteReader();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    Song song = new Song
                    {
                        SongID = reader.GetInt32(reader.GetOrdinal("SongID")),
                        Title = reader.GetString(reader.GetOrdinal("title")),
                        Artist = reader.GetString(reader.GetOrdinal("artist")),
                        FilePath = reader.GetString(reader.GetOrdinal("file_Path")),
                    };
                    songs.Add(song);
                }
            }
            else
            {
                Console.WriteLine("資料庫為空！");
            }
            sqlConnection.Close();
            return songs;
        }
        public List<Song> GetSongsByUser(int userId)
        {
            List<Song> songs = new List<Song>();
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                string sql = @"
                SELECT s.SongID, s.title, s.artist, s.file_path
                FROM Playlist p
                JOIN SongList s ON p.SongID = s.SongID
                WHERE p.UserID = @UserID";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            songs.Add(new Song
                            {
                                SongID = reader.GetInt32(reader.GetOrdinal("SongID")),
                                Title = reader.GetString(reader.GetOrdinal("title")),
                                Artist = reader.GetString(reader.GetOrdinal("artist")),
                                FilePath = reader.GetString(reader.GetOrdinal("file_path"))
                            });
                        }
                    }
                }
            }
            return songs;
        }
        public int NewCollectIfNotExists(int userId, int songId)
        {
            using var conn = new SqlConnection(connStr);
            conn.Open();
            var sql = @"
                INSERT INTO Playlist (UserID, SongID)
                SELECT @uid, @sid
                WHERE NOT EXISTS (
                    SELECT 1 FROM Playlist WHERE UserID=@uid AND SongID=@sid
                );";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@uid", userId);
            cmd.Parameters.AddWithValue("@sid", songId);
            return cmd.ExecuteNonQuery(); // 1=新增、0=原本就有
        }
        public void RemoveFromPlaylist(int userId, int songId)
        {
            using var conn = new SqlConnection(connStr);
            conn.Open();
            var sql = "DELETE FROM Playlist WHERE UserID=@uid AND SongID=@sid";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@uid", userId);
            cmd.Parameters.AddWithValue("@sid", songId);
            cmd.ExecuteNonQuery();
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
