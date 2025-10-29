using System;
using CarbonProject.Data;
using CarbonProject.Models;
using CarbonProject.Models.EFModels;
using System.Text.Json;

// 這裡架構 ActivityLogService (非靜態，用 DI 注入) Accont.cs
// Use -> Models/EFModels/ActivityLog.cs
// 須將以下這些規則加入 Controller
namespace CarbonProject.Services
{
    public class ActivityLogService
    {
        private readonly CarbonDbContext _context;

        public ActivityLogService(CarbonDbContext context)
        {
            _context = context;
        }

        // 所有記錄用 LogAsync() 寫入 EF Core，可序列化 detailsObj，保留原本 JSON 資料
        // 不再用靜態 ActivityLog.Write()
        public async Task LogAsync(
            int? memberId,
            int? companyId,
            string actionType,
            string actionCategory,
            string outcome,
            string ip = null,
            string userAgent = null,
            string source = "Web",
            string createdBy = null,
            object detailsObj = null)
        {
            var log = new ActivityLog
            {
                MemberId = memberId,
                CompanyId = companyId,
                ActionType = actionType,
                ActionCategory = actionCategory,
                Outcome = outcome,
                IpAddress = ip,
                UserAgent = userAgent,
                Source = source,
                CorrelationId = Guid.NewGuid().ToString(),
                Details = detailsObj != null ? JsonSerializer.Serialize(detailsObj) : null,
                CreatedBy = createdBy,
                ActionTime = DateTime.Now,
                CreatedAt = DateTime.Now
            };

            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}