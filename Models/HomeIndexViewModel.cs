using System;
using System.Collections.Generic;

namespace CarbonProject.Models
{
    public class HomeIndexViewModel
    {
        public int TotalCompanies { get; set; }
        public int TotalMembers { get; set; }
        public int ActiveMembers { get; set; }   // 最近 30 天內登入過
        public int RecentLogins { get; set; }    // 最近 7 天內登入次數
        public List<ActivityRecord> RecentActivities { get; set; } = new List<ActivityRecord>();    // 活動紀錄列表
        public class ActivityRecord
        {
            public DateTime ActionTime { get; set; }
            public string Username { get; set; }
            public string Action { get; set; }
        }
    }
}