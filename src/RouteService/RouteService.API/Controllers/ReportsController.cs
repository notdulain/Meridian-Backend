using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteService.API.Services;

namespace RouteService.API.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly IFuelCostReportService _fuelCostReportService;

    public ReportsController(IFuelCostReportService fuelCostReportService)
    {
        _fuelCostReportService = fuelCostReportService;
    }

    [HttpGet("fuel-cost")]
    [Authorize(Roles = "Admin,Dispatcher,Manager")]
    public async Task<IActionResult> GetFuelCostReport(
        [FromQuery] int? vehicleId = null,
        [FromQuery] DateTime? startDateUtc = null,
        [FromQuery] DateTime? endDateUtc = null,
        CancellationToken cancellationToken = default)
    {
        if (startDateUtc.HasValue && endDateUtc.HasValue && endDateUtc.Value < startDateUtc.Value)
        {
            return BadRequest(new { success = false, message = "endDateUtc must be greater than or equal to startDateUtc.", errors = Array.Empty<string>() });
        }

        try
        {
            var report = await _fuelCostReportService.GetFuelCostReportAsync(vehicleId, startDateUtc, endDateUtc, cancellationToken);
            return Ok(new { success = true, data = report });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message, errors = Array.Empty<string>() });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Failed to fetch fuel cost report", errors = new[] { ex.Message } });
        }
    }
}