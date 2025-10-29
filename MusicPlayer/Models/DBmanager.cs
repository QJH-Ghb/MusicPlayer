using Microsoft.Data.SqlClient;
using MusicPlayer.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
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
        public int GetOrCreateLocalUserIdByExternal(string provider, string externalKey, string? name, string? email)
        {
            if (string.IsNullOrWhiteSpace(provider)) throw new ArgumentException("provider is required");
            if (string.IsNullOrWhiteSpace(externalKey)) throw new ArgumentException("externalKey is required");

            using var conn = new SqlConnection(connStr);
            conn.Open();
            using var tran = conn.BeginTransaction();

            try
            {
                // 1) 先查是否已有映射
                using (var cmd = new SqlCommand(@"
                    SELECT UE.UserID
                    FROM dbo.UsersExternal UE
                    WHERE UE.Provider = @p AND UE.ExternalKey = @k;
                    ", conn, tran))
                {
                    cmd.Parameters.AddWithValue("@p", provider);
                    cmd.Parameters.AddWithValue("@k", externalKey);
                    var existing = cmd.ExecuteScalar();
                    if (existing != null && existing != DBNull.Value)
                    {
                        tran.Commit();
                        return Convert.ToInt32(existing);
                    }
                }

                // 2) 若沒有映射：先試著用 Email 找看是否已有本地帳號（使用者先註冊、後綁定）
                int? userIdByEmail = null;
                if (!string.IsNullOrWhiteSpace(email))
                {
                    using var cmdFindByEmail = new SqlCommand(@"
                    SELECT TOP 1 U.UserID FROM dbo.Users U WHERE U.Email = @em;
                    ", conn, tran);
                    cmdFindByEmail.Parameters.AddWithValue("@em", email!);
                    var found = cmdFindByEmail.ExecuteScalar();
                    if (found != null && found != DBNull.Value)
                        userIdByEmail = Convert.ToInt32(found);
                }

                int localUserId;
                if (userIdByEmail.HasValue)
                {
                    localUserId = userIdByEmail.Value;
                }
                else
                {
                    // 3) 建立新的本地 Users 資料列（UserName 需避免撞名）
                    // 產生一個安全的使用者名稱（純示範）：ext-<provider>-<6位隨機>
                    string baseUserName = GenerateSafeUserName(name, provider);

                    // 確保不撞名：若撞名就加編號尾碼
                    string finalUserName = EnsureUniqueUserName(conn, tran, baseUserName);

                    using var cmdInsertUser = new SqlCommand(@"
                        INSERT INTO dbo.Users (UserName, Password, Email, CreatedAt)
                        OUTPUT INSERTED.UserID
                        VALUES (@un, @pw, @em, SYSUTCDATETIME());
                        ", conn, tran);

                    cmdInsertUser.Parameters.AddWithValue("@un", finalUserName);
                    // 外部登入無密碼，本地密碼留空字串或預設值（視你的 schema 規則）
                    cmdInsertUser.Parameters.AddWithValue("@pw", "");
                    cmdInsertUser.Parameters.AddWithValue("@em", (object?)email ?? DBNull.Value);

                    localUserId = Convert.ToInt32(cmdInsertUser.ExecuteScalar());
                }

                // 4) 寫入映射表
                using (var cmdMap = new SqlCommand(@"
                        INSERT INTO dbo.UsersExternal (UserID, Provider, ExternalKey)
                        VALUES (@uid, @p, @k);
                        ", conn, tran))
                {
                    cmdMap.Parameters.AddWithValue("@uid", localUserId);
                    cmdMap.Parameters.AddWithValue("@p", provider);
                    cmdMap.Parameters.AddWithValue("@k", externalKey);
                    cmdMap.ExecuteNonQuery();
                }

                tran.Commit();
                return localUserId;
            }
            catch
            {
                try { tran.Rollback(); } catch { /* ignore */ }
                throw;
            }
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
        private static string GenerateSafeUserName(string? name, string provider)
        {
            string baseName = string.IsNullOrWhiteSpace(name) ? "user" : name.Trim();

            // 去掉不適合當帳號的符號（僅保留字母數字與 _ - .）
            var filtered = new StringBuilder();
            foreach (var ch in baseName)
            {
                if (char.IsLetterOrDigit(ch) || ch == '_' || ch == '-' || ch == '.')
                    filtered.Append(ch);
            }
            if (filtered.Length == 0) filtered.Append("user");

            // 簡短化，避免太長（依你的 Users.UserName 長度調整，這裡示範 24）
            var shortName = filtered.Length > 24 ? filtered.ToString(0, 24) : filtered.ToString();

            // 加上 provider 與隨機尾碼，降低撞名機率
            var rnd = new Random();
            var tail = rnd.Next(0, 999999).ToString("000000");
            return $"ext-{provider}-{shortName}-{tail}";
        }
        private static string EnsureUniqueUserName(SqlConnection conn, SqlTransaction tran, string baseUserName)
        {
            // 假設 UserName 有 UNIQUE 索引（或至少當成邏輯唯一）
            string candidate = baseUserName;
            int suffix = 0;

            while (true)
            {
                using var cmd = new SqlCommand(@"
                SELECT COUNT(1) FROM dbo.Users WHERE UserName = @un;
                ", conn, tran);
                cmd.Parameters.AddWithValue("@un", candidate);
                int count = Convert.ToInt32(cmd.ExecuteScalar());

                if (count == 0) return candidate;

                suffix++;
                // 控制長度避免超過欄位限制（假設 50 字元）
                string trimmed = baseUserName;
                const int maxLen = 50;
                if (trimmed.Length > maxLen - 1 - suffix.ToString().Length)
                    trimmed = trimmed.Substring(0, maxLen - 1 - suffix.ToString().Length);

                candidate = $"{trimmed}-{suffix}";
            }
        }
    }
}
