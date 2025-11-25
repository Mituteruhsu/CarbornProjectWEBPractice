using CarbonProject.Attributes;
using CarbonProject.Models.EFModels;
using CarbonProject.Repositories;
using CarbonProject.Service.Logging;
using Microsoft.AspNetCore.Mvc;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace CarbonProject.Controllers
{
    // 後端強制：只有 Admin 能進來
    [AuthorizeRole(roles: new[] { "Admin" })]
    public class AdminController : Controller
    {
        private readonly HomeIndexRepository _homeRepo;
        private readonly ActivityLogService _activityLog;
        public AdminController(ILogger<AdminController> logger, IConfiguration config, HomeIndexRepository homeRepo, ActivityLogService activityLog)
        {
            _homeRepo = homeRepo;
            _activityLog = activityLog;
        }
        // 管理後台首頁
        public async Task<IActionResult> Index()
        {
            return View();
        }
        // 回傳最近 7 天登入統計給 Chart.js
        public JsonResult GetLoginTrend(int days = 7)
        {
            var (labels, counts) = _homeRepo.GetRecentLogins(days);
            return Json(new { labels, counts });
        }

        // 儀表板頁面
        public async Task<IActionResult> Dashboard()
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

        // 管理權限頁面
        public IActionResult Permissions()
        {
            return View();
        }
    }
}
