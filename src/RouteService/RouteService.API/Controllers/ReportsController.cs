using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteService.API.Services;
using System.Globalization;
using System.IO;
using System.Text;

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

    [HttpGet("fuel-cost/csv")]
    [Authorize(Roles = "Admin,Dispatcher,Manager")]
    public async Task<IActionResult> GetFuelCostReportCsv(
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
            using var csvWriter = new StringWriter(CultureInfo.InvariantCulture);
            csvWriter.WriteLine("Period,Vehicle ID,Driver ID,Trips,Distance (km),Fuel (L),Total Cost (LKR)");

            foreach (var item in report)
            {
                csvWriter.WriteLine(string.Join(",",
                    item.PeriodStartUtc.ToString("O", CultureInfo.InvariantCulture),
                    item.VehicleId,
                    item.DriverId,
                    item.TripCount,
                    item.TotalDistanceKm.ToString(CultureInfo.InvariantCulture),
                    item.TotalFuelConsumptionLitres.ToString(CultureInfo.InvariantCulture),
                    item.TotalFuelCostLkr.ToString(CultureInfo.InvariantCulture)));
            }

            return BuildCsvFile(csvWriter.ToString(), "fuel-cost-report");
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

    private FileContentResult BuildCsvFile(string csvContent, string fileNamePrefix)
    {
        var fileName = $"{fileNamePrefix}-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
        return File(Encoding.UTF8.GetBytes(csvContent), "application/octet-stream", fileName);
    }
}