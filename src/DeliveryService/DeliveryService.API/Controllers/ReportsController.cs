using DeliveryService.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliveryService.API.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly IDeliveryReportService _deliveryReportService;

    public ReportsController(IDeliveryReportService deliveryReportService)
    {
        _deliveryReportService = deliveryReportService;
    }

    [HttpGet("delivery-success")]
    [Authorize(Roles = "Admin,Dispatcher")]
    public async Task<IActionResult> GetDeliverySuccessReport(
        [FromQuery] DateTime? startDateUtc = null,
        [FromQuery] DateTime? endDateUtc = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var summary = await _deliveryReportService.GetDeliverySuccessRateAsync(startDateUtc, endDateUtc, cancellationToken);
            return Ok(new { success = true, data = summary, meta = new { startDateUtc, endDateUtc } });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message, errors = Array.Empty<string>() });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Failed to fetch delivery success report", errors = new[] { ex.Message } });
        }
    }
}