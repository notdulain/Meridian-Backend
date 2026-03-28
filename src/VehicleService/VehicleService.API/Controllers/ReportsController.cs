using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleService.API.Services;

namespace VehicleService.API.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly IVehicleService _vehicleService;

    public ReportsController(IVehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    [HttpGet("vehicle-utilization")]
    [Authorize(Roles = "Admin,Dispatcher")]
    public async Task<IActionResult> GetVehicleUtilizationReport([FromQuery] DateTime? startDateUtc = null, [FromQuery] DateTime? endDateUtc = null)
    {
        try
        {
            var report = await _vehicleService.GetVehicleUtilizationReportAsync(startDateUtc, endDateUtc);
            return Ok(new { success = true, data = report, meta = new { startDateUtc, endDateUtc } });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message, errors = Array.Empty<string>() });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Failed to fetch vehicle utilization report", errors = new[] { ex.Message } });
        }
    }
}