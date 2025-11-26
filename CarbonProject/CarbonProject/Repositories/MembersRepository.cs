using CarbonProject.Helpers;
using CarbonProject.Models;
using CarbonProject.Models.EFModels;
using CarbonProject.Models.EFModels.RBAC;
using Microsoft.Data.SqlClient;
using System.Data;
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
                            Debug.WriteLine("===== Repositories/MembersRepository.cs =====");
                            Debug.WriteLine("--- CheckLogin(string usernameOrEmail, string password) ---");
                            Debug.WriteLine($"連線 DB 比對");
                            string hash = reader["PasswordHash"].ToString();
                            int memberId = Convert.ToInt32(reader["MemberId"]);
                            int companyId = reader["CompanyId"] != DBNull.Value ? Convert.ToInt32(reader["CompanyId"]) : 0;

                            //登入錯誤嘗試紀錄
                            int failedAttempts = reader["FailedLoginAttempts"] != DBNull.Value ? Convert.ToInt32(reader["FailedLoginAttempts"]) : 0;
                            DateTime? lastFailedLoginAt = reader["LastFailedLoginAt"] != DBNull.Value ? Convert.ToDateTime(reader["LastFailedLoginAt"]) : null;
                            Debug.WriteLine($"登入錯誤嘗試紀錄: {failedAttempts}");

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
                                Debug.WriteLine($"登入錯誤次數 >= 5，（30 分鐘內）");
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
                            bool checkloginvalidity = BCrypt.Net.BCrypt.Verify(password, hash);
                            Debug.WriteLine($"BCrypt 驗證密碼{checkloginvalidity}");
                            if (checkloginvalidity)
                            {
                                Debug.WriteLine($"BCrypt 驗證密碼成功");

                                var roles = GetRolesByMemberId(memberId, conn);

                                return new MembersViewModel
                                {
                                    MemberId = memberId,
                                    Username = reader["Username"].ToString(),
                                    Email = reader["Email"].ToString(),
                                    FullName = reader["FullName"].ToString(),
                                    CompanyId = companyId,
                                    IsActive = true,

                                    UserRoles = roles.Select(r => new UserRole
                                    {
                                        MemberId = memberId,
                                        RoleId = r.RoleId,
                                        Role = new Role { RoleId = r.RoleId, RoleName = r.RoleName }
                                    }).ToList()
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

        // 登入失敗時遞增 FailedLoginAttempts + 更新失敗時間，若達上限則回傳 true
        public bool IncrementFailedLogin(string usernameOrEmail)
        {
            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();
                // 先取得目前錯誤次數
                string selectSql = "SELECT FailedLoginAttempts FROM Users WHERE Username=@U OR Email=@U";
                int currentAttempts = 0;

                using (var selectCmd = new SqlCommand(selectSql, conn))
                {
                    selectCmd.Parameters.AddWithValue("@U", usernameOrEmail);
                    var result = selectCmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                        currentAttempts = Convert.ToInt32(result);
                }

                currentAttempts++; // 增加一次

                // 若錯誤達 5 次 → 鎖定帳號
                bool isLocked = currentAttempts >= 5;

                // 更新資料庫
                string updateSql = @"
                        UPDATE Users 
                        SET 
                            FailedLoginAttempts = @Count,
                            LastFailedLoginAt = SYSUTCDATETIME(),
                            IsActive = @IsActive
                        WHERE Username = @U OR Email = @U";

                using (var updateCmd = new SqlCommand(updateSql, conn))
                {
                    updateCmd.Parameters.AddWithValue("@Count", currentAttempts);
                    updateCmd.Parameters.AddWithValue("@IsActive", !isLocked);
                    updateCmd.Parameters.AddWithValue("@U", usernameOrEmail);
                    updateCmd.ExecuteNonQuery();
                }
                Debug.WriteLine("===== Repositories/MembersRepository.cs =====");
                Debug.WriteLine("--- IncrementFailedLogin ---");                
                Debug.WriteLine($"[DEBUG] {usernameOrEmail} -> FailedLoginAttempts: {currentAttempts}, Locked: {isLocked}");
                return isLocked;
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
        // 協助 MembersRepository.CheckLogin() 取得 roles
        private List<Role> GetRolesByMemberId(int memberId, SqlConnection conn)
        {
            var roles = new List<Role>();
            string sql = @"
                            SELECT r.RoleId, r.RoleName
                            FROM UserRoles ur
                            JOIN Roles r ON ur.RoleId = r.RoleId
                            WHERE ur.MemberId = @MemberId;
                        ";

            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@MemberId", memberId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        roles.Add(new Role
                        {
                            RoleId = Convert.ToInt32(reader["RoleId"]),
                            RoleName = reader["RoleName"].ToString()
                        });
                    }
                }
            }

            return roles;
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
            var dict = new Dictionary<int, MembersViewModel>();

            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();
                string sql = @"
                                SELECT 
                                    u.*, 
                                    c.CompanyId, c.CompanyName, c.TaxId, c.Industry_Id, c.Address, c.Contact_Email,
                                    ur.RoleId, r.RoleName
                                FROM Users u
                                LEFT JOIN Companies c ON u.CompanyId = c.CompanyId
                                LEFT JOIN UserRoles ur ON ur.MemberId = u.MemberId
                                LEFT JOIN Roles r ON r.RoleId = ur.RoleId
                                ORDER BY u.MemberId;
                            ";

                using (var cmd = new SqlCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int memberId = Convert.ToInt32(reader["MemberId"]);

                        // 若不存在，先建立主物件
                        if (!dict.ContainsKey(memberId))
                        {
                            dict[memberId] = new MembersViewModel
                            {
                                MemberId = memberId,
                                Username = reader["Username"].ToString(),
                                Email = reader["Email"].ToString(),
                                PasswordHash = reader["PasswordHash"].ToString(),
                                FullName = reader["FullName"].ToString(),
                                Role = reader["Role"].ToString(),

                                EmailConfirmed = Convert.ToBoolean(reader["EmailConfirmed"]),
                                PhoneConfirmed = Convert.ToBoolean(reader["PhoneConfirmed"]),

                                FailedLoginAttempts = reader["FailedLoginAttempts"] != DBNull.Value
                                    ? Convert.ToInt32(reader["FailedLoginAttempts"]) : 0,

                                LastLoginAt = reader["LastLoginAt"] != DBNull.Value
                                    ? Convert.ToDateTime(reader["LastLoginAt"]) : null,

                                CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                                UpdatedAt = Convert.ToDateTime(reader["UpdatedAt"]),
                                IsActive = Convert.ToBoolean(reader["IsActive"]),

                                Company = reader["CompanyId"] != DBNull.Value ? new Company
                                {
                                    CompanyId = Convert.ToInt32(reader["CompanyId"]),
                                    CompanyName = reader["CompanyName"].ToString(),
                                    TaxId = reader["TaxId"].ToString(),
                                    Industry_Id = reader["Industry_Id"].ToString(),
                                    Address = reader["Address"].ToString(),
                                    Contact_Email = reader["Contact_Email"].ToString()
                                } : null,

                                UserRoles = new List<UserRole>()
                            };
                        }

                        // 處理多角色
                        if (reader["RoleId"] != DBNull.Value)
                        {
                            dict[memberId].UserRoles.Add(new UserRole
                            {
                                MemberId = memberId,
                                RoleId = Convert.ToInt32(reader["RoleId"]),
                                Role = new Role
                                {
                                    RoleId = Convert.ToInt32(reader["RoleId"]),
                                    RoleName = reader["RoleName"].ToString()
                                }
                            });
                        }
                    }
                }
            }

            return dict.Values.ToList();
        }
        // 取得單一會員資料 by ID
        public MembersViewModel? GetMemberById(int memberId)
        {
            MembersViewModel? member = null;

            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();
                string sql = @"
                                SELECT 
                                    u.*, 
                                    c.CompanyId, c.CompanyName, c.TaxId, c.Industry_Id, c.Address AS CompanyAddress, c.Contact_Email,
                                    ur.RoleId, r.RoleName
                                FROM Users u
                                LEFT JOIN Companies c ON u.CompanyId = c.CompanyId
                                LEFT JOIN UserRoles ur ON ur.MemberId = u.MemberId
                                LEFT JOIN Roles r ON r.RoleId = ur.RoleId
                                WHERE u.MemberId=@MemberId;
                            ";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@MemberId", memberId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (member == null)
                            {
                                member = new MembersViewModel
                                {
                                    MemberId = Convert.ToInt32(reader["MemberId"]),
                                    Username = reader["Username"].ToString(),
                                    Email = reader["Email"].ToString(),
                                    FullName = reader["FullName"].ToString(),
                                    Gender = reader["Gender"] != DBNull.Value ? reader["Gender"].ToString().Trim() : null, // 加上這行
                                    Birthday = reader["Birthday"] != DBNull.Value ? Convert.ToDateTime(reader["Birthday"]) : (DateTime?)null,
                                    Address = reader["Address"] != DBNull.Value ? reader["Address"].ToString() : null,
                                    Role = reader["Role"].ToString(),
                                    PasswordHash = reader["PasswordHash"].ToString(),
                                    IsActive = Convert.ToBoolean(reader["IsActive"]),
                                    EmailConfirmed = Convert.ToBoolean(reader["EmailConfirmed"]),
                                    PhoneConfirmed = Convert.ToBoolean(reader["PhoneConfirmed"]),
                                    FailedLoginAttempts = reader["FailedLoginAttempts"] != DBNull.Value ? Convert.ToInt32(reader["FailedLoginAttempts"]) : 0,
                                    LastLoginAt = reader["LastLoginAt"] != DBNull.Value ? Convert.ToDateTime(reader["LastLoginAt"]) : null,
                                    LastLogoutAt = reader["LastLogoutAt"] != DBNull.Value ? Convert.ToDateTime(reader["LastLogoutAt"]) : null,
                                    LastFailedLoginAt = reader["LastFailedLoginAt"] != DBNull.Value ? Convert.ToDateTime(reader["LastFailedLoginAt"]) : null,
                                    CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                                    UpdatedAt = Convert.ToDateTime(reader["UpdatedAt"]),
                                    Company = reader["CompanyId"] != DBNull.Value ? new Company
                                    {
                                        CompanyId = Convert.ToInt32(reader["CompanyId"]),
                                        CompanyName = reader["CompanyName"].ToString(),
                                        TaxId = reader["TaxId"].ToString(),
                                        Industry_Id = reader["Industry_Id"].ToString(),
                                        Address = reader["CompanyAddress"].ToString(),
                                        Contact_Email = reader["Contact_Email"].ToString()
                                    } : null,
                                    UserRoles = new List<UserRole>()
                                };
                            }

                            // 多角色處理
                            if (reader["RoleId"] != DBNull.Value)
                            {
                                member.UserRoles.Add(new UserRole
                                {
                                    MemberId = memberId,
                                    RoleId = Convert.ToInt32(reader["RoleId"]),
                                    Role = new Role
                                    {
                                        RoleId = Convert.ToInt32(reader["RoleId"]),
                                        RoleName = reader["RoleName"].ToString()
                                    }
                                });
                            }
                        }
                    }
                }

                // 取得該會員的 Activity Log
                member.ActivityLogs = GetActivityLogsByMemberId(memberId, conn);
            }

            return member;
        }

        // 取得會員操作紀錄
        private List<ActivityLog> GetActivityLogsByMemberId(int memberId, SqlConnection conn)
        {
            var logs = new List<ActivityLog>();
            string sql = @"
                            SELECT * FROM ActivityLog 
                            WHERE MemberId=@MemberId
                            ORDER BY CreatedAt DESC;
                        ";

            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@MemberId", memberId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        logs.Add(new ActivityLog
                        {
                            LogId = Convert.ToInt32(reader["LogId"]),
                            MemberId = Convert.ToInt32(reader["MemberId"]),
                            ActionType = reader["ActionType"].ToString(),
                            ActionCategory = reader["ActionCategory"].ToString(),
                            Outcome = reader["Outcome"].ToString(),
                            CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                            CreatedBy = reader["CreatedBy"].ToString(),
                            IpAddress = reader["IpAddress"].ToString(),
                            UserAgent = reader["UserAgent"].ToString(),
                            Details = reader["Details"].ToString()
                        });
                    }
                }
            }
            return logs;
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
        public bool UpdateMember(int id,
                                 string username,
                                 string email,
                                 string fullname,
                                 DateTime? birthday,
                                 string genderStr, // "M"/"F" or null
                                 string address,
                                 bool isActive)
        {
            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();
                string sql = @"
                                UPDATE Users
                                SET 
                                    Username = @Username,
                                    Email = @Email,
                                    FullName = @FullName,
                                    Birthday = @Birthday,
                                    Gender = @Gender,
                                    Address = @Address,
                                    IsActive = @IsActive,
                                    UpdatedAt = SYSUTCDATETIME()
                                WHERE MemberId = @Id;
                            ";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@Username", username ?? "");
                    cmd.Parameters.AddWithValue("@Email", email ?? "");
                    cmd.Parameters.AddWithValue("@FullName", fullname ?? "");

                    if (birthday.HasValue)
                        cmd.Parameters.AddWithValue("@Birthday", birthday.Value);
                    else
                        cmd.Parameters.AddWithValue("@Birthday", DBNull.Value);

                    if (!string.IsNullOrEmpty(genderStr))
                        cmd.Parameters.AddWithValue("@Gender", genderStr);
                    else
                        cmd.Parameters.AddWithValue("@Gender", DBNull.Value);

                    cmd.Parameters.AddWithValue("@Address", address ?? "");
                    cmd.Parameters.AddWithValue("@IsActive", isActive);

                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
    }
}