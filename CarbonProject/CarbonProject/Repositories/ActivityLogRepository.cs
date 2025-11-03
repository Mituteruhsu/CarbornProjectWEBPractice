using CarbonProject.Helpers;
using CarbonProject.Models;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace CarbonProject.Repositories
{
    public class ActivityLogRepository
    {
        // 連線字串從 appsettings.json 取得
        private readonly string connStr;

        public ActivityLogRepository(IConfiguration configuration)
        {
            connStr = configuration.GetConnectionString("DefaultConnection");
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
                                al.CreatedBy,
                                al.IpAddress,
                                al.ActionType
                            FROM ActivityLog al
                            LEFT JOIN Users u ON al.MemberId = u.MemberId
                            ORDER BY al.ActionTime DESC";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Top", top);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // 動作顯示轉換成更可讀的中文
                            string actionType = reader["ActionType"].ToString();
                            string actionDisplay;
                            if (actionType == "Auth.Login.Success") actionDisplay = "登入系統";
                            else if (actionType == "Auth.Login.Failed") actionDisplay = "登入失敗";
                            else if (actionType == "Auth.Logout") actionDisplay = "登出系統";
                            else if (actionType == "HomePage.Index") actionDisplay = "進入首頁";
                            else if (actionType == "Home.View.Privacy") actionDisplay = "Privacy隱私權政策";
                            else if (actionType == "Home.View.Refrences") actionDisplay = "Refrences參考頁";
                            else
                                actionDisplay = actionType; // 其他原樣顯示

                            string user = reader["CreatedBy"]?.ToString();
                            string ipAddress = reader["IpAddress"]?.ToString();
                            // 遮蔽最後一段 (避免識別個人)
                            var maskedIp = Regex.Replace(ipAddress, @"\.\d+\.\d+$", ".***.***");
                            string actionUser;
                            if (user == "Anonymous")
                                actionUser = $"匿名:{maskedIp}";
                            else
                                actionUser = user;

                            // UTC -> 台北
                            DateTime utcTime = Convert.ToDateTime(reader["ActionTime"]);
                            DateTime taipeiTime = TimeHelper.ToTaipeiTime(utcTime);

                            list.Add(new HomeIndexViewModel.ActivityRecord
                            {
                                Username = actionUser,
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