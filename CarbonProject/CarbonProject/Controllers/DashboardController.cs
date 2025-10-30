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

        // （選用）回傳 JSON 的 API（讓前端可延遲載入）
        public async Task<IActionResult> GetChartsData()
        {
            var vm = new DashboardChartsViewModel();

            vm.YearlyTotals = await _context.CompanyEmissions
                .Where(e => e.TotalEmission != null)
                .GroupBy(e => e.Year)
                .Select(g => new YearlyTotalDto
                {
                    Year = g.Key,
                    TotalEmission = g.Sum(x => x.TotalEmission)
                })
                .OrderBy(x => x.Year)
                .ToListAsync();

            vm.YearlyAverages = await _context.CompanyEmissions
                .Where(e => e.TotalEmission != null)
                .GroupBy(e => e.Year)
                .Select(g => new YearlyAvgPerCompanyDto
                {
                    Year = g.Key,
                    AvgPerCompany = g.Sum(x => x.TotalEmission)
                                     / (g.Select(x => x.CompanyId).Distinct().Count())
                })
                .OrderBy(x => x.Year)
                .ToListAsync();

            var latestYear = await _context.CompanyEmissions
                .Where(e => e.TotalEmission != null)
                .MaxAsync(e => (int?)e.Year) ?? 0;

            if (latestYear > 0)
            {
                var scopeTotals = await _context.CompanyEmissions
                    .Where(e => e.Year == latestYear)
                    .GroupBy(e => 1)
                    .Select(g => new
                    {
                        Scope1 = g.Sum(x => x.Scope1Emission),
                        Scope2 = g.Sum(x => x.Scope2Emission),
                        Scope3 = g.Sum(x => x.Scope3Emission)
                    })
                    .FirstOrDefaultAsync();

                if (scopeTotals != null)
                {
                    vm.LatestScopeShares = new List<ScopeShareDto>
                {
                    new ScopeShareDto{ Scope = "Scope1", Value = scopeTotals.Scope1 },
                    new ScopeShareDto{ Scope = "Scope2", Value = scopeTotals.Scope2 },
                    new ScopeShareDto{ Scope = "Scope3", Value = scopeTotals.Scope3 }
                };
                }

                vm.TopCompaniesLatest = await _context.CompanyEmissions
                    .Where(e => e.Year == latestYear && e.TotalEmission != null)
                    .GroupBy(e => e.CompanyId)
                    .Select(g => new TopCompanyDto
                    {
                        CompanyId = g.Key,
                        TotalEmission = g.Sum(x => x.TotalEmission)
                    })
                    .OrderByDescending(x => x.TotalEmission)
                    .Take(10)
                    .ToListAsync();
            }

            // 回傳 JSON（前端用 fetch）
            return Json(vm);
        }
        public IActionResult CALCULATLINK()
        {
            return View();
        }
        // ==== Demo顯示圖表 ====
        public async Task<IActionResult> Report()
        {
            var vm = new DashboardChartsViewModel();

            // 年度總排放量
            vm.YearlyTotals = await _context.CompanyEmissions
                .Where(e => e.TotalEmission != null)
                .GroupBy(e => e.Year)
                .Select(g => new YearlyTotalDto
                {
                    Year = g.Key,
                    TotalEmission = g.Sum(x => x.TotalEmission)
                })
                .OrderBy(x => x.Year)
                .ToListAsync();

            // 每公司年度平均
            vm.YearlyAverages = await _context.CompanyEmissions
                .Where(e => e.TotalEmission != null)
                .GroupBy(e => e.Year)
                .Select(g => new YearlyAvgPerCompanyDto
                {
                    Year = g.Key,
                    AvgPerCompany = g.Sum(x => x.TotalEmission) / (g.Select(x => x.CompanyId).Distinct().Count())
                })
                .OrderBy(x => x.Year)
                .ToListAsync();

            // 最新年度範疇比例
            var latestYear = await _context.CompanyEmissions
                .Where(e => e.TotalEmission != null)
                .MaxAsync(e => (int?)e.Year) ?? 0;

            if (latestYear > 0)
            {
                var scopeTotals = await _context.CompanyEmissions
                    .Where(e => e.Year == latestYear)
                    .GroupBy(e => 1)
                    .Select(g => new
                    {
                        Scope1 = g.Sum(x => x.Scope1Emission),
                        Scope2 = g.Sum(x => x.Scope2Emission),
                        Scope3 = g.Sum(x => x.Scope3Emission)
                    })
                    .FirstOrDefaultAsync();

                if (scopeTotals != null)
                {
                    vm.LatestScopeShares = new List<ScopeShareDto>
            {
                new ScopeShareDto { Scope = "Scope1", Value = scopeTotals.Scope1 },
                new ScopeShareDto { Scope = "Scope2", Value = scopeTotals.Scope2 },
                new ScopeShareDto { Scope = "Scope3", Value = scopeTotals.Scope3 }
            };
                }

                // 最新年度前 10 名公司總排放
                vm.TopCompaniesLatest = await _context.CompanyEmissions
                    .Where(e => e.Year == latestYear && e.TotalEmission != null)
                    .GroupBy(e => e.CompanyId)
                    .Select(g => new TopCompanyDto
                    {
                        CompanyId = g.Key,
                        TotalEmission = g.Sum(x => x.TotalEmission)
                    })
                    .OrderByDescending(x => x.TotalEmission)
                    .Take(10)
                    .ToListAsync();
            }

            return View(vm);
        }
        public IActionResult NameYourProject3()
        {
            return View();
        }
    }
}