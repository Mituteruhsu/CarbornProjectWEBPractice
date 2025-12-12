using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class CarbonCalculationAPIController : ControllerBase
{
    private readonly CarbonCalculationService _service;

    public CarbonCalculationAPIController(CarbonCalculationService service)
    {
        _service = service;
    }

    [AutoValidateAntiforgeryToken]
    [HttpPost("Save")]
    public async Task<IActionResult> Save([FromBody] SaveCarbonRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest("資料格式錯誤");

        int userId = int.Parse(HttpContext.Session.GetString("MemberId") ?? "0");
        string role = HttpContext.Session.GetString("Role") ?? "User";
        
        if (userId == 0)
            return Unauthorized("使用者未登入");

        await _service.SaveRecordAsync(
            userId,
            request.Name,
            request.InputValue,
            request.Factor,
            request.ResultValue,
            role
        );

        return Ok(new { success = true });
    }
}

public class SaveCarbonRequest
{
    public string Name { get; set; }
    public decimal InputValue { get; set; }
    public decimal Factor { get; set; }
    public decimal ResultValue { get; set; }
}
