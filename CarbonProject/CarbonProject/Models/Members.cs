using BCrypt.Net;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text.Json;

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
        public int CompanyId { get; set; }
        public string Role { get; set; } // Admin / Viewer / Company
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsActive { get; set; }

        // 連線字串從 appsettings.json 取得
        private static string connStr;

        public static void Init(IConfiguration configuration)
        {
            connStr = configuration.GetConnectionString("DefaultConnection");
        }

        // 新增會員
        public static int AddMember(Members member)
        {
            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();

                // 先檢查帳號/Email
                string checkSql = "SELECT COUNT(*) FROM Users WHERE Username=@Username OR Email=@Email";
                using (var cmd = new SqlCommand(checkSql, conn))
                {
                    cmd.Parameters.AddWithValue("@Username", member.Username);
                    cmd.Parameters.AddWithValue("@Email", member.Email);

                    int count = (int)cmd.ExecuteScalar();
                    if (count > 0) return -1; // 已存在
                }

                // 密碼加密
                string hashedPwd = BCrypt.Net.BCrypt.HashPassword(member.PasswordHash);

                // 新增會員並回傳新 ID
                string insertSql = @"
                    INSERT INTO Users (Username, Email, PasswordHash, FullName, Role, IsActive, CreatedAt, UpdatedAt)
                    OUTPUT INSERTED.MemberId
                    VALUES (@Username, @Email, @PasswordHash, @FullName, @Role, @IsActive, GETDATE(), GETDATE());
                ";

                using (var cmd = new SqlCommand(insertSql, conn))
                {
                    cmd.Parameters.AddWithValue("@Username", member.Username);
                    cmd.Parameters.AddWithValue("@Email", member.Email);
                    cmd.Parameters.AddWithValue("@PasswordHash", hashedPwd);
                    cmd.Parameters.AddWithValue("@FullName", member.FullName ?? "");
                    cmd.Parameters.AddWithValue("@Role", member.Role ?? "Viewer");
                    cmd.Parameters.AddWithValue("@IsActive", member.IsActive);
                    cmd.Parameters.AddWithValue("@CompanyId", member.CompanyId);

                    // 取得新建會員的 MemberId
                    int newId = (int)cmd.ExecuteScalar();
                    member.MemberId = newId;
                    return newId;
                }
            }
        }

        // 登入驗證
        public static Members? CheckLogin(string usernameOrEmail, string password)
        {
            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();

                string sql = @"SELECT * FROM Users WHERE Username=@U OR Email=@U";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@U", usernameOrEmail);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string hash = reader["PasswordHash"].ToString();
                            int memberId = Convert.ToInt32(reader["MemberId"]);
                            int companyId = reader["CompanyId"] != DBNull.Value ? Convert.ToInt32(reader["CompanyId"]) : 0;

                            //登入錯誤嘗試紀錄
                            int failedAttempts = reader["FailedLoginAttempts"] != DBNull.Value ? Convert.ToInt32(reader["FailedLoginAttempts"]) : 0;
                            DateTime? lastFailedLoginAt = reader["LastFailedLoginAt"] != DBNull.Value ? Convert.ToDateTime(reader["LastFailedLoginAt"]) : null;

                            // 自動解鎖：若已鎖住且 30 分鐘已過
                            if (failedAttempts >= 5 && lastFailedLoginAt.HasValue &&
                                DateTime.Now.Subtract(lastFailedLoginAt.Value).TotalMinutes >= 30)
                            {
                                ResetFailedLogin(Convert.ToInt32(reader["MemberId"]));
                                failedAttempts = 0;
                            }

                            // 如果錯誤次數 >= 5，（30 分鐘內）
                            if (failedAttempts >= 5)
                            {
                                // 帳號被鎖定，不進行密碼驗證
                                return new Members
                                {
                                    MemberId = Convert.ToInt32(reader["MemberId"]),
                                    Username = reader["Username"].ToString(),
                                    Email = reader["Email"].ToString(),
                                    Role = reader["Role"].ToString(),
                                    IsActive = false // 標記為鎖定（供 Controller 判斷）

                                };
                            }
                            // 驗證密碼
                            if (BCrypt.Net.BCrypt.Verify(password, hash))
                            {
                                return new Members
                                {
                                    MemberId = memberId,
                                    Username = reader["Username"].ToString(),
                                    Email = reader["Email"].ToString(),
                                    FullName = reader["FullName"].ToString(),
                                    Role = reader["Role"].ToString(),
                                    CompanyId = companyId,
                                    IsActive = true
                                };
                            }
                        }
                    }
                }
            }
            return null;
        }

        // 更新最後登入時間
        public static void UpdateLastLoginAt(int memberId)
        {
            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();
                string sql = @"
                    UPDATE Users 
                    SET 
                        LastLoginAt = GETDATE(),
                        FailedLoginAttempts = 0,
                        LastFailedLoginAt = NULL
                    WHERE MemberId = @MemberId";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@MemberId", memberId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // 登入失敗時遞增 FailedLoginAttempts + 更新失敗時間
        public static void IncrementFailedLogin(string usernameOrEmail)
        {
            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();
                string sql = @"
                    UPDATE Users 
                    SET 
                        FailedLoginAttempts = ISNULL(FailedLoginAttempts, 0) + 1,
                        LastFailedLoginAt = GETDATE()
                    WHERE Username = @U OR Email = @U";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@U", usernameOrEmail);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        // 登入成功後重設失敗次數
        public static void ResetFailedLogin(int memberId)
        {
            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();
                string sql = @"UPDATE Users 
                               SET FailedLoginAttempts = 0,
                                   LastFailedLoginAt = NULL
                               WHERE MemberId=@ID";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@ID", memberId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // 更新最後登出時間
        public static void UpdateLastLogoutAt(int memberId)
        {
            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();
                string sql = @"UPDATE Users 
                       SET LastLogoutAt = GETDATE() 
                       WHERE MemberId = @MemberId";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@MemberId", memberId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // 會員名取得會員ID
        public static int GetMemberIdByUsername(string username)
        {
            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();
                string sql = @"SELECT MemberId FROM Users WHERE Username=@Username";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Username", username);
                    object result = cmd.ExecuteScalar();
                    return result == null ? 0 : Convert.ToInt32(result);
                }
            }
        }

        // 取得所有會員
        public static List<Members> GetAllMembers()
        {
            var list = new List<Members>();
            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();
                string sql = "SELECT * FROM Users";
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
                            Role = reader["Role"].ToString(),
                            CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                            UpdatedAt = Convert.ToDateTime(reader["UpdatedAt"]),
                            IsActive = Convert.ToBoolean(reader["IsActive"])
                        });
                    }
                }
            }
            return list;
        }
        // 刪除會員 by id
        public static bool DeleteMember(int id)
        {
            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();
                string sql = "DELETE FROM Users WHERE MemberId=@Id";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
        // 更新會員資料
        public static bool UpdateMember(int id, string username, string email, string fullname)
        {
            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();
                string sql = @"UPDATE Users 
                               SET Username=@Username, Email=@Email, FullName=@FullName, UpdatedAt=GETDATE() 
                               WHERE MemberId=@Id";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@Username", username);
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@FullName", fullname ?? "");
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
        // 取得最近 N 筆活動紀錄
        public class ActivityRecord
        {
            public DateTime ActionTime { get; set; }
            public string Username { get; set; }
            public string Action { get; set; }
        }

        public static List<ActivityRecord> GetRecentActivities(int top = 20)
        {
            var list = new List<ActivityRecord>();
            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();
                // 抓取最近登入與登出
                string sql = $@"
                    SELECT TOP (@Top) Username, LastLoginAt AS ActionTime, N'登入系統' AS Action
                    FROM Users
                    WHERE LastLoginAt IS NOT NULL
                    UNION ALL
                    SELECT TOP (@Top) Username, LastLogoutAt AS ActionTime, N'登出系統' AS Action
                    FROM Users
                    WHERE LastLogoutAt IS NOT NULL
                    ORDER BY ActionTime DESC";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Top", top);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new ActivityRecord
                            {
                                Username = reader["Username"].ToString(),
                                ActionTime = Convert.ToDateTime(reader["ActionTime"]),
                                Action = reader["Action"].ToString()
                            });
                        }
                    }
                }
            }
            return list;
        }
    }
}