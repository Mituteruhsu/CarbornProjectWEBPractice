using Microsoft.AspNetCore.Mvc;

public class CarbonCalculatorController : Controller
{
    private readonly CarbonFactorImportService _service;

    public CarbonCalculatorController(CarbonFactorImportService service)
    {
        _service = service;
    }

    // 顯示最新資料
    public async Task<IActionResult> Index()
    {
        var factors = await _service.GetAllFactors();
        return View(factors);
    }
    [HttpGet]
    public async Task<IActionResult> GetEmissionFactors()
    {
        var factors = await _service.GetAllFactors();

        var result = factors.Select(f => new {
            name = f.Name,
            unit = f.Unit,
            factor = f.Coe,
            year = f.AnnouncementYear
        });

        return Json(result);
    }

}
