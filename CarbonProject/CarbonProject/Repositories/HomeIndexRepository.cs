using CarbonProject.Helpers;
using CarbonProject.Models;
using Microsoft.Data.SqlClient;
using System.Diagnostics;

namespace CarbonProject.Repositories
{
    public class HomeIndexRepository
    {
        private readonly string connStr;
        private readonly ActivityLogRepository _activityLogRepository;
        public HomeIndexRepository(IConfiguration configuration, ActivityLogRepository activityLogRepository)
        {
            connStr = configuration.GetConnectionString("DefaultConnection");
            _activityLogRepository = activityLogRepository;
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
                conn.Close();
            }
            // 5. 最近 20 筆活動紀錄
            //From -> Repositories/ActivityLogRepository.cs
            model.RecentActivities = _activityLogRepository.GetRecentActivities(20);

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