using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Moq;
using System.Text.Json;
using TrackingService.API.Controllers;
using TrackingService.API.Hubs;
using TrackingService.API.Models;
using Xunit;

namespace TrackingService.Tests;

/// <summary>
/// Tests for TrackingController.
/// PostLocation mocks IHubContext&lt;TrackingHub&gt; to verify SignalR SendAsync is called.
/// GetAssignmentHistory and GetDriverLastKnown are pure placeholder implementations.
/// </summary>
public class TrackingControllerTests
{
    private readonly Mock<IHubContext<TrackingHub>> _hubContextMock;
    private readonly Mock<IHubClients> _clientsMock;
    private readonly Mock<IClientProxy> _clientProxyMock;
    private readonly TrackingController _controller;

    public TrackingControllerTests()
    {
        _hubContextMock = new Mock<IHubContext<TrackingHub>>();
        _clientsMock = new Mock<IHubClients>();
        _clientProxyMock = new Mock<IClientProxy>();

        // Wire up: HubContext.Clients.Group(...) => clientProxy
        _hubContextMock.Setup(h => h.Clients).Returns(_clientsMock.Object);
        _clientsMock
            .Setup(c => c.Group(It.IsAny<string>()))
            .Returns(_clientProxyMock.Object);

        _controller = new TrackingController(_hubContextMock.Object);
    }

    // ---------- POST /api/tracking/location ----------

    [Fact]
    public async Task PostLocation_ValidUpdate_Returns200()
    {
        // Arrange
        var update = CreateValidLocationUpdate();

        // Act
        var result = await _controller.PostLocation(update);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        Assert.NotNull(ok.Value);

        var success = GetPropertyValue<bool>(ok.Value, "success");
        Assert.True(success);
    }

    [Fact]
    public async Task PostLocation_ValidUpdate_BroadcastsToCorrectSignalRGroup()
    {
        // Arrange
        var update = CreateValidLocationUpdate();
        update.AssignmentId = 7;

        // Act
        await _controller.PostLocation(update);

        // Assert — SignalR Group called with correct group name
        _clientsMock.Verify(
            c => c.Group("tracking-7"),
            Times.Once);
    }

    [Fact]
    public async Task PostLocation_ValidUpdate_CallsSendAsync()
    {
        // Arrange
        var update = CreateValidLocationUpdate();

        // Act
        await _controller.PostLocation(update);

        // Assert — SendAsync("ReceiveLocationUpdate", ...) was called once
        _clientProxyMock.Verify(
            cp => cp.SendCoreAsync(
                "ReceiveLocationUpdate",
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PostLocation_SetsLocationUpdateIdAndTimestamp()
    {
        // Arrange
        var update = CreateValidLocationUpdate();
        update.LocationUpdateId = 0; // starts at 0

        // Act
        var result = await _controller.PostLocation(update);

        // Assert — Placeholder sets LocationUpdateId = 1 and Timestamp to UtcNow
        var ok = Assert.IsType<OkObjectResult>(result);
        var dataJson = GetRawProperty(ok.Value, "data");
        Assert.NotNull(dataJson);
        var data = JsonSerializer.Deserialize<JsonElement>(dataJson!);
        Assert.Equal(1, GetInt32Property(data, "locationUpdateId"));
    }

    [Fact]
    public async Task PostLocation_ResponseContainsLocationData()
    {
        // Arrange
        var update = CreateValidLocationUpdate();
        update.DriverId = 42;
        update.AssignmentId = 5;
        update.Latitude = 6.9271m;
        update.Longitude = 79.8612m;

        // Act
        var result = await _controller.PostLocation(update);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var dataJson = GetRawProperty(ok.Value, "data");
        Assert.NotNull(dataJson);
        var data = JsonSerializer.Deserialize<JsonElement>(dataJson!);
        Assert.Equal(42, GetInt32Property(data, "driverId"));
        Assert.Equal(5, GetInt32Property(data, "assignmentId"));
    }

    // ---------- GET /api/tracking/assignment/{assignmentId}/history ----------

    [Fact]
    public void GetAssignmentHistory_ReturnsEmptyList()
    {
        // Act
        var result = _controller.GetAssignmentHistory(10);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        Assert.NotNull(ok.Value);

        var success = GetPropertyValue<bool>(ok.Value, "success");
        Assert.True(success);

        var dataJson = GetRawProperty(ok.Value, "data");
        Assert.NotNull(dataJson);
        var dataArray = JsonSerializer.Deserialize<List<JsonElement>>(dataJson!);
        Assert.NotNull(dataArray);
        Assert.Empty(dataArray);
    }

    [Fact]
    public void GetAssignmentHistory_Returns200ForAnyAssignmentId()
    {
        // Act
        var result = _controller.GetAssignmentHistory(999);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        var success = GetPropertyValue<bool>(ok.Value, "success");
        Assert.True(success);
    }

    // ---------- GET /api/tracking/driver/{driverId}/last-known ----------

    [Fact]
    public void GetDriverLastKnown_ReturnsLocationUpdate()
    {
        // Act
        var result = _controller.GetDriverLastKnown(15);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        Assert.NotNull(ok.Value);

        var success = GetPropertyValue<bool>(ok.Value, "success");
        Assert.True(success);

        var dataJson = GetRawProperty(ok.Value, "data");
        Assert.NotNull(dataJson);
        Assert.NotEmpty(dataJson!);
    }

    [Fact]
    public void GetDriverLastKnown_ResponseContainsMatchingDriverId()
    {
        // Act
        var result = _controller.GetDriverLastKnown(15);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var dataJson = GetRawProperty(ok.Value, "data");
        Assert.NotNull(dataJson);
        var data = JsonSerializer.Deserialize<JsonElement>(dataJson!);

        // Placeholder echoes back the driverId
        Assert.Equal(15, GetInt32Property(data, "driverId"));
    }

    // ---------- Helpers ----------

    private static LocationUpdate CreateValidLocationUpdate() => new()
    {
        AssignmentId = 1,
        DriverId = 3,
        Latitude = 6.9271m,
        Longitude = 79.8612m,
        SpeedKmh = 60m,
        Timestamp = DateTime.UtcNow
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
