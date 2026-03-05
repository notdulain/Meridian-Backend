using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RouteService.API.Controllers;
using RouteService.API.Models;
using RouteService.API.Services;
using Xunit;

namespace RouteService.Tests;

public class RoutesControllerTests
{
    private readonly Mock<IGoogleMapsService> _googleMapsServiceMock;
    private readonly RoutesController _controller;

    public RoutesControllerTests()
    {
        _googleMapsServiceMock = new Mock<IGoogleMapsService>(MockBehavior.Strict);
        _controller = new RoutesController(_googleMapsServiceMock.Object);
    }

    [Fact]
    public async Task OptimizeRoute_ReturnsBadRequest_WhenOriginOrDestinationMissing()
    {
        var request = new OptimizeRouteRequest
        {
            Origin = string.Empty,
            Destination = "Kandy",
            VehicleId = 1,
            DeliveryId = 1
        };

        var result = await _controller.OptimizeRoute(request, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task OptimizeRoute_ReturnsOptimizedRouteAndAlternatives()
    {
        var request = new OptimizeRouteRequest
        {
            Origin = "Colombo",
            Destination = "Kandy",
            VehicleId = 2,
            DeliveryId = 10
        };

        var routes = new List<RouteOption>
        {
            new()
            {
                RouteId = "r-1",
                Summary = "Fastest route",
                Distance = "115 km",
                DistanceValue = 115000,
                Duration = "2 hours",
                DurationValue = 7200,
                FuelCost = 3864,
                PolylinePoints = "abc"
            },
            new()
            {
                RouteId = "r-2",
                Summary = "Alternative route",
                Distance = "121 km",
                DistanceValue = 121000,
                Duration = "2 hours 15 mins",
                DurationValue = 8100,
                FuelCost = 4065.6,
                PolylinePoints = "def"
            }
        };

        _googleMapsServiceMock
            .Setup(x => x.GetAlternativeRoutesAsync("Colombo", "Kandy", It.IsAny<CancellationToken>()))
            .ReturnsAsync(routes);

        var result = await _controller.OptimizeRoute(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);

        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal("r-1", root.GetProperty("optimizedRoute").GetProperty("RouteId").GetString());
        Assert.Equal(1, root.GetProperty("alternatives").GetArrayLength());
    }

    [Fact]
    public async Task OptimizeRoute_ReturnsNotFound_WhenNoRoutesReturned()
    {
        var request = new OptimizeRouteRequest
        {
            Origin = "Colombo",
            Destination = "Kandy",
            VehicleId = 2,
            DeliveryId = 10
        };

        _googleMapsServiceMock
            .Setup(x => x.GetAlternativeRoutesAsync("Colombo", "Kandy", It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _controller.OptimizeRoute(request, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFound.StatusCode);
    }

    [Fact]
    public async Task CalculateRoute_ReturnsOk_WhenRouteFound()
    {
        _googleMapsServiceMock
            .Setup(x => x.GetRouteAsync("Colombo", "Kandy", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoogleDirectionsResult
            {
                Distance = "115 km",
                Duration = "2 hours",
                Polyline = "encoded"
            });

        var result = await _controller.CalculateRoute("Colombo", "Kandy", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }

    [Fact]
    public async Task GetAlternativeRoutes_ReturnsRoutes_WhenServiceSucceeds()
    {
        _googleMapsServiceMock
            .Setup(x => x.GetAlternativeRoutesAsync("Colombo", "Kandy", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new RouteOption
                {
                    RouteId = "r-1",
                    Summary = "Fastest route",
                    Distance = "115 km",
                    DistanceValue = 115000,
                    Duration = "2 hours",
                    DurationValue = 7200,
                    FuelCost = 3864,
                    PolylinePoints = "poly-1"
                }
            ]);

        var result = await _controller.GetAlternativeRoutes("Colombo", "Kandy", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }

    [Fact]
    public async Task GetAlternativeRoutes_ReturnsNotFound_WhenNoRoutesExist()
    {
        _googleMapsServiceMock
            .Setup(x => x.GetAlternativeRoutesAsync("Colombo", "Kandy", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RouteNotFoundException("No routes found."));

        var result = await _controller.GetAlternativeRoutes("Colombo", "Kandy", CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFound.StatusCode);
    }
}
