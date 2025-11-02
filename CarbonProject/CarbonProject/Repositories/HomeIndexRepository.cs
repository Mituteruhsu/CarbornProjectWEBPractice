using CarbonProject.Helpers;
using CarbonProject.Models;
using Microsoft.Data.SqlClient;
using System.Diagnostics;

namespace CarbonProject.Repositories
{
    public class HomeIndexRepository
    {
        private readonly string connStr;

        public HomeIndexRepository(IConfiguration configuration)
        {
            connStr = configuration.GetConnectionString("DefaultConnection");
        }

        // 取得 Dashboard 數據
        public HomeIndexViewModel GetIndexData()
        {
            var model = new HomeIndexViewModel();

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                // 1.公司總數
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Companies", conn))
                {
                    model.TotalCompanies = (int)cmd.ExecuteScalar();
                }

                // 2.總會員數
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Users", conn))
                {
                    model.TotalMembers = (int)cmd.ExecuteScalar();
                }

                // 3. 活躍會員：最近 30 天內有成功登入的會員數
                string activeSql = @"
                    SELECT COUNT(DISTINCT MemberId) 
                    FROM ActivityLog 
                    WHERE ActionType = 'Auth.Login.Success' 
                      AND ActionTime >= DATEADD(DAY, -30, SYSUTCDATETIME())";
                using (SqlCommand cmd = new SqlCommand(activeSql, conn))
                {
                    model.ActiveMembers = (int)cmd.ExecuteScalar();
                }

                // 4. 最近 7 天登入次數
                string recentSql = @"
                    SELECT COUNT(*) 
                    FROM ActivityLog 
                    WHERE ActionType = 'Auth.Login.Success' 
                      AND ActionTime >= DATEADD(DAY, -7, SYSUTCDATETIME())";
                using (SqlCommand cmd = new SqlCommand(recentSql, conn))
                {
                    model.RecentLogins = (int)cmd.ExecuteScalar();
                }

                // 5. 最近 20 筆活動紀錄
                string recentActSql = @"
                    SELECT TOP 20 al.ActionTime, u.Username, al.ActionType
                    FROM ActivityLog al
                    LEFT JOIN Users u ON al.MemberId = u.MemberId
                    ORDER BY al.ActionTime DESC";

                using (SqlCommand cmd = new SqlCommand(recentActSql, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string actionType = reader["ActionType"].ToString();

                        // 轉換為可讀文字 (if/else)
                        string actionDisplay;
                        if (actionType == "Auth.Login.Success")
                        {
                            actionDisplay = "登入系統";
                        }
                        else if (actionType == "Auth.Login.Failed")
                        {
                            actionDisplay = "登入失敗";
                        }
                        else if (actionType == "Auth.Logout")
                        {
                            actionDisplay = "登出系統";
                        }
                        else
                        {
                            actionDisplay = actionType; // 其他原樣顯示
                        }

                        model.RecentActivities.Add(new HomeIndexViewModel.ActivityRecord
                        {
                        // Use -> Helpers/TimeHelper.cs
                        ActionTime = TimeHelper.ToTaipeiTime(Convert.ToDateTime(reader["ActionTime"])), // UTC -> 台北
                        Username = reader["Username"]?.ToString() ?? "匿名",
                        Action = actionDisplay
                        });
                    }
                }

                conn.Close();
                return model;
            }

            return model;
        }

        // 取得最近 N 天登入統計給 Chart.js
        public (List<string> Labels, List<int> Counts) GetRecentLogins(int days = 7)
        {
            var labels = new List<string>();
            var counts = new List<int>();

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                for (int i = days - 1; i >= 0; i--)
                {
                    DateTime date = DateTime.UtcNow.Date.AddDays(-i); // UTC
                    string sql = @"
                        SELECT COUNT(*) 
                        FROM ActivityLog 
                        WHERE ActionType='Auth.Login.Success' 
                          AND CAST(ActionTime AS DATE) = @Date";
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Date", date);
                        int count = (int)cmd.ExecuteScalar();
                        labels.Add(TimeHelper.ToTaipeiTime(date).ToString("MM/dd")); // 顯示台北
                        counts.Add(count);
                    }
                }
                conn.Close();
            }
            return (labels, counts);
        }
    }
}