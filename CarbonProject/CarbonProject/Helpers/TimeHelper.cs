using System;

namespace CarbonProject.Helpers
{
    public static class TimeHelper
    {
        // 將 UTC 時間轉為台北時間
        public static DateTime ToTaipeiTime(DateTime utcTime)
        {
            if (utcTime.Kind == DateTimeKind.Unspecified)
            {
                // 若未指定，先當作 UTC
                utcTime = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);
            }
            else if (utcTime.Kind == DateTimeKind.Local)
            {
                // 若來源是 Local，先轉為 UTC 再轉台北
                utcTime = utcTime.ToUniversalTime();
            }

            TimeZoneInfo taipeiZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(utcTime, taipeiZone);
        }
    }
}