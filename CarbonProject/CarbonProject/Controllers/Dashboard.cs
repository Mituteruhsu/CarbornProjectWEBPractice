using Microsoft.AspNetCore.Mvc;
using CarbonProject.Models;

namespace CarbonProject.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            // 模擬資料
            var model = new DashboardViewModel
            {
                TotalCompanies = 12,
                TotalMembers = 58,
                ActiveMembers = 47,
                RecentLogins = 23
            };

            return View(model);
        }
    }
}