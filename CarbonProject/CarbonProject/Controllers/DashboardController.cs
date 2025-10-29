using CarbonProject.Data;
using CarbonProject.Models;
using CarbonProject.Models.EFModels;
using CarbonProject.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text.Json;   // 用來轉 JSON 格式
using System.Text.RegularExpressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace CarbonProject.Controllers
{
    // DashboardController 負責數據顯示、統計分析
    public class DashboardController : Controller
    {
        private readonly CarbonDbContext _context;          // From -> Data/CarbonDbContext.cs
        private readonly EmissionService _emissionService;  // From -> Service/EmissionService.cs
        public DashboardController(CarbonDbContext context, EmissionService emissionService)
        {
            _context = context;
            _emissionService = emissionService;
        }
        // 建立 ViewModel For -> "Index()"
        public class DashboardViewModel
        {
            public List<CompanyEmissionTarget> CompanyEmissionTargets { get; set; }
            public List<CompanyEmission> CompanyEmissions { get; set; }
            public decimal AvgReductionPercent { get; set; }
            public decimal AvgTotalEmission { get; set; }
            public List<YearlyEmissionAverage> YearlyEmissionAverages { get; set; }
        }
        public async Task<IActionResult> Index()
        {
            // 取得前 1000 筆資料
            var targets = await _context.CompanyEmissionTargets
                                        .OrderBy(t => t.TargetId)
                                        .Take(1000)
                                        .ToListAsync();

            var emissions = await _context.CompanyEmissions
                                        .OrderBy(e => e.EmissionId)
                                        .Take(1000)
                                        .ToListAsync();

            var avgReduction = await _emissionService
                                        .GetAverageReductionPercentAsync();
            var avgEmission = await _emissionService
                                        .GetAverageTotalEmissionAsync();
            // 年度平均資料
            var yearlyAverages = await _emissionService
                                        .GetYearlyAverageEmissionsAsync();

            // 封裝成 ViewModel Use -> "DashboardViewModel"
            // For -> Views/Dashboard/Index.cshtml
            var viewModel = new DashboardViewModel
            {
                CompanyEmissionTargets = targets,
                CompanyEmissions = emissions,
                AvgReductionPercent = avgReduction,
                AvgTotalEmission = avgEmission,
                YearlyEmissionAverages = yearlyAverages
            };

            return View(viewModel);
        }
        
        public IActionResult NameYourProject1()
        {
            return View();
        }
        public IActionResult NameYourProject2()
        {
            return View();
        }
        public IActionResult NameYourProject3()
        {
            return View();
        }
    }
}