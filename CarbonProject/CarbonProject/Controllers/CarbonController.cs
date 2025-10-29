using CarbonProject.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarbonProject.Controllers
{
    public class CarbonController : Controller
    {
        private readonly CarbonDbContext _context;

        public CarbonController(CarbonDbContext context)
        {
            _context = context;
        }

        // 取得所有排放目標
        public async Task<IActionResult> EmissionTargets()
        {
            var targets = await _context.CompanyEmissionTargets
                                        .OrderBy(t => t.TargetId)
                                        .Take(1000)
                                        .ToListAsync();
            return View(targets);
        }

        // 取得所有排放量資料
        public async Task<IActionResult> Emissions()
        {
            var emissions = await _context.CompanyEmissions
                                          .OrderBy(e => e.EmissionId)
                                          .Take(1000)
                                          .ToListAsync();
            return View(emissions);
        }
    }
}