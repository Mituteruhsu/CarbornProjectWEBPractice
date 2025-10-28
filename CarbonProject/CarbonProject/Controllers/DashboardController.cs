using CarbonProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Text.Json;   // 用來轉 JSON 格式
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace CarbonProject.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}