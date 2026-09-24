using CafeApp.Business.Services.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CafeApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly ReportService _reportService;

    public ReportsController(ReportService reportService)
    {
        _reportService = reportService;
    }

    // Hem Kasiyer hem Müdür günün anlık durumunu görür
    [HttpGet("today-summary")]
    public async Task<IActionResult> GetTodaySummary()
    {
        var summary = await _reportService.GetTodayLiveSummaryAsync();
        return Ok(summary);
    }

    // Yalnızca Müdür gün sonu Z raporu alabilir
    [Authorize(Roles = "Manager")]
    [HttpPost("close-day-z-report")]
    public async Task<IActionResult> CloseDay()
    {
        var result = await _reportService.CloseDayAndGenerateZReportAsync();
        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(result);
    }
}