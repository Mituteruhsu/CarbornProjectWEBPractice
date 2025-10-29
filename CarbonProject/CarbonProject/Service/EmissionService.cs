using CarbonProject.Controllers;
using CarbonProject.Data;
using CarbonProject.Models;
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
        private readonly IHttpContextAccessor _httpContextAccessor;

        public EmissionService(CarbonDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // 平均減碳目標百分比
        // Use -> Models/EFModels/CompanyEmissionTarget.cs
        // For -> Controllers/DashboardController.cs
        public async Task<decimal> GetAverageReductionPercentAsync()
        {
            return await _context.CompanyEmissionTargets
                .Where(t => t.ReductionPercent != null)
                .AverageAsync(t => t.ReductionPercent);
        }

        // 平均總排放量
        // Use -> Models/EFModels/CompanyEmissionTarget.cs
        public async Task<decimal> GetAverageTotalEmissionAsync()
        {
            return await _context.CompanyEmissionTargets
                .Where(e => e.TargetEmission != null)
                .AverageAsync(e => e.TargetEmission);
        }

        // 依年份分組，每年所有公司總排放量，所有公司平均
        // Use -> Models/EFModels/CompanyEmission.cs
        public async Task<List<YearlyEmissionAverage>> GetYearlyAverageEmissionsAsync()
        {
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
        // 新增公司排放紀錄（寫入 CompanyEmissions + ActivityLog）
        // Use -> Models/EFModels/CompanyEmission.cs
        // Use -> Models/EFModels/ActivityLog.cs
        public async Task<bool> AddCompanyEmissionAsync(int memberId, int companyId, CompanyEmission model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                model.CompanyId = companyId;
                model.CreatedAt = DateTime.Now;
                _context.CompanyEmissions.Add(model);
                await _context.SaveChangesAsync();

                // 取得目前登入資訊
                var http = _httpContextAccessor.HttpContext;
                var ip = http?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";
                var ua = http?.Request?.Headers["User-Agent"].ToString() ?? "unknown";

                // 寫入活動日誌
                var log = new ActivityLog
                {
                    MemberId = memberId,
                    CompanyId = companyId,
                    ActionType = "Create",
                    ActionCategory = "CompanyEmission",
                    ActionTime = DateTime.Now,
                    Outcome = "Success",
                    IpAddress = ip,
                    UserAgent = ua,
                    Source = "Web",
                    CorrelationId = Guid.NewGuid().ToString(),
                    Details = $"新增排放資料 Year={model.Year}, Quarter={model.Quarter}, Total={model.TotalEmission}",
                    CreatedBy = $"Member {memberId}",
                    CreatedAt = DateTime.Now
                };

                _context.ActivityLogs.Add(log);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                // 若失敗也記錄 log
                var failLog = new ActivityLog
                {
                    MemberId = memberId,
                    CompanyId = companyId,
                    ActionType = "Create",
                    ActionCategory = "CompanyEmission",
                    ActionTime = DateTime.Now,
                    Outcome = "Fail",
                    Details = ex.Message,
                    CreatedBy = $"Member {memberId}",
                    CreatedAt = DateTime.Now
                };
                _context.ActivityLogs.Add(failLog);
                await _context.SaveChangesAsync();

                return false;
            }
        }
        // 新增公司減碳目標（寫入 CompanyEmissionTargets + ActivityLog）
        // Use -> Models/EFModels/CompanyEmissionTarget.cs
        // Use -> Models/EFModels/ActivityLog.cs
        public async Task<bool> AddEmissionTargetAsync(int memberId, int companyId, CompanyEmissionTarget model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                model.CompanyId = companyId;
                model.CreatedAt = DateTime.Now;
                _context.CompanyEmissionTargets.Add(model);
                await _context.SaveChangesAsync();

                var http = _httpContextAccessor.HttpContext;
                var ip = http?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";
                var ua = http?.Request?.Headers["User-Agent"].ToString() ?? "unknown";

                var log = new ActivityLog
                {
                    MemberId = memberId,
                    CompanyId = companyId,
                    ActionType = "Create",
                    ActionCategory = "CompanyEmissionTarget",
                    ActionTime = DateTime.Now,
                    Outcome = "Success",
                    IpAddress = ip,
                    UserAgent = ua,
                    Source = "Web",
                    CorrelationId = Guid.NewGuid().ToString(),
                    Details = $"新增減碳目標 TargetYear={model.TargetYear}, Reduction={model.ReductionPercent}%",
                    CreatedBy = $"Member {memberId}",
                    CreatedAt = DateTime.Now
                };

                _context.ActivityLogs.Add(log);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                var failLog = new ActivityLog
                {
                    MemberId = memberId,
                    CompanyId = companyId,
                    ActionType = "Create",
                    ActionCategory = "CompanyEmissionTarget",
                    ActionTime = DateTime.Now,
                    Outcome = "Fail",
                    Details = ex.Message,
                    CreatedBy = $"Member {memberId}",
                    CreatedAt = DateTime.Now
                };
                _context.ActivityLogs.Add(failLog);
                await _context.SaveChangesAsync();

                return false;
            }
        }
    }
}