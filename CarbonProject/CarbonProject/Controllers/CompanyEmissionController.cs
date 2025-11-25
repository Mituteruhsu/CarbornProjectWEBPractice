using CarbonProject.Data;
using CarbonProject.Models.EFModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarbonProject.Controllers
{
    // CarbonController 負責 CRUD 操作（新增 / 編輯 / 刪除排放資料與目標）
    public class CompanyEmissionController : Controller
    {
        private readonly CarbonDbContext _context;

        public CompanyEmissionController(CarbonDbContext context)
        {
            _context = context;
        }

        // ========== 新增排放量 ==========
        // For -> Views/Dashboard/CreateEmission.cshtml
        [HttpGet]
        public IActionResult CreateEmission()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEmission(CompanyEmission model)
        {
            if (!ModelState.IsValid)
                return View(model);

            model.CreatedAt = DateTime.UtcNow;
            model.UpdatedAt = DateTime.UtcNow;
            _context.CompanyEmissions.Add(model);
            await _context.SaveChangesAsync();

            await LogActivity("CreateEmission", $"新增排放量紀錄：公司ID={model.CompanyId}, 年度={model.Year}");
            TempData["Success"] = "排放量資料已新增成功！";
            return RedirectToAction("Index", "Dashboard");
        }

        // ========== 編輯排放量 ==========
        // For -> Views/Dashboard/EditEmission.cshtml
        [HttpGet]
        public async Task<IActionResult> EditEmission(int id)
        {
            var emission = await _context.CompanyEmissions.FindAsync(id);
            if (emission == null) return NotFound();
            return View(emission);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEmission(CompanyEmission model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var existing = await _context.CompanyEmissions.FindAsync(model.EmissionId);
            if (existing == null) return NotFound();

            existing.Scope1Emission = model.Scope1Emission;
            existing.Scope2Emission = model.Scope2Emission;
            existing.Scope3Emission = model.Scope3Emission;
            existing.TotalEmission = model.TotalEmission;
            existing.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await LogActivity("EditEmission", $"編輯排放量紀錄：公司ID={model.CompanyId}, 年度={model.Year}");
            TempData["Success"] = "排放量資料已更新！";
            return RedirectToAction("Index", "Dashboard");
        }

        // ========== 刪除 ==========
        // For -> Views/Dashboard/EditEmission.cshtml
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEmission(int id)
        {
            var item = await _context.CompanyEmissions.FindAsync(id);
            if (item == null) return NotFound();

            _context.CompanyEmissions.Remove(item);
            await _context.SaveChangesAsync();
            await LogActivity("DeleteEmission", $"刪除排放量紀錄 ID={id}");
            return RedirectToAction("Index", "Dashboard");
        }

        // ========== 共用活動紀錄 ==========
        // For -> Views/Dashboard/_FormStyles.cshtml
        private async Task LogActivity(string action, string details)
        {
            var log = new ActivityLog
            {
                MemberId = 1, // 這裡可改為真實登入者ID
                CompanyId = 1,
                ActionType = action,
                ActionCategory = "Carbon",
                ActionTime = DateTime.UtcNow,
                Outcome = "Success",
                Source = "CarbonController",
                Details = details,
                CreatedBy = "System",
                CreatedAt = DateTime.UtcNow
            };
            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}