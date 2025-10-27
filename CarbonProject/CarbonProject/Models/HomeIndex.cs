using Microsoft.Data.SqlClient;

namespace CarbonProject.Models
{
    public class HomeIndexViewModel
    {
        public int TotalCompanies { get; set; }
        public int TotalMembers { get; set; }
        public int ActiveMembers { get; set; }   // 最近 30 天內登入過
        public int RecentLogins { get; set; }    // 最近 7 天內登入次數

        private static string connStr;

        public static void Init(IConfiguration configuration)
        {
            connStr = configuration.GetConnectionString("DefaultConnection");
        }

        // 取得 Dashboard 數據
        public static HomeIndexViewModel GetIndexData()
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

                // 3.最近 30 天內登入過的會員（活躍會員）
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM Users WHERE LastLoginAt >= DATEADD(DAY, -30, GETDATE())", conn))
                {
                    model.ActiveMembers = (int)cmd.ExecuteScalar();
                }

                // 4.最近 7 日登入過的登入次數
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM Users WHERE LastLoginAt >= DATEADD(DAY, -7, GETDATE())", conn))
                {
                    model.RecentLogins = (int)cmd.ExecuteScalar();
                }

                conn.Close();
            }

            return model;
        }
    }
}