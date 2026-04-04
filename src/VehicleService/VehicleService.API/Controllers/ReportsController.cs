using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text;
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

    [HttpGet("vehicle-utilization/csv")]
    [Authorize(Roles = "Admin,Dispatcher")]
    public async Task<IActionResult> GetVehicleUtilizationReportCsv([FromQuery] DateTime? startDateUtc = null, [FromQuery] DateTime? endDateUtc = null)
    {
        try
        {
            var report = await _vehicleService.GetVehicleUtilizationReportAsync(startDateUtc, endDateUtc);
            var csv = new StringBuilder();
            csv.AppendLine("VehicleId,TripsCount,KilometersDriven,IdleTimeMinutes");

            foreach (var item in report)
            {
                csv.AppendLine(string.Join(",",
                    item.VehicleId,
                    item.TripsCount,
                    item.KilometersDriven.ToString(CultureInfo.InvariantCulture),
                    item.IdleTimeMinutes.ToString(CultureInfo.InvariantCulture)));
            }

            return BuildCsvFile(csv.ToString(), "vehicle-utilization-report");
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

    private FileContentResult BuildCsvFile(string csvContent, string fileNamePrefix)
    {
        var fileName = $"{fileNamePrefix}-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
        return File(Encoding.UTF8.GetBytes(csvContent), "text/csv", fileName);
    }
}