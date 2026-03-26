using DriverService.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DriverService.API.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly IDriverService _driverService;

    public ReportsController(IDriverService driverService)
    {
        _driverService = driverService;
    }

    [HttpGet("driver-performance")]
    [Authorize(Roles = "Admin,Dispatcher")]
    public async Task<IActionResult> GetDriverPerformanceReport([FromQuery] DateTime? startDateUtc = null, [FromQuery] DateTime? endDateUtc = null)
    {
        try
        {
            var report = await _driverService.GetDriverPerformanceReportAsync(startDateUtc, endDateUtc);
            return Ok(new { success = true, data = report, meta = new { startDateUtc, endDateUtc } });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message, errors = Array.Empty<string>() });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Failed to fetch driver performance report", errors = new[] { ex.Message } });
        }
    }
}
