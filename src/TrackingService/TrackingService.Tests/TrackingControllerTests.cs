using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using TrackingService.API.Controllers;
using TrackingService.API.Hubs;
using TrackingService.API.Models;
using TrackingService.API.Repositories;
using Xunit;

namespace TrackingService.Tests;

public class TrackingControllerTests
{
    private readonly Mock<IHubContext<TrackingHub>> _hubContextMock;
    private readonly Mock<IHubClients> _clientsMock;
    private readonly Mock<IClientProxy> _clientProxyMock;
    private readonly Mock<ITrackingRepository> _repositoryMock;
    private readonly Mock<ILogger<TrackingController>> _loggerMock;
    private readonly TrackingController _controller;

    public TrackingControllerTests()
    {
        _hubContextMock = new Mock<IHubContext<TrackingHub>>();
        _clientsMock = new Mock<IHubClients>();
        _clientProxyMock = new Mock<IClientProxy>();
        _repositoryMock = new Mock<ITrackingRepository>();
        _loggerMock = new Mock<ILogger<TrackingController>>();

        _hubContextMock.Setup(h => h.Clients).Returns(_clientsMock.Object);
        _clientsMock
            .Setup(c => c.Group(It.IsAny<string>()))
            .Returns(_clientProxyMock.Object);

        _repositoryMock
            .Setup(r => r.LogLocationAsync(It.IsAny<LocationUpdate>()))
            .ReturnsAsync((LocationUpdate update) =>
            {
                update.LocationUpdateId = 1;
                return update;
            });

        _repositoryMock
            .Setup(r => r.GetHistoryAsync(It.IsAny<int>()))
            .ReturnsAsync(Array.Empty<LocationUpdate>());

        _controller = new TrackingController(
            _hubContextMock.Object,
            _repositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task PostLocation_ValidUpdate_Returns200()
    {
        var update = CreateValidLocationUpdate();

        var result = await _controller.PostLocation(update);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        Assert.True(GetPropertyValue<bool>(ok.Value, "success"));
    }

    [Fact]
    public async Task PostLocation_ValidUpdate_BroadcastsToCorrectSignalRGroup()
    {
        var update = CreateValidLocationUpdate();
        update.AssignmentId = 7;

        await _controller.PostLocation(update);

        _clientsMock.Verify(c => c.Group("tracking-7"), Times.Once);
        _clientProxyMock.Verify(
            cp => cp.SendCoreAsync(
                "ReceiveLocationUpdate",
                It.Is<object[]>(args => MatchesLocationUpdate(args, update)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PostLocation_MissingTimestamp_SetsUtcTimestampBeforeSave()
    {
        var update = CreateValidLocationUpdate();
        update.Timestamp = default;

        await _controller.PostLocation(update);

        _repositoryMock.Verify(
            r => r.LogLocationAsync(It.Is<LocationUpdate>(x => x.Timestamp != default)),
            Times.Once);
    }

    [Fact]
    public async Task PostLocation_LogsBroadcastCoordinates()
    {
        var update = CreateValidLocationUpdate();

        await _controller.PostLocation(update);

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state.ToString()!.Contains("Broadcasted driver coordinates via SignalR.") &&
                    state.ToString()!.Contains("AssignmentId: 1") &&
                    state.ToString()!.Contains("DriverId: 3") &&
                    state.ToString()!.Contains("Latitude: 6.9271") &&
                    state.ToString()!.Contains("Longitude: 79.8612")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAssignmentHistory_ReturnsRepositoryData()
    {
        var history = new[]
        {
            new LocationUpdate
            {
                LocationUpdateId = 4,
                AssignmentId = 10,
                DriverId = 99,
                Latitude = 6.9m,
                Longitude = 79.8m,
                Timestamp = DateTime.UtcNow
            }
        };

        _repositoryMock
            .Setup(r => r.GetHistoryAsync(10))
            .ReturnsAsync(history);

        var result = await _controller.GetAssignmentHistory(10);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.True(GetPropertyValue<bool>(ok.Value, "success"));

        var data = JsonSerializer.Deserialize<List<LocationUpdate>>(GetRawProperty(ok.Value, "data")!);
        Assert.NotNull(data);
        Assert.Single(data);
        Assert.Equal(99, data[0].DriverId);
    }

    [Fact]
    public async Task GetDriverLastKnown_WhenFound_ReturnsLocationUpdate()
    {
        _repositoryMock
            .Setup(r => r.GetLastKnownLocationAsync(15))
            .ReturnsAsync(new LocationUpdate
            {
                LocationUpdateId = 2,
                AssignmentId = 5,
                DriverId = 15,
                Latitude = 6.9271m,
                Longitude = 79.8612m,
                Timestamp = DateTime.UtcNow
            });

        var result = await _controller.GetDriverLastKnown(15);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.True(GetPropertyValue<bool>(ok.Value, "success"));

        var data = JsonSerializer.Deserialize<LocationUpdate>(GetRawProperty(ok.Value, "data")!);
        Assert.NotNull(data);
        Assert.Equal(15, data.DriverId);
    }

    [Fact]
    public async Task GetDriverLastKnown_WhenMissing_ReturnsNotFound()
    {
        _repositoryMock
            .Setup(r => r.GetLastKnownLocationAsync(15))
            .ReturnsAsync((LocationUpdate?)null);

        var result = await _controller.GetDriverLastKnown(15);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.False(GetPropertyValue<bool>(notFound.Value, "success"));
    }

    private static LocationUpdate CreateValidLocationUpdate() => new()
    {
        AssignmentId = 1,
        DriverId = 3,
        Latitude = 6.9271m,
        Longitude = 79.8612m,
        SpeedKmh = 60m,
        Timestamp = DateTime.UtcNow
    };

    private static bool MatchesLocationUpdate(object[] args, LocationUpdate expected)
    {
        if (args.Length != 1 || args[0] is not LocationUpdate actual)
            return false;

        return actual.AssignmentId == expected.AssignmentId &&
               actual.DriverId == expected.DriverId &&
               actual.Latitude == expected.Latitude &&
               actual.Longitude == expected.Longitude;
    }

    private static T? GetPropertyValue<T>(object? obj, string propertyName)
    {
        if (obj == null) return default;

        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(obj));
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            if (prop.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                return JsonSerializer.Deserialize<T>(prop.Value.GetRawText());
        }

        return default;
    }

    private static string? GetRawProperty(object? obj, string propertyName)
    {
        if (obj == null) return null;

        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(obj));
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            if (prop.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                return prop.Value.GetRawText();
        }

        return null;
    }
}
