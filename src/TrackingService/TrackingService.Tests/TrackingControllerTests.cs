using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Moq;
using System.Text.Json;
using TrackingService.API.Controllers;
using TrackingService.API.Hubs;
using TrackingService.API.Models;
using TrackingService.API.Repositories;
using Xunit;

namespace TrackingService.Tests;

/// <summary>
/// Unit tests for TrackingController.
/// All external dependencies (IHubContext, ITrackingRepository) are mocked with Moq.
/// </summary>
public class TrackingControllerTests
{
    private readonly Mock<IHubContext<TrackingHub>> _hubContextMock;
    private readonly Mock<IHubClients> _clientsMock;
    private readonly Mock<IClientProxy> _clientProxyMock;
    private readonly Mock<ITrackingRepository> _repositoryMock;
    private readonly TrackingController _controller;

    public TrackingControllerTests()
    {
        _hubContextMock    = new Mock<IHubContext<TrackingHub>>();
        _clientsMock       = new Mock<IHubClients>();
        _clientProxyMock   = new Mock<IClientProxy>();
        _repositoryMock    = new Mock<ITrackingRepository>();

        // Wire up: HubContext.Clients.Group(...) → clientProxy
        _hubContextMock.Setup(h => h.Clients).Returns(_clientsMock.Object);
        _clientsMock
            .Setup(c => c.Group(It.IsAny<string>()))
            .Returns(_clientProxyMock.Object);

        // Default stub: LogLocationAsync echoes the update back with LocationUpdateId = 1
        _repositoryMock
            .Setup(r => r.LogLocationAsync(It.IsAny<LocationUpdate>()))
            .ReturnsAsync((LocationUpdate u) =>
            {
                u.LocationUpdateId = 1;
                return u;
            });

        // Default stub: GetHistoryAsync returns an empty list
        _repositoryMock
            .Setup(r => r.GetHistoryAsync(It.IsAny<int>()))
            .ReturnsAsync(Enumerable.Empty<LocationUpdate>());

        // Default stub: GetLastKnownLocationAsync returns a seeded location
        _repositoryMock
            .Setup(r => r.GetLastKnownLocationAsync(It.IsAny<int>()))
            .ReturnsAsync((int driverId) => new LocationUpdate
            {
                LocationUpdateId = 99,
                AssignmentId     = 1,
                DriverId         = driverId,
                Latitude         = 6.9271m,
                Longitude        = 79.8612m,
                Timestamp        = DateTime.UtcNow,
                SpeedKmh         = 0m,
            });

        _controller = new TrackingController(_hubContextMock.Object, _repositoryMock.Object);
    }

    // ── POST /api/tracking/location ────────────────────────────────────────────

    [Fact]
    public async Task PostLocation_ValidUpdate_Returns200()
    {
        var update = CreateValidLocationUpdate();

        var result = await _controller.PostLocation(update);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        Assert.NotNull(ok.Value);

        var success = GetPropertyValue<bool>(ok.Value, "success");
        Assert.True(success);
    }

    [Fact]
    public async Task PostLocation_ValidUpdate_BroadcastsToCorrectSignalRGroup()
    {
        var update = CreateValidLocationUpdate();
        update.AssignmentId = 7;

        await _controller.PostLocation(update);

        _clientsMock.Verify(c => c.Group("tracking-7"), Times.Once);
    }

    [Fact]
    public async Task PostLocation_ValidUpdate_CallsSendAsync()
    {
        var update = CreateValidLocationUpdate();

        await _controller.PostLocation(update);

        _clientProxyMock.Verify(
            cp => cp.SendCoreAsync(
                "ReceiveLocationUpdate",
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PostLocation_CallsRepository_LogLocationAsync()
    {
        var update = CreateValidLocationUpdate();

        await _controller.PostLocation(update);

        _repositoryMock.Verify(r => r.LogLocationAsync(update), Times.Once);
    }

    [Fact]
    public async Task PostLocation_ResponseContainsPersistedLocationUpdateId()
    {
        var update = CreateValidLocationUpdate();
        update.LocationUpdateId = 0;

        var result = await _controller.PostLocation(update);

        var ok      = Assert.IsType<OkObjectResult>(result);
        var dataJson = GetRawProperty(ok.Value, "data");
        Assert.NotNull(dataJson);
        var data = JsonSerializer.Deserialize<JsonElement>(dataJson!);
        Assert.Equal(1, GetInt32Property(data, "locationUpdateId"));
    }

    [Fact]
    public async Task PostLocation_ResponseContainsLocationData()
    {
        var update = CreateValidLocationUpdate();
        update.DriverId      = 42;
        update.AssignmentId  = 5;
        update.Latitude      = 6.9271m;
        update.Longitude     = 79.8612m;

        var result = await _controller.PostLocation(update);

        var ok      = Assert.IsType<OkObjectResult>(result);
        var dataJson = GetRawProperty(ok.Value, "data");
        Assert.NotNull(dataJson);
        var data = JsonSerializer.Deserialize<JsonElement>(dataJson!);
        Assert.Equal(42, GetInt32Property(data, "driverId"));
        Assert.Equal(5,  GetInt32Property(data, "assignmentId"));
    }

    [Fact]
    public async Task PostLocation_DefaultTimestamp_IsSetToUtcNow()
    {
        var update = CreateValidLocationUpdate();
        update.Timestamp = default; // force the controller to set it

        var before = DateTime.UtcNow;
        await _controller.PostLocation(update);
        var after = DateTime.UtcNow;

        // The controller should have overwritten the default timestamp
        Assert.InRange(update.Timestamp, before.AddSeconds(-1), after.AddSeconds(1));
    }

    // ── GET /api/tracking/assignment/{assignmentId}/history ────────────────────

    [Fact]
    public async Task GetAssignmentHistory_ReturnsEmptyList()
    {
        var result = await _controller.GetAssignmentHistory(10);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);

        var success  = GetPropertyValue<bool>(ok.Value, "success");
        Assert.True(success);

        var dataJson = GetRawProperty(ok.Value, "data");
        Assert.NotNull(dataJson);
        var dataArray = JsonSerializer.Deserialize<List<JsonElement>>(dataJson!);
        Assert.NotNull(dataArray);
        Assert.Empty(dataArray);
    }

    [Fact]
    public async Task GetAssignmentHistory_Returns200ForAnyAssignmentId()
    {
        var result = await _controller.GetAssignmentHistory(999);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);

        var success = GetPropertyValue<bool>(ok.Value, "success");
        Assert.True(success);
    }

    [Fact]
    public async Task GetAssignmentHistory_CallsRepository()
    {
        await _controller.GetAssignmentHistory(42);

        _repositoryMock.Verify(r => r.GetHistoryAsync(42), Times.Once);
    }

    // ── GET /api/tracking/driver/{driverId}/last-known ─────────────────────────

    [Fact]
    public async Task GetDriverLastKnown_Returns200WithLocationData()
    {
        var result = await _controller.GetDriverLastKnown(15);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        Assert.NotNull(ok.Value);

        var success = GetPropertyValue<bool>(ok.Value, "success");
        Assert.True(success);
    }

    [Fact]
    public async Task GetDriverLastKnown_ResponseContainsMatchingDriverId()
    {
        var result = await _controller.GetDriverLastKnown(15);

        var ok      = Assert.IsType<OkObjectResult>(result);
        var dataJson = GetRawProperty(ok.Value, "data");
        Assert.NotNull(dataJson);
        var data = JsonSerializer.Deserialize<JsonElement>(dataJson!);
        Assert.Equal(15, GetInt32Property(data, "driverId"));
    }

    [Fact]
    public async Task GetDriverLastKnown_ReturnsNotFound_WhenNoDataExists()
    {
        _repositoryMock
            .Setup(r => r.GetLastKnownLocationAsync(999))
            .ReturnsAsync((LocationUpdate?)null);

        var result = await _controller.GetDriverLastKnown(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetDriverLastKnown_CallsRepository()
    {
        await _controller.GetDriverLastKnown(7);

        _repositoryMock.Verify(r => r.GetLastKnownLocationAsync(7), Times.Once);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static LocationUpdate CreateValidLocationUpdate() => new()
    {
        AssignmentId = 1,
        DriverId     = 3,
        Latitude     = 6.9271m,
        Longitude    = 79.8612m,
        SpeedKmh     = 60m,
        Timestamp    = DateTime.UtcNow,
    };

    private static T? GetPropertyValue<T>(object? obj, string propertyName)
    {
        if (obj == null) return default;
        var json = JsonSerializer.Serialize(obj);
        using var doc = JsonDocument.Parse(json);
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
        var json = JsonSerializer.Serialize(obj);
        using var doc = JsonDocument.Parse(json);
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            if (prop.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                return prop.Value.GetRawText();
        }
        return null;
    }

    private static int GetInt32Property(JsonElement element, string propertyName)
    {
        foreach (var prop in element.EnumerateObject())
        {
            if (prop.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                return prop.Value.GetInt32();
        }
        throw new KeyNotFoundException($"Property '{propertyName}' not found.");
    }
}
