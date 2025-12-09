using Microsoft.AspNetCore.Mvc;

public class CarbonFactorController : Controller
{
    private readonly CarbonFactorImportService _service;

    public CarbonFactorController(CarbonFactorImportService service)
    {
        _service = service;
    }

    // 顯示最新資料
    public async Task<IActionResult> Index()
    {
        var factors = await _service.GetAllFactors();
        return View(factors);
    }

    // 手動同步 API
    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> ManualSync()
    {
        var factors = await _service.FetchAll("cfp_p_02", "e9370020-f106-4efc-8521-a9cef11b10aa");
        int newCount = await _service.SyncToDb(factors);
        TempData["Message"] = $"同步完成，共新增 {newCount} 筆資料";
        return RedirectToAction("Index");
    }
}
