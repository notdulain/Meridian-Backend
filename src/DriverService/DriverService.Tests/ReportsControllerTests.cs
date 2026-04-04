using DriverService.API.Controllers;
using DriverService.API.Models;
using DriverService.API.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Text;
using Xunit;

namespace DriverService.Tests;

public class ReportsControllerTests
{
    private readonly Mock<IDriverService> _serviceMock;
    private readonly ReportsController _controller;

    public ReportsControllerTests()
    {
        _serviceMock = new Mock<IDriverService>();
        _controller = new ReportsController(_serviceMock.Object);
    }

    [Fact]
    public async Task GetDriverPerformanceReportCsv_ReturnsFile_WhenServiceSucceeds()
    {
        var report = new List<DriverPerformanceMetrics>
        {
            new()
            {
                DriverId = 7,
                DeliveriesCompleted = 14,
                AverageDeliveryTimeMinutes = 22.5,
                OnTimeRatePercent = 92.3
            }
        };

        _serviceMock
            .Setup(s => s.GetDriverPerformanceReportAsync(null, null))
            .ReturnsAsync(report);

        var result = await _controller.GetDriverPerformanceReportCsv();

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv", file.ContentType);
        var csv = Encoding.UTF8.GetString(file.FileContents);
        Assert.Contains("DriverId,DeliveriesCompleted,AverageDeliveryTimeMinutes,OnTimeRatePercent", csv);
        Assert.Contains("7,14,22.5,92.3", csv);
    }
}
