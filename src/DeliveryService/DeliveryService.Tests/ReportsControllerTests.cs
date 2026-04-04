using DeliveryService.API.Controllers;
using DeliveryService.API.Models;
using DeliveryService.API.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Text;
using Xunit;

namespace DeliveryService.Tests;

public class ReportsControllerTests
{
    private readonly Mock<IDeliveryReportService> _reportServiceMock;
    private readonly ReportsController _controller;

    public ReportsControllerTests()
    {
        _reportServiceMock = new Mock<IDeliveryReportService>();
        _controller = new ReportsController(_reportServiceMock.Object);
    }

    [Fact]
    public async Task GetDeliverySuccessReport_Returns200_WhenSuccessful()
    {
        var expected = new DeliverySuccessRateSummary
        {
            DeliveredCount = 8,
            FailedCount = 1,
            CancelledCount = 1,
            TerminalCount = 10,
            SuccessRatePercentage = 80m
        };

        _reportServiceMock
            .Setup(s => s.GetDeliverySuccessRateAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.GetDeliverySuccessReport(null, null, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetDeliverySuccessReport_Returns400_WhenDateRangeInvalid()
    {
        _reportServiceMock
            .Setup(s => s.GetDeliverySuccessRateAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("startDateUtc must be earlier than endDateUtc."));

        var start = new DateTime(2026, 3, 30, 10, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 3, 30, 9, 0, 0, DateTimeKind.Utc);

        var result = await _controller.GetDeliverySuccessReport(start, end, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task GetDeliverySuccessReport_Returns400_WhenUnhandledErrorOccurs()
    {
        _reportServiceMock
            .Setup(s => s.GetDeliverySuccessRateAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("db failure"));

        var result = await _controller.GetDeliverySuccessReport(null, null, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task GetDeliverySuccessReportCsv_ReturnsFile_WhenSuccessful()
    {
        var expected = new DeliverySuccessRateSummary
        {
            DeliveredCount = 8,
            FailedCount = 1,
            CancelledCount = 1,
            TerminalCount = 10,
            SuccessRatePercentage = 80m
        };

        _reportServiceMock
            .Setup(s => s.GetDeliverySuccessRateAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.GetDeliverySuccessReportCsv(null, null, CancellationToken.None);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv", file.ContentType);
        var csv = Encoding.UTF8.GetString(file.FileContents);
        Assert.Contains("DeliveredCount,FailedCount,CancelledCount,TerminalCount,SuccessRatePercentage", csv);
    }

    [Fact]
    public async Task GetDeliveryTrendsCsv_ReturnsFile_WhenSuccessful()
    {
        var trends = new List<DeliveryTrendPoint>
        {
            new()
            {
                Period = "2026-04-01",
                Total = 10,
                Pending = 1,
                Assigned = 1,
                InTransit = 2,
                Delivered = 5,
                Failed = 1,
                Canceled = 0
            }
        };

        _reportServiceMock
            .Setup(s => s.GetDeliveryTrendsAsync("daily", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trends);

        var result = await _controller.GetDeliveryTrendsCsv("daily", null, null, CancellationToken.None);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv", file.ContentType);
        var csv = Encoding.UTF8.GetString(file.FileContents);
        Assert.Contains("Period,Total,Pending,Assigned,InTransit,Delivered,Failed,Canceled", csv);
        Assert.Contains("2026-04-01,10,1,1,2,5,1,0", csv);
    }
}