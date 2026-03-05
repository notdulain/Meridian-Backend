using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Text.Json;
using RouteService.API.Controllers;
using RouteService.API.Models;
using RouteService.API.Services;
using Xunit;

namespace RouteService.Tests;

/// <summary>
/// Tests for RoutesController.
/// Note: The controller is a placeholder with no dependencies — straightforward to test directly.
/// </summary>
public class RoutesControllerTests
{
    private readonly RoutesController _controller;
    private readonly Mock<IGoogleMapsService> _googleMapsServiceMock;

    public RoutesControllerTests()
    {
        _googleMapsServiceMock = new Mock<IGoogleMapsService>(MockBehavior.Strict);
        _controller = new RoutesController(_googleMapsServiceMock.Object);
    }

    // ---------- POST /api/routes/optimize ----------

    [Fact]
    public void OptimizeRoute_ReturnsSuccessWithAtLeastOneOption()
    {
        // Arrange
        var request = new OptimizeRouteRequest
        {
            Origin = "Colombo",
            Destination = "Kandy",
            VehicleId = 1,
            DeliveryId = 10
        };

        // Act
        var result = _controller.OptimizeRoute(request);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        Assert.NotNull(ok.Value);

        var success = GetPropertyValue<bool>(ok.Value, "success");
        Assert.True(success);

        // Placeholder always returns at least one route option
        var dataJson = GetRawProperty(ok.Value, "data");
        Assert.NotNull(dataJson);
        var options = JsonSerializer.Deserialize<List<JsonElement>>(dataJson!);
        Assert.NotNull(options);
        Assert.NotEmpty(options);
    }

    [Fact]
    public void OptimizeRoute_ReturnedOptionHasExpectedFields()
    {
        // Arrange
        var request = new OptimizeRouteRequest
        {
            Origin = "Colombo",
            Destination = "Kandy",
            VehicleId = 1,
            DeliveryId = 10
        };

        // Act
        var result = _controller.OptimizeRoute(request);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var dataJson = GetRawProperty(ok.Value, "data");
        Assert.NotNull(dataJson);

        var options = JsonSerializer.Deserialize<List<JsonElement>>(dataJson!);
        Assert.NotNull(options);
        var first = options![0];

        // Verify all required RouteOption fields are present
        Assert.True(TryGetCaseInsensitive(first, "routeId", out var routeId));
        Assert.NotEmpty(routeId.GetString()!);
        Assert.True(TryGetCaseInsensitive(first, "summary", out _));
        Assert.True(TryGetCaseInsensitive(first, "distance", out _));
        Assert.True(TryGetCaseInsensitive(first, "duration", out _));
        Assert.True(TryGetCaseInsensitive(first, "fuelCost", out _));
    }

    [Fact]
    public void OptimizeRoute_ReturnedOptionHasPositiveFuelCost()
    {
        // Arrange
        var request = new OptimizeRouteRequest
        {
            Origin = "Colombo",
            Destination = "Galle",
            VehicleId = 2,
            DeliveryId = 5
        };

        // Act
        var result = _controller.OptimizeRoute(request);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var dataJson = GetRawProperty(ok.Value, "data");
        var options = JsonSerializer.Deserialize<List<JsonElement>>(dataJson!);
        Assert.NotNull(options);

        foreach (var option in options!)
        {
            var fuelCost = GetDoubleProperty(option, "fuelCost");
            Assert.True(fuelCost > 0, $"Expected fuelCost > 0 but got {fuelCost}");
        }
    }

    // ---------- GET /api/routes/{routeId} ----------

    [Fact]
    public void GetRoute_ReturnsRouteOptionWithMatchingId()
    {
        // Act
        var result = _controller.GetRoute("R-42");

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        Assert.NotNull(ok.Value);

        var success = GetPropertyValue<bool>(ok.Value, "success");
        Assert.True(success);

        // Placeholder echoes back the routeId
        var dataJson = GetRawProperty(ok.Value, "data");
        Assert.NotNull(dataJson);
        var route = JsonSerializer.Deserialize<JsonElement>(dataJson!);
        Assert.Equal("R-42", GetStringProperty(route, "routeId"));
    }

    [Fact]
    public void GetRoute_ResponseContainsAllRequiredFields()
    {
        // Act
        var result = _controller.GetRoute("R-1");

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var dataJson = GetRawProperty(ok.Value, "data");
        Assert.NotNull(dataJson);

        var route = JsonSerializer.Deserialize<JsonElement>(dataJson!);
        Assert.True(TryGetCaseInsensitive(route, "routeId", out _));
        Assert.True(TryGetCaseInsensitive(route, "summary", out var summary));
        Assert.NotEmpty(summary.GetString()!);
        Assert.True(TryGetCaseInsensitive(route, "distance", out _));
        Assert.True(TryGetCaseInsensitive(route, "duration", out _));
        Assert.True(TryGetCaseInsensitive(route, "fuelCost", out _));
    }

    // ---------- POST /api/routes/fuel-cost ----------

    [Fact]
    public void CalculateFuelCost_ReturnsSuccessWithFuelCost()
    {
        // Act
        var result = _controller.CalculateFuelCost();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        Assert.NotNull(ok.Value);

        var success = GetPropertyValue<bool>(ok.Value, "success");
        Assert.True(success);

        // Placeholder returns a fuelCost value
        var fuelCost = GetPropertyValue<double>(ok.Value, "fuelCost");
        Assert.True(fuelCost > 0, $"Expected fuelCost > 0 but got {fuelCost}");
    }

    [Fact]
    public void CalculateFuelCost_ReturnsExpectedPlaceholderValue()
    {
        // Act
        var result = _controller.CalculateFuelCost();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var fuelCost = GetPropertyValue<double>(ok.Value, "fuelCost");
        // Placeholder always returns 10.5
        Assert.Equal(10.5, fuelCost);
    }

    // ---------- Helpers ----------

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

    private static bool TryGetCaseInsensitive(JsonElement element, string propertyName, out JsonElement value)
    {
        foreach (var prop in element.EnumerateObject())
        {
            if (prop.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value;
                return true;
            }
        }
        value = default;
        return false;
    }

    private static string? GetStringProperty(JsonElement element, string propertyName)
    {
        foreach (var prop in element.EnumerateObject())
        {
            if (prop.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                return prop.Value.GetString();
        }
        return null;
    }

    private static double GetDoubleProperty(JsonElement element, string propertyName)
    {
        foreach (var prop in element.EnumerateObject())
        {
            if (prop.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                return prop.Value.GetDouble();
        }
        throw new KeyNotFoundException($"Property '{propertyName}' not found.");
    }
}
