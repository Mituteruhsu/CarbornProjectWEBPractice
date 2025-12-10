using CarbonProject.Controllers;
using Microsoft.AspNetCore.Mvc;

public class CarbonCalculatorController : Controller
{
    private readonly ILogger<CarbonCalculatorController> _logger;
    private readonly CarbonFactorImportService _service;

    public CarbonCalculatorController(ILogger<CarbonCalculatorController> logger, CarbonFactorImportService service)
    {
        _logger = logger;
        _service = service;
    }

    // 顯示最新資料
    public async Task<IActionResult> Index()
    {
        _logger.LogInformation("Index 方法被呼叫");
        var factors = await _service.GetAllFactors();
        return View(factors);
    }
}
