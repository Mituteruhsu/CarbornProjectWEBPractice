using CarbonProject.Controllers;
using CarbonProject.Data;
using CarbonProject.Models.EFModels;
using Microsoft.EntityFrameworkCore;

// 這裡架構計算規則或使用服務
// Use -> Models/EFModels/CompanyEmission.cs
// Use -> Models/EFModels/CompanyEmissionTarget.cs
// 須將以下這些規則加入 Controller
namespace CarbonProject.Services
{
    public class EmissionService
    {
        private readonly CarbonDbContext _context;

        public EmissionService(CarbonDbContext context)
        {
            _context = context;
        }

        // 平均減碳目標百分比
        // To -> Controllers/DashboardController.cs
        public async Task<decimal> GetAverageReductionPercentAsync()
        {
            return await _context.CompanyEmissionTargets
                .Where(t => t.ReductionPercent != null)
                .AverageAsync(t => t.ReductionPercent);
        }

        // 平均總排放量
        public async Task<decimal> GetAverageTotalEmissionAsync()
        {
            return await _context.CompanyEmissionTargets
                .Where(e => e.TargetEmission != null)
                .AverageAsync(e => e.TargetEmission);
        }
        public async Task<List<YearlyEmissionAverage>> GetYearlyAverageEmissionsAsync()
        {
            // 依年份分組，每年所有公司總排放量，所有公司平均
            var yearlyTotals = await _context.CompanyEmissions
                .Where(e => e.TotalEmission != null)
                .GroupBy(e => e.Year)
                .Select(g => new
                {
                    Year = g.Key,
                    CompanyCount = g.Select(e => e.CompanyId).Distinct().Count(),
                    TotalEmissionSum = g.Sum(e => e.TotalEmission),
                    avgOfSums = g.Sum(e => e.TotalEmission) / g.Select(e => e.CompanyId).Distinct().Count()
                })
                .ToListAsync();

            return yearlyTotals
                .Select(x => new YearlyEmissionAverage
                {
                    Year = x.Year,
                    CompanyCount = x.CompanyCount,
                    TotalEmissionSum = x.TotalEmissionSum,
                    AverageAcrossYears = x.avgOfSums
                })
                .ToList();
        }
    }
}