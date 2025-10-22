using BCrypt.Net;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
// 從上方工具->NuGet套件管理員->管理方案的 NuGet 套件->瀏覽輸入後點選安裝(需選擇專案)
// 需安裝 MySql.Data
// 需安裝 BCrypt.Net-Next
// 需安裝 Microsoft.Data.SqlClient


namespace CarbonProject.Models
{
    public class Members
    {
        public int MemberId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; } // Admin / Member
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // 連線字串從 appsettings.json 取得
        private static string connStr;

        public static void Init(IConfiguration configuration)
        {
            connStr = configuration.GetConnectionString("DefaultConnection");
        }

        // 新增會員
        public static bool AddMember(Members member)
        {
            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();

                // 先檢查帳號/Email
                string checkSql = "SELECT COUNT(*) FROM Members WHERE Username=@Username OR Email=@Email";
                using (var cmd = new SqlCommand(checkSql, conn))
                {
                    cmd.Parameters.AddWithValue("@Username", member.Username);
                    cmd.Parameters.AddWithValue("@Email", member.Email);

                    int count = (int)cmd.ExecuteScalar();
                    if (count > 0) return false;
                }

                // 密碼加密
                string hashedPwd = BCrypt.Net.BCrypt.HashPassword(member.PasswordHash);

                // 新增會員
                string insertSql = @"INSERT INTO Members
                    (Username, Email, PasswordHash, FullName, Role, CreatedAt, UpdatedAt)
                    VALUES (@Username, @Email, @PasswordHash, @FullName, @Role, GETDATE(), GETDATE())";

                using (var cmd = new SqlCommand(insertSql, conn))
                {
                    cmd.Parameters.AddWithValue("@Username", member.Username);
                    cmd.Parameters.AddWithValue("@Email", member.Email);
                    cmd.Parameters.AddWithValue("@PasswordHash", hashedPwd);
                    cmd.Parameters.AddWithValue("@FullName", member.FullName ?? "");
                    cmd.Parameters.AddWithValue("@Role", member.Role ?? "Member");
                    cmd.ExecuteNonQuery();
                }
            }
            return true;
        }

        // 登入驗證
        public static Members? CheckLogin(string usernameOrEmail, string password)
        {
            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();

                string sql = "SELECT * FROM Members WHERE Username=@U OR Email=@U";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@U", usernameOrEmail);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string hash = reader["PasswordHash"].ToString();
                            if (BCrypt.Net.BCrypt.Verify(password, hash))
                            {
                                return new Members
                                {
                                    MemberId = Convert.ToInt32(reader["MemberId"]),
                                    Username = reader["Username"].ToString(),
                                    Email = reader["Email"].ToString(),
                                    FullName = reader["FullName"].ToString(),
                                    Role = reader["Role"].ToString()
                                };
                            }
                        }
                    }
                }
            }
            return null;
        }

        // 取得所有會員
        public static List<Members> GetAllMembers()
        {
            var list = new List<Members>();
            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();
                string sql = "SELECT * FROM Members";
                using (var cmd = new SqlCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Members
                        {
                            MemberId = (int)reader["MemberId"],
                            Username = reader["Username"].ToString(),
                            Email = reader["Email"].ToString(),
                            PasswordHash = reader["PasswordHash"].ToString(),
                            FullName = reader["FullName"].ToString(),
                            CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                            UpdatedAt = Convert.ToDateTime(reader["UpdatedAt"]),
                            Role = reader["Role"].ToString()
                        });
                    }
                }
            }
            return list;
        }
        public static bool DeleteMember(int id)
        {
            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();
                string sql = "DELETE FROM Members WHERE MemberId=@Id";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public static bool UpdateMember(int id, string username, string email, string fullname)
        {
            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();
                string sql = "UPDATE Members SET Username=@Username, Email=@Email, FullName=@FullName, UpdatedAt=NOW() WHERE MemberId=@Id";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@Username", username);
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@FullName", fullname ?? "");
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
    }
}