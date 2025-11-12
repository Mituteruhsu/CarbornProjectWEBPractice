using CarbonProject.Models;
using CarbonProject.Models.EFModels;
using CarbonProject.Service.Logging;
using CsvHelper;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.Design;
using System.Data;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace CarbonProject.Controllers
{
    public class CefController : Controller
    {
        private readonly ILogger<CefController> _logger;
        private readonly ActivityLogService _activityLog;
        public CefController(ILogger<CefController> logger, ActivityLogService activityLog)
        {
            _logger = logger;
            _activityLog = activityLog;
        }
        // ---------- ActivityLog Helper ----------
        // For Pages below
        private async Task LogActivityAsync(string actionType, string actionCategory = "PageView", string outcome = "Success")
        {
            int? memberId = HttpContext.Session.GetInt32("MemberId");
            int? companyId = HttpContext.Session.GetInt32("CompanyId");
            string username = HttpContext.Session.GetString("Username") ?? "Anonymous";

            await _activityLog.LogAsync(
                memberId: memberId,
                companyId: companyId,
                actionType: actionType,
                actionCategory: actionCategory,
                outcome: outcome,
                ip: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString(),
                createdBy: username,
                detailsObj: new { Controller = "Dashboard", Action = actionType }
            );
        }
        [HttpGet]
        public async Task<IActionResult> Index(string search)
        {
            var cefItemsList = GetCefItemsFromCSV();

            if (!string.IsNullOrEmpty(search))
            {
                cefItemsList = cefItemsList
                    .Where(c => c.name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                c.departmentname.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            else
            {
                if (cefItemsList.Any())
                {
                    int latestYear = cefItemsList.Max(c => c.announcementyear);
                    cefItemsList = cefItemsList
                        .Where(c => c.announcementyear == latestYear)
                        .ToList();
                }
            }

            var viewModel = new CefItemsViewModel
            {
                items = cefItemsList
            };

            // Use ActivityLog
            await LogActivityAsync("Dashboard.SearchPage");

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IndexPost(string search)
        {
            var cefItemsList = GetCefItemsFromCSV();

            if (!string.IsNullOrEmpty(search))
            {
                cefItemsList = cefItemsList
                    .Where(c => c.name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                c.departmentname.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            else
            {
                if (cefItemsList.Any())
                {
                    int latestYear = cefItemsList.Max(c => c.announcementyear);
                    cefItemsList = cefItemsList
                        .Where(c => c.announcementyear == latestYear)
                        .ToList();
                }
            }

            var viewModel = new CefItemsViewModel
            {
                items = cefItemsList
            };

            // Use ActivityLog
            await LogActivityAsync("Dashboard.SearchBtn");

            return View("Index", viewModel);
        }
        // 讀取 CSV
        // Use -> wwwroot/data/cef_items.csv

        public static List<CefItems> GetCefItemsFromCSV()
        {
            string filePath = "wwwroot/data/cef_items.csv";
            List<CefItems> cefItemsList = new List<CefItems>();
            StreamReader stream = new StreamReader(filePath);
            CsvReader csvReader = new CsvReader(stream, System.Globalization.CultureInfo.InvariantCulture);
            foreach (var record in csvReader.GetRecords<CefItems>())
            {
                cefItemsList.Add(record);
            }
            return cefItemsList;
        }
    }
}
