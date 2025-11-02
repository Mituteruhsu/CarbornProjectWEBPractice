using System;
using CarbonProject.Data;
using CarbonProject.Models;
using CarbonProject.Models.EFModels;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

// 這裡架構 ActivityLogService (非靜態，用 DI 注入) Accont.cs
// Use -> Models/EFModels/ActivityLog.cs
// 須將以下這些規則加入 Controller
namespace CarbonProject.Services
{
    public class ActivityLogService
    {
        private readonly CarbonDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ActivityLogService(CarbonDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
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
            // 若沒傳入 IP 或 UserAgent，從 HttpContext 自動取
            ip ??= _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
            userAgent ??= _httpContextAccessor.HttpContext?.Request?.Headers["User-Agent"].ToString();

            // 自動生成 CorrelationId（每個請求唯一）
            // var correlationId = _httpContextAccessor.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString();

            // 嘗試用 TraceIdentifier 轉 GUID，如果失敗就生成新的 GUID
            Guid correlationId;
            if (!Guid.TryParse(_httpContextAccessor.HttpContext?.TraceIdentifier, out correlationId))
            {
                correlationId = Guid.NewGuid();
            }

            // 序列化詳細資訊
            var detailsJson = detailsObj != null
                ? JsonSerializer.Serialize(detailsObj, new JsonSerializerOptions { WriteIndented = true })
                : null;

            if (companyId != null)
            {
                bool exists = await _context.Companies.AnyAsync(c => c.CompanyId == companyId.Value);
                if (!exists)
                {
                    // 不寫入不存在的 CompanyId，或設定為 null
                    companyId = null;
                }
            }

            var log = new ActivityLog
            {
                MemberId = memberId,
                CompanyId = companyId,
                ActionType = actionType,
                ActionCategory = actionCategory,
                Outcome = outcome,
                IpAddress = ip,
                UserAgent = userAgent,
                Source = source,    // 預設 Web，可依場景改成 "API" 或 "BackgroundJob"
                CorrelationId = correlationId,
                Details = detailsJson,
                CreatedBy = createdBy,
                ActionTime = DateTime.UtcNow, // 存 UTC
                CreatedAt = DateTime.UtcNow, // 存 UTC
            };

            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}