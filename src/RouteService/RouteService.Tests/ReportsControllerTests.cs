using Microsoft.AspNetCore.Mvc;
using Moq;
using RouteService.API.Controllers;
using RouteService.API.Models;
using RouteService.API.Services;
using System.Text;
using Xunit;

namespace RouteService.Tests;

public class ReportsControllerTests
{
    private readonly Mock<IFuelCostReportService> _reportServiceMock = new();

    private ReportsController CreateController() => new(_reportServiceMock.Object);

    [Fact]
    public async Task GetFuelCostReport_Returns200_WhenServiceSucceeds()
    {
        var report = new List<FuelCostAggregate>
        {
            new()
            {
                VehicleId = 11,
                DriverId = 21,
                PeriodStartUtc = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
                TripCount = 2,
                TotalDistanceKm = 220,
                TotalFuelConsumptionLitres = 22m,
                TotalFuelCostLkr = 6600m
            }
        };

        _reportServiceMock
            .Setup(x => x.GetFuelCostReportAsync(null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await CreateController().GetFuelCostReport(null, null, null, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetFuelCostReport_Returns400_WhenServiceThrows()
    {
        _reportServiceMock
            .Setup(x => x.GetFuelCostReportAsync(null, null, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("db failure"));

        var result = await CreateController().GetFuelCostReport(null, null, null, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task GetFuelCostReport_Returns400_WhenDateRangeIsInvalid()
    {
        var startDateUtc = new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc);
        var endDateUtc = new DateTime(2026, 4, 9, 0, 0, 0, DateTimeKind.Utc);

        var result = await CreateController().GetFuelCostReport(11, startDateUtc, endDateUtc, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task GetFuelCostReportCsv_ReturnsFile_WhenServiceSucceeds()
    {
        var report = new List<FuelCostAggregate>
        {
            new()
            {
                VehicleId = 11,
                DriverId = 21,
                PeriodStartUtc = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
                TripCount = 2,
                TotalDistanceKm = 220,
                TotalFuelConsumptionLitres = 22m,
                TotalFuelCostLkr = 6600m
            }
        };

        _reportServiceMock
            .Setup(x => x.GetFuelCostReportAsync(null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await CreateController().GetFuelCostReportCsv(null, null, null, CancellationToken.None);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv", file.ContentType);
        var csv = Encoding.UTF8.GetString(file.FileContents);
        Assert.Contains("VehicleId,DriverId,PeriodStartUtc,TripCount,TotalDistanceKm,TotalFuelConsumptionLitres,TotalFuelCostLkr", csv);
        Assert.Contains("11,21,2026-04-01T00:00:00.0000000Z,2,220,22,6600", csv);
    }
}
