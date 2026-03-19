using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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

    // ---- Rank endpoint: success and response structure ----

    [Fact]
    public async Task GetRankedRoutes_Returns200Ok_WithRouteList()
    {
        var rankingResponse = new RouteRankingResponse
        {
            Success = true,
            Routes =
            [
                new RouteRankedOption
                {
                    RouteId = "route-1",
                    Rank = 1,
                    Summary = "Primary Route",
                    DistanceKm = 108,
                    DurationHours = 2.17,
                    FuelConsumptionLitres = 9,
                    FuelCostLKR = 4050,
                    PolylinePoints = "poly1",
                    IsRecommended = true
                }
            ],
            RecommendedRouteId = "route-1"
        };

        _googleMapsServiceMock
            .Setup(x => x.GetRankedRoutesAsync("Colombo", "Kandy", It.IsAny<CancellationToken>()))
            .ReturnsAsync(rankingResponse);

        var result = await _controller.GetRankedRoutes("Colombo", "Kandy", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }

    [Fact]
    public async Task GetRankedRoutes_ResponseContainsRouteList()
    {
        var rankingResponse = new RouteRankingResponse
        {
            Success = true,
            Routes =
            [
                new RouteRankedOption
                {
                    RouteId = "route-1",
                    Rank = 1,
                    Summary = "Primary",
                    DistanceKm = 100,
                    DurationHours = 2,
                    FuelConsumptionLitres = 8.33,
                    FuelCostLKR = 3750,
                    PolylinePoints = "enc",
                    IsRecommended = true
                }
            ],
            RecommendedRouteId = "route-1"
        };

        _googleMapsServiceMock
            .Setup(x => x.GetRankedRoutesAsync("Colombo", "Kandy", It.IsAny<CancellationToken>()))
            .ReturnsAsync(rankingResponse);

        var result = await _controller.GetRankedRoutes("Colombo", "Kandy", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<RouteRankingResponse>(ok.Value);
        Assert.NotNull(response.Routes);
        Assert.Single(response.Routes);
        Assert.Equal("route-1", response.Routes[0].RouteId);
    }

    [Fact]
    public async Task GetRankedRoutes_ResponseContainsRecommendedRouteId()
    {
        var rankingResponse = new RouteRankingResponse
        {
            Success = true,
            Routes =
            [
                new RouteRankedOption
                {
                    RouteId = "recommended-id",
                    Rank = 1,
                    Summary = "Best",
                    DistanceKm = 100,
                    DurationHours = 2,
                    FuelConsumptionLitres = 8,
                    FuelCostLKR = 3600,
                    PolylinePoints = "p",
                    IsRecommended = true
                }
            ],
            RecommendedRouteId = "recommended-id"
        };

        _googleMapsServiceMock
            .Setup(x => x.GetRankedRoutesAsync("Colombo", "Kandy", It.IsAny<CancellationToken>()))
            .ReturnsAsync(rankingResponse);

        var result = await _controller.GetRankedRoutes("Colombo", "Kandy", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<RouteRankingResponse>(ok.Value);
        Assert.Equal("recommended-id", response.RecommendedRouteId);
    }

    [Fact]
    public async Task GetRankedRoutes_ResponseStructureIsCorrect()
    {
        var rankingResponse = new RouteRankingResponse
        {
            Success = true,
            Routes =
            [
                new RouteRankedOption
                {
                    RouteId = "r1",
                    Rank = 1,
                    Summary = "Primary Route",
                    DistanceKm = 115,
                    DurationHours = 2,
                    FuelConsumptionLitres = 9.58,
                    FuelCostLKR = 4312.5,
                    PolylinePoints = "poly",
                    IsRecommended = true
                }
            ],
            RecommendedRouteId = "r1"
        };

        _googleMapsServiceMock
            .Setup(x => x.GetRankedRoutesAsync("Colombo", "Kandy", It.IsAny<CancellationToken>()))
            .ReturnsAsync(rankingResponse);

        var result = await _controller.GetRankedRoutes("Colombo", "Kandy", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<RouteRankingResponse>(ok.Value);
        Assert.True(response.Success);
        Assert.NotEmpty(response.Routes);
        Assert.False(string.IsNullOrEmpty(response.RecommendedRouteId));
        var route = response.Routes[0];
        Assert.True(route.RouteId.Length > 0);
        Assert.True(route.Rank >= 1);
        Assert.True(route.DistanceKm > 0);
        Assert.True(route.DurationHours > 0);
        Assert.True(route.FuelConsumptionLitres > 0);
        Assert.True(route.FuelCostLKR > 0);
        Assert.True(route.IsRecommended);
    }

    // ---- Rank endpoint: error handling ----

    [Fact]
    public async Task GetRankedRoutes_ReturnsBadRequest_WhenOriginMissing()
    {
        var result = await _controller.GetRankedRoutes("", "Kandy", CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task GetRankedRoutes_ReturnsBadRequest_WhenDestinationMissing()
    {
        var result = await _controller.GetRankedRoutes("Colombo", "   ", CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task GetRankedRoutes_ReturnsNotFound_WhenNoRoutesReturned()
    {
        _googleMapsServiceMock
            .Setup(x => x.GetRankedRoutesAsync("Colombo", "Kandy", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RouteNotFoundException("No routes found for the provided origin and destination."));

        var result = await _controller.GetRankedRoutes("Colombo", "Kandy", CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFound.StatusCode);
    }

    [Fact]
    public async Task GetRankedRoutes_Returns502BadGateway_WhenGoogleApiFails()
    {
        _googleMapsServiceMock
            .Setup(x => x.GetRankedRoutesAsync("Colombo", "Kandy", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new GoogleMapsServiceException("Unable to retrieve route information from Google Maps."));

        var result = await _controller.GetRankedRoutes("Colombo", "Kandy", CancellationToken.None);

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(502, statusResult.StatusCode);
    }
}
