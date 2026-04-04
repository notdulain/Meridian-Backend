using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Text.Json;
using System.Text;
using VehicleService.API.Controllers;
using VehicleService.API.Models;
using VehicleService.API.Services;
using Xunit;

namespace VehicleService.Tests;

public class ReportsControllerTests
{
    private readonly Mock<IVehicleService> _serviceMock;
    private readonly ReportsController _controller;

    public ReportsControllerTests()
    {
        _serviceMock = new Mock<IVehicleService>();
        _controller = new ReportsController(_serviceMock.Object);
    }

    [Fact]
    public async Task GetVehicleUtilizationReport_Returns200_WhenServiceSucceeds()
    {
        var report = new List<VehicleUtilizationMetrics>
        {
            new() { VehicleId = 1, TripsCount = 4, KilometersDriven = 180, IdleTimeMinutes = 240 }
        };

        _serviceMock
            .Setup(s => s.GetVehicleUtilizationReportAsync(null, null))
            .ReturnsAsync(report);

        var result = await _controller.GetVehicleUtilizationReport();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        var success = GetPropertyValue<bool>(ok.Value, "success");
        Assert.True(success);
    }

    [Fact]
    public async Task GetVehicleUtilizationReport_Returns400_WhenServiceThrowsArgumentException()
    {
        _serviceMock
            .Setup(s => s.GetVehicleUtilizationReportAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ThrowsAsync(new ArgumentException("End date must be greater than start date."));

        var result = await _controller.GetVehicleUtilizationReport(DateTime.UtcNow, DateTime.UtcNow.AddDays(-1));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
        var success = GetPropertyValue<bool>(badRequest.Value, "success");
        Assert.False(success);
    }

    [Fact]
    public async Task GetVehicleUtilizationReport_Returns400_WhenServiceThrowsUnexpectedException()
    {
        _serviceMock
            .Setup(s => s.GetVehicleUtilizationReportAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ThrowsAsync(new Exception("Unexpected failure"));

        var result = await _controller.GetVehicleUtilizationReport();

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
        var success = GetPropertyValue<bool>(badRequest.Value, "success");
        Assert.False(success);
    }

    [Fact]
    public async Task GetVehicleUtilizationReportCsv_ReturnsFile_WhenServiceSucceeds()
    {
        var report = new List<VehicleUtilizationMetrics>
        {
            new() { VehicleId = 1, TripsCount = 4, KilometersDriven = 180, IdleTimeMinutes = 240 }
        };

        _serviceMock
            .Setup(s => s.GetVehicleUtilizationReportAsync(null, null))
            .ReturnsAsync(report);

        var result = await _controller.GetVehicleUtilizationReportCsv();

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv", file.ContentType);
        var csv = Encoding.UTF8.GetString(file.FileContents);
        Assert.Contains("VehicleId,TripsCount,KilometersDriven,IdleTimeMinutes", csv);
        Assert.Contains("1,4,180,240", csv);
    }

    private static T? GetPropertyValue<T>(object? obj, string propertyName)
    {
        if (obj == null) return default;

        var json = JsonSerializer.Serialize(obj);
        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty(propertyName, out var property))
        {
            return JsonSerializer.Deserialize<T>(property.GetRawText());
        }

        return default;
    }
}