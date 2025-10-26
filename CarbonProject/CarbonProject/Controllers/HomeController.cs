using System.Diagnostics;
using CarbonProject.Models;
using Microsoft.AspNetCore.Mvc;

namespace CarbonProject.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            var model = new DashboardViewModel
            {
                TotalCompanies = 12,
                TotalMembers = 58,
                ActiveMembers = 47,
                RecentLogins = 23
            };
            return View(model);
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
