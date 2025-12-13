using CarbonProject.Models.Request;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

[Route("api/[controller]")]
[ApiController]
public class CarbonCalculationAPIController : ControllerBase
{
    private readonly CarbonCalculationService _service;

    public CarbonCalculationAPIController(CarbonCalculationService service)
    {
        _service = service;
    }

    [HttpPost("Save")]
    public async Task<IActionResult> Save([FromBody] List<SaveCarbonRequest> request)
    {
        Debug.WriteLine("===== CarbonCalculationAPIController.cs =====");
        if (!ModelState.IsValid)
            return BadRequest("資料格式錯誤");

        int? userId = HttpContext.Session.GetInt32("MemberId");
        string roles = HttpContext.Session.GetString("Roles") ?? "User";
        Debug.WriteLine($"userId: {userId}");
        Debug.WriteLine($"roles: {roles}");
        if (userId == null || userId == 0)
            return Unauthorized("使用者未登入");

        try
        {
            var count = await _service.SaveBatchAsync(
                userId.Value,
                roles,
                request,
                DateTime.Now.ToString("yyyy-MM-dd HH:mm")
            );
            return Ok(new { success = true, count });
        }
        catch (Exception ex)
        {
            Debug.WriteLine("❌ 儲存失敗：" + ex.Message);
            return StatusCode(500, ex.Message);
        }
    }
}