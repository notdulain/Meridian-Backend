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
    public async Task<IActionResult> GetFuelCostReport(CancellationToken cancellationToken = default)
    {
        try
        {
            var report = await _fuelCostReportService.GetFuelCostReportAsync(cancellationToken);
            return Ok(new { success = true, data = report });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Failed to fetch fuel cost report", errors = new[] { ex.Message } });
        }
    }
}