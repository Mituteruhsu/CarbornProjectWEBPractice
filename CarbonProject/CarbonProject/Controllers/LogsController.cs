// Controllers/LogsController.cs
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CarbonProject.Data;
using CarbonProject.Models.EFModels;
using CarbonProject.Models.ViewModels;
using CarbonProject.Service.Logging;

namespace CarbonProject.Controllers
{
    public class LogsController : Controller
    {
        private readonly CarbonDbContext _context;
        private readonly ActivityLogService _activityLogService;

        public LogsController(CarbonDbContext context, ActivityLogService activityLogService)
        {
            _context = context;
            _activityLogService = activityLogService;
        }

        // Index -> 顯示 logs（支援 filter + 分頁）
        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] LogQueryModel q)
        {
            // 基本 Query
            var query = _context.ActivityLogs.AsQueryable();

            // 時間範圍（ActionTime）
            if (q.From.HasValue)
            {
                var fromUtc = q.From.Value.ToUniversalTime();
                query = query.Where(x => x.ActionTime >= fromUtc);
            }
            if (q.To.HasValue)
            {
                // To 的當天結束時間
                var toUtc = q.To.Value.ToUniversalTime().AddDays(1).AddTicks(-1);
                query = query.Where(x => x.ActionTime <= toUtc);
            }

            if (q.MemberId.HasValue)
            {
                query = query.Where(x => x.MemberId == q.MemberId.Value);
            }
            if (!string.IsNullOrEmpty(q.ActionCategory))
            {
                query = query.Where(x => x.ActionCategory == q.ActionCategory);
            }
            if (!string.IsNullOrEmpty(q.ActionType))
            {
                query = query.Where(x => x.ActionType == q.ActionType);
            }
            if (!string.IsNullOrEmpty(q.Outcome))
            {
                query = query.Where(x => x.Outcome == q.Outcome);
            }
            if (!string.IsNullOrEmpty(q.IpAddress))
            {
                query = query.Where(x => x.IpAddress.Contains(q.IpAddress));
            }
            if (!string.IsNullOrEmpty(q.Keyword))
            {
                // 關鍵字搜尋於 Details 欄位（JSON）或 ActionType / ActionCategory / UserAgent 等
                var kw = q.Keyword;
                query = query.Where(x =>
                    (x.Details != null && x.Details.Contains(kw)) ||
                    (x.ActionType != null && x.ActionType.Contains(kw)) ||
                    (x.ActionCategory != null && x.ActionCategory.Contains(kw)) ||
                    (x.UserAgent != null && x.UserAgent.Contains(kw))
                );
            }

            // 排序：最新在前
            query = query.OrderByDescending(x => x.ActionTime);

            // 分頁
            var total = await query.CountAsync();
            var page = Math.Max(1, q.Page);
            var pageSize = Math.Clamp(q.PageSize, 5, 200);
            var skip = (page - 1) * pageSize;

            var items = await query
                .Skip(skip)
                .Take(pageSize)
                .Select(x => new LogListViewModel
                {
                    LogId = x.LogId,
                    MemberId = x.MemberId,
                    CompanyId = x.CompanyId,
                    ActionType = x.ActionType,
                    ActionCategory = x.ActionCategory,
                    ActionTime = x.ActionTime,
                    Outcome = x.Outcome,
                    IpAddress = x.IpAddress,
                    UserAgent = x.UserAgent,
                    Source = x.Source
                    // 若你有 Member 資料表可在此 join 並填入 MemberName
                })
                .ToListAsync();

            // 傳回 view model + 分頁資訊（簡單）
            ViewBag.TotalCount = total;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.Query = q; // 回填搜尋欄

            return View(items);
        }

        // Details API -> 回傳 JSON（供 Modal 呼叫）
        [HttpGet]
        public async Task<IActionResult> Details(long id)
        {
            var log = await _context.ActivityLogs.FirstOrDefaultAsync(x => x.LogId == id);
            if (log == null) return NotFound();

            // 嘗試美化 Details (JSON pretty)
            string prettyDetails = log.Details;
            if (!string.IsNullOrEmpty(log.Details))
            {
                try
                {
                    using var doc = JsonDocument.Parse(log.Details);
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    prettyDetails = JsonSerializer.Serialize(doc.RootElement, options);
                }
                catch
                {
                    // 如果解析失敗（不是 JSON），就保留原文
                    prettyDetails = log.Details;
                }
            }

            var vm = new LogDetailViewModel
            {
                LogId = log.LogId,
                MemberId = log.MemberId,
                CompanyId = log.CompanyId,
                ActionType = log.ActionType,
                ActionCategory = log.ActionCategory,
                ActionTime = log.ActionTime,
                Outcome = log.Outcome,
                IpAddress = log.IpAddress,
                UserAgent = log.UserAgent,
                Source = log.Source,
                CorrelationId = log.CorrelationId,
                DetailsJson = prettyDetails,
                CreatedBy = log.CreatedBy,
                CreatedAt = log.CreatedAt
            };

            // 回傳 JSON（會被前端 JS 使用）
            return Json(new
            {
                logId = vm.LogId,
                memberId = vm.MemberId,
                companyId = vm.CompanyId,
                actionType = vm.ActionType,
                actionCategory = vm.ActionCategory,
                actionTime = vm.ActionTime.ToString("yyyy-MM-dd HH:mm:ss"),
                outcome = vm.Outcome,
                ipAddress = vm.IpAddress,
                userAgent = vm.UserAgent,
                source = vm.Source,
                correlationId = vm.CorrelationId,
                detailsJson = vm.DetailsJson,
                createdBy = vm.CreatedBy,
                createdAt = vm.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }

        // Export CSV (簡易實作，可按需擴充) - 可選
        [HttpGet]
        public async Task<IActionResult> Export([FromQuery] LogQueryModel q)
        {
            var query = _context.ActivityLogs.AsQueryable();

            if (q.From.HasValue) query = query.Where(x => x.ActionTime >= q.From.Value.ToUniversalTime());
            if (q.To.HasValue) query = query.Where(x => x.ActionTime <= q.To.Value.ToUniversalTime().AddDays(1).AddTicks(-1));
            if (q.MemberId.HasValue) query = query.Where(x => x.MemberId == q.MemberId.Value);
            if (!string.IsNullOrEmpty(q.ActionCategory)) query = query.Where(x => x.ActionCategory == q.ActionCategory);
            if (!string.IsNullOrEmpty(q.ActionType)) query = query.Where(x => x.ActionType == q.ActionType);
            if (!string.IsNullOrEmpty(q.Outcome)) query = query.Where(x => x.Outcome == q.Outcome);
            if (!string.IsNullOrEmpty(q.Keyword)) query = query.Where(x => (x.Details != null && x.Details.Contains(q.Keyword)));

            var list = await query.OrderByDescending(x => x.ActionTime).ToListAsync();

            var csv = new System.Text.StringBuilder();
            csv.AppendLine("LogId,ActionTime,MemberId,CompanyId,ActionCategory,ActionType,Outcome,IpAddress,Source,CreatedBy");
            foreach (var l in list)
            {
                var line = $"{l.LogId},\"{l.ActionTime:yyyy-MM-dd HH:mm:ss}\",{l.MemberId},{l.CompanyId},\"{EscapeCsv(l.ActionCategory)}\",\"{EscapeCsv(l.ActionType)}\",\"{EscapeCsv(l.Outcome)}\",\"{EscapeCsv(l.IpAddress)}\",\"{EscapeCsv(l.Source)}\",\"{EscapeCsv(l.CreatedBy)}\"";
                csv.AppendLine(line);
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"ActivityLogs_{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
        }

        private string EscapeCsv(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            return input.Replace("\"", "\"\"");
        }
    }
}
