using CarbonProject.Helpers;
using CarbonProject.Models;
using Microsoft.Data.SqlClient;
using System.Diagnostics;

namespace CarbonProject.Repositories
{
    public class MembersRepository
    {
        // 連線字串從 appsettings.json 取得
        private readonly string connStr;

        public MembersRepository(IConfiguration configuration)
        {
            connStr = configuration.GetConnectionString("DefaultConnection");
        }
        // 新增會員
        public int AddMember(MembersViewModel member)
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
                    VALUES (@Username, @Email, @PasswordHash, @FullName, @Role, @IsActive, SYSUTCDATETIME(), SYSUTCDATETIME());
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
        public MembersViewModel? CheckLogin(string usernameOrEmail, string password)
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
                                DateTime.UtcNow.Subtract(lastFailedLoginAt.Value).TotalMinutes >= 30)
                            {
                                ResetFailedLogin(Convert.ToInt32(reader["MemberId"]));
                                failedAttempts = 0;
                            }

                            // 如果錯誤次數 >= 5，（30 分鐘內）
                            if (failedAttempts >= 5)
                            {
                                // 帳號被鎖定，不進行密碼驗證
                                return new MembersViewModel
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
                                return new MembersViewModel
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
        public void UpdateLastLoginAt(int memberId)
        {
            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();
                string sql = @"
                    UPDATE Users 
                    SET 
                        LastLoginAt = SYSUTCDATETIME(),
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
        public void IncrementFailedLogin(string usernameOrEmail)
        {
            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();
                string sql = @"
                    UPDATE Users 
                    SET 
                        FailedLoginAttempts = ISNULL(FailedLoginAttempts, 0) + 1,
                        LastFailedLoginAt = SYSUTCDATETIME()
                    WHERE Username = @U OR Email = @U";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@U", usernameOrEmail);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        // 登入成功後重設失敗次數
        public void ResetFailedLogin(int memberId)
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
        public void UpdateLastLogoutAt(int memberId)
        {
            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();
                string sql = @"UPDATE Users 
                       SET LastLogoutAt = SYSUTCDATETIME() 
                       WHERE MemberId = @MemberId";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@MemberId", memberId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // 會員名取得會員ID
        public int GetMemberIdByUsername(string username)
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
        public List<MembersViewModel> GetAllMembers()
        {
            var list = new List<MembersViewModel>();
            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();
                string sql = "SELECT * FROM Users";
                using (var cmd = new SqlCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new MembersViewModel
                        {
                            MemberId = (int)reader["MemberId"],
                            Username = reader["Username"].ToString(),
                            Email = reader["Email"].ToString(),
                            PasswordHash = reader["PasswordHash"].ToString(),
                            FullName = reader["FullName"].ToString(),
                            Role = reader["Role"].ToString(),
                            CreatedAt = TimeHelper.ToTaipeiTime(Convert.ToDateTime(reader["CreatedAt"])),
                            UpdatedAt = TimeHelper.ToTaipeiTime(Convert.ToDateTime(reader["UpdatedAt"])),
                            IsActive = Convert.ToBoolean(reader["IsActive"])
                        });
                    }
                }
            }
            return list;
        }
        // 刪除會員 by id
        public bool DeleteMember(int id)
        {

            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();

                // 刪除使用者
                string sql = "DELETE FROM Users WHERE MemberId=@Id";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
        // 更新會員資料
        public bool UpdateMember(int id, string username, string email, string fullname)
        {
            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();
                string sql = @"UPDATE Users 
                               SET Username=@Username, Email=@Email, FullName=@FullName, UpdatedAt=SYSUTCDATETIME() 
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
        public List<HomeIndexViewModel.ActivityRecord> GetRecentActivities(int top = 20)
        {
            var list = new List<HomeIndexViewModel.ActivityRecord>();
            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();
                // 抓取最近20筆資料
                string sql = @"
                            SELECT TOP (@Top)
                                al.ActionTime,
                                al.ActionType,
                                al.CreatedBy
                            FROM ActivityLog al
                            ORDER BY al.ActionTime DESC";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Top", top);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string actionType = reader["ActionType"].ToString();

                            // 可選：轉換成更可讀的中文
                            string actionDisplay;
                            if (actionType == "Auth.Login.Success")
                                actionDisplay = "登入系統";
                            else if (actionType == "Auth.Login.Failed")
                                actionDisplay = "登入失敗";
                            else if (actionType == "Auth.Logout")
                                actionDisplay = "登出系統";
                            else
                                actionDisplay = actionType; // 其他原樣顯示

                            // UTC -> 台北
                            DateTime utcTime = Convert.ToDateTime(reader["ActionTime"]);
                            DateTime taipeiTime = TimeHelper.ToTaipeiTime(utcTime);

                            list.Add(new HomeIndexViewModel.ActivityRecord
                            {
                                Username = reader["CreatedBy"]?.ToString() ?? "Anonymous",
                                ActionTime = taipeiTime, // 修正成台北時間
                                Action = actionDisplay
                            });
                        }
                    }
                }
            }
            return list;
        }
    }
}