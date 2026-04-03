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

    /// <summary>Get the overall delivery success-rate summary for a date range.</summary>
    [HttpGet("delivery-success")]
    [Authorize(Roles = "Admin,Dispatcher,Manager")]
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

    /// <summary>
    /// Get delivery counts aggregated by time period for trend analysis.
    /// range: "daily" (default) | "weekly" | "monthly"
    /// from/to: optional UTC bounds; sensible defaults apply when omitted.
    /// </summary>
    [HttpGet("delivery-trends")]
    [Authorize(Roles = "Admin,Dispatcher,Manager")]
    public async Task<IActionResult> GetDeliveryTrends(
        [FromQuery] string range = "daily",
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        if (range is not ("daily" or "weekly" or "monthly"))
            return BadRequest(new { success = false, message = "range must be 'daily', 'weekly', or 'monthly'." });

        try
        {
            var trends = await _deliveryReportService.GetDeliveryTrendsAsync(range, from, to, cancellationToken);
            return Ok(new { success = true, data = trends, meta = new { range, from, to } });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message, errors = Array.Empty<string>() });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to fetch delivery trends", errors = new[] { ex.Message } });
        }
    }
}