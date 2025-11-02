using CarbonProject.Models;
using CarbonProject.Repositories;
using CarbonProject.Services;
using Microsoft.AspNetCore.Mvc;
using System.Configuration;
using System.Diagnostics;

// From -> Service/ActivityLogService.cs
namespace CarbonProject.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _config;
        private readonly ActivityLogService _activityLog;
        private readonly HomeIndexRepository _homeRepo;
        public HomeController(ILogger<HomeController> logger, IConfiguration config, ActivityLogService activityLog, HomeIndexRepository homeRepo)
        {
            _logger = logger;
            _config = config;
            _activityLog = activityLog;
            _homeRepo = homeRepo;
        }

        // 在 Index 記錄瀏覽首頁事件
        // Include ActivityLogService
        // From -> Service/ActivityLogService.cs
        public async Task<IActionResult> Index()
        {
            var model = _homeRepo.GetIndexData();

            // 取得 nullable MemberId 和 CompanyId
            int? memberId = HttpContext.Session.GetInt32("MemberId");
            int? companyId = HttpContext.Session.GetInt32("CompanyId");

            // 統一取得 Username（含匿名判斷）
            string username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrWhiteSpace(username))
                username = "Anonymous";


            // 記錄 ActivityLog
            await _activityLog.LogAsync(
                memberId: memberId,
                companyId: companyId,
                actionType: "HomePage.Index",
                actionCategory: "PageView",
                outcome: "Success",
                ip: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString(),
                createdBy: username,
                detailsObj: new { page = "Index" }
            );

            return View(model);
        }
        // 回傳最近 7 天登入統計給 Chart.js
        public JsonResult GetLoginTrend(int days = 7)
        {
            var (labels, counts) = _homeRepo.GetRecentLogins(days);
            return Json(new { labels, counts });
        }
        // Include ActivityLogService
        // From -> Service/ActivityLogService.cs
        public async Task<IActionResult> Privacy()
        {
            int? memberId = HttpContext.Session.GetInt32("MemberId");
            int? companyId = HttpContext.Session.GetInt32("CompanyId");

            string username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrWhiteSpace(username))
                username = "Anonymous";

            await _activityLog.LogAsync(
                memberId: memberId,
                companyId: companyId,
                actionType: "Home.View.Privacy",
                actionCategory: "PageView",
                outcome: "Success",
                ip: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString(),
                createdBy: username,
                detailsObj: new { page = "Privacy" }
            );
            return View();
        }
        // Include ActivityLogService
        // From -> Service/ActivityLogService.cs
        public async Task<IActionResult> Refrences()
        {
            int? memberId = HttpContext.Session.GetInt32("MemberId");
            int? companyId = HttpContext.Session.GetInt32("CompanyId");

            string username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrWhiteSpace(username))
                username = "Anonymous";

            await _activityLog.LogAsync(
                memberId: memberId,
                companyId: companyId,
                actionType: "Home.View.Refrences",
                actionCategory: "PageView",
                outcome: "Success",
                ip: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString(),
                createdBy: username,
                detailsObj: new { page = "Refrences" }
            );

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
