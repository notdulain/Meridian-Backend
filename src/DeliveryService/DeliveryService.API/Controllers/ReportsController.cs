using DeliveryService.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.IO;
using System.Text;

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

    [HttpGet("delivery-success/csv")]
    [Authorize(Roles = "Admin,Dispatcher,Manager")]
    public async Task<IActionResult> GetDeliverySuccessReportCsv(
        [FromQuery] DateTime? startDateUtc = null,
        [FromQuery] DateTime? endDateUtc = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var summary = await _deliveryReportService.GetDeliverySuccessRateAsync(startDateUtc, endDateUtc, cancellationToken);
            using var csvWriter = new StringWriter(CultureInfo.InvariantCulture);
            csvWriter.WriteLine("Delivered,Failed,Cancelled,Terminal Deliveries,Success Rate (%)");
            csvWriter.WriteLine(string.Join(",",
                summary.DeliveredCount,
                summary.FailedCount,
                summary.CancelledCount,
                summary.TerminalCount,
                summary.SuccessRatePercentage.ToString(CultureInfo.InvariantCulture)));

            return BuildCsvFile(csvWriter.ToString(), "delivery-success-report");
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

    [HttpGet("delivery-trends/csv")]
    [Authorize(Roles = "Admin,Dispatcher,Manager")]
    public async Task<IActionResult> GetDeliveryTrendsCsv(
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
            using var csvWriter = new StringWriter(CultureInfo.InvariantCulture);
            csvWriter.WriteLine("Period,Total Deliveries,Pending,Assigned,In Transit,Delivered,Failed,Canceled");

            foreach (var trend in trends)
            {
                csvWriter.WriteLine(string.Join(",",
                    EscapeCsv(trend.Period),
                    trend.Total,
                    trend.Pending,
                    trend.Assigned,
                    trend.InTransit,
                    trend.Delivered,
                    trend.Failed,
                    trend.Canceled));
            }

            return BuildCsvFile(csvWriter.ToString(), "delivery-trends-report");
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

    private FileContentResult BuildCsvFile(string csvContent, string fileNamePrefix)
    {
        var fileName = $"{fileNamePrefix}-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
        return File(Encoding.UTF8.GetBytes(csvContent), "application/octet-stream", fileName);
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var escaped = value.Replace("\"", "\"\"");
        return escaped.Contains(',') || escaped.Contains('"') || escaped.Contains('\n') || escaped.Contains('\r')
            ? $"\"{escaped}\""
            : escaped;
    }
}
