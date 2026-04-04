using DriverService.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text;

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

    [HttpGet("driver-performance/csv")]
    [Authorize(Roles = "Admin,Dispatcher,Manager")]
    public async Task<IActionResult> GetDriverPerformanceReportCsv([FromQuery] DateTime? startDateUtc = null, [FromQuery] DateTime? endDateUtc = null)
    {
        try
        {
            var report = await _driverService.GetDriverPerformanceReportAsync(startDateUtc, endDateUtc);
            var csv = new StringBuilder();
            csv.AppendLine("DriverId,DeliveriesCompleted,AverageDeliveryTimeMinutes,OnTimeRatePercent");

            foreach (var item in report)
            {
                csv.AppendLine(string.Join(",",
                    item.DriverId,
                    item.DeliveriesCompleted,
                    item.AverageDeliveryTimeMinutes.ToString(CultureInfo.InvariantCulture),
                    item.OnTimeRatePercent.ToString(CultureInfo.InvariantCulture)));
            }

            return BuildCsvFile(csv.ToString(), "driver-performance-report");
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

    private FileContentResult BuildCsvFile(string csvContent, string fileNamePrefix)
    {
        var fileName = $"{fileNamePrefix}-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
        return File(Encoding.UTF8.GetBytes(csvContent), "text/csv", fileName);
    }
}
