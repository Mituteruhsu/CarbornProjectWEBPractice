using CarbonProject.Models;
using Microsoft.AspNetCore.Mvc;
using System.Configuration;
using System.Diagnostics;

namespace CarbonProject.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _config;
        public HomeController(ILogger<HomeController> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;

            // 初始化連線字串
            HomeIndexViewModel.Init(config);
            ActivityLog.Init(config);
        }

        public IActionResult Index()
        {
            var model = HomeIndexViewModel.GetIndexData();
            return View(model);
        }
        // 回傳最近 7 天登入統計給 Chart.js
        public JsonResult GetLoginTrend(int days = 7)
        {
            var (labels, counts) = HomeIndexViewModel.GetRecentLogins(days);
            return Json(new { labels, counts });
        }
        public IActionResult Privacy()
        {
            return View();
        }
        public IActionResult Refrences()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
