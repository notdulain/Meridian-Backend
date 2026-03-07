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

// ---- Route parsing, fuel calculation, and ranking (GoogleMapsService with fake HTTP, no real API) ----

/// <summary>
/// Unit tests for route parsing, fuel calculation, and ranking logic in GoogleMapsService (no real API calls).
/// Uses a fake HTTP handler that returns predefined Google Routes API JSON.
/// </summary>
public class GoogleMapsServiceRankingTests
{
    private const string FakeApiKey = "test-api-key";

    private static GoogleMapsService CreateServiceWithFakeResponse(string routesJson)
    {
        var handler = new FakeRoutesHttpHandler(routesJson);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://routes.googleapis.com") };
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["GoogleMaps:ApiKey"]).Returns(FakeApiKey);
        var logger = new Mock<ILogger<GoogleMapsService>>();
        return new GoogleMapsService(client, config.Object, logger.Object);
    }

    [Fact]
    public async Task GetRankedRoutesAsync_ConvertsMetersToKm_Correctly()
    {
        var routesJson = "{\"routes\":[{\"distanceMeters\":120000,\"duration\":\"3600s\",\"polyline\":{\"encodedPolyline\":\"p1\"},\"routeLabels\":[\"DEFAULT_ROUTE\"]}]}";
        var service = CreateServiceWithFakeResponse(routesJson);

        var response = await service.GetRankedRoutesAsync("Colombo", "Kandy", CancellationToken.None);

        Assert.True(response.Success);
        var route = Assert.Single(response.Routes);
        Assert.Equal(120, route.DistanceKm);
    }

    [Fact]
    public async Task GetRankedRoutesAsync_ConvertsSecondsToHours_Correctly()
    {
        var routesJson = "{\"routes\":[{\"distanceMeters\":100000,\"duration\":\"8100s\",\"polyline\":{\"encodedPolyline\":\"p1\"},\"routeLabels\":[\"DEFAULT_ROUTE\"]}]}";
        var service = CreateServiceWithFakeResponse(routesJson);

        var response = await service.GetRankedRoutesAsync("Colombo", "Kandy", CancellationToken.None);

        Assert.True(response.Success);
        var route = Assert.Single(response.Routes);
        Assert.Equal(2.25, route.DurationHours);
    }

    [Fact]
    public async Task GetRankedRoutesAsync_ExtractsPolyline_Correctly()
    {
        var routesJson = "{\"routes\":[{\"distanceMeters\":100000,\"duration\":\"3600s\",\"polyline\":{\"encodedPolyline\":\"encoded_polyline_abc123\"},\"routeLabels\":[\"DEFAULT_ROUTE\"]}]}";
        var service = CreateServiceWithFakeResponse(routesJson);

        var response = await service.GetRankedRoutesAsync("Colombo", "Kandy", CancellationToken.None);

        Assert.True(response.Success);
        var route = Assert.Single(response.Routes);
        Assert.Equal("encoded_polyline_abc123", route.PolylinePoints);
    }

    [Fact]
    public async Task GetRankedRoutesAsync_CalculatesFuelConsumptionLitres_Correctly()
    {
        var routesJson = "{\"routes\":[{\"distanceMeters\":120000,\"duration\":\"3600s\",\"polyline\":{\"encodedPolyline\":\"p1\"},\"routeLabels\":[\"DEFAULT_ROUTE\"]}]}";
        var service = CreateServiceWithFakeResponse(routesJson);

        var response = await service.GetRankedRoutesAsync("Colombo", "Kandy", CancellationToken.None);

        Assert.True(response.Success);
        var route = Assert.Single(response.Routes);
        Assert.Equal(10, route.FuelConsumptionLitres);
    }

    [Fact]
    public async Task GetRankedRoutesAsync_CalculatesFuelCostLKR_Correctly()
    {
        var routesJson = "{\"routes\":[{\"distanceMeters\":120000,\"duration\":\"3600s\",\"polyline\":{\"encodedPolyline\":\"p1\"},\"routeLabels\":[\"DEFAULT_ROUTE\"]}]}";
        var service = CreateServiceWithFakeResponse(routesJson);

        var response = await service.GetRankedRoutesAsync("Colombo", "Kandy", CancellationToken.None);

        Assert.True(response.Success);
        var route = Assert.Single(response.Routes);
        Assert.Equal(4500, route.FuelCostLKR);
    }

    [Fact]
    public async Task GetRankedRoutesAsync_CalculatesFuelCorrectly_ForDifferentDistances()
    {
        var routesJson = "{\"routes\":[{\"distanceMeters\":60000,\"duration\":\"3600s\",\"polyline\":{\"encodedPolyline\":\"a\"},\"routeLabels\":[\"DEFAULT_ROUTE\"]},{\"distanceMeters\":120000,\"duration\":\"7200s\",\"polyline\":{\"encodedPolyline\":\"b\"},\"routeLabels\":[\"DEFAULT_ROUTE_ALTERNATE\"]}]}";
        var service = CreateServiceWithFakeResponse(routesJson);

        var response = await service.GetRankedRoutesAsync("Colombo", "Kandy", CancellationToken.None);

        Assert.True(response.Success);
        Assert.Equal(2, response.Routes.Count);
        var routeA = response.Routes.First(r => r.DistanceKm == 60);
        var routeB = response.Routes.First(r => r.DistanceKm == 120);
        Assert.Equal(5, routeA.FuelConsumptionLitres);
        Assert.Equal(2250, routeA.FuelCostLKR);
        Assert.Equal(10, routeB.FuelConsumptionLitres);
        Assert.Equal(4500, routeB.FuelCostLKR);
    }

    [Fact]
    public async Task GetRankedRoutesAsync_RoutesSorted_ByFuelCostThenDistanceThenDuration()
    {
        var routesJson = "{\"routes\":[{\"distanceMeters\":120000,\"duration\":\"7200s\",\"polyline\":{\"encodedPolyline\":\"x\"},\"routeLabels\":[\"DEFAULT_ROUTE\"]},{\"distanceMeters\":115000,\"duration\":\"8100s\",\"polyline\":{\"encodedPolyline\":\"y\"},\"routeLabels\":[\"DEFAULT_ROUTE_ALTERNATE\"]},{\"distanceMeters\":105000,\"duration\":\"9000s\",\"polyline\":{\"encodedPolyline\":\"z\"},\"routeLabels\":[]}]}";
        var service = CreateServiceWithFakeResponse(routesJson);

        var response = await service.GetRankedRoutesAsync("Colombo", "Kandy", CancellationToken.None);

        Assert.True(response.Success);
        Assert.Equal(3, response.Routes.Count);
        Assert.Equal(1, response.Routes[0].Rank);
        Assert.Equal(2, response.Routes[1].Rank);
        Assert.Equal(3, response.Routes[2].Rank);
        Assert.True(response.Routes[0].FuelCostLKR <= response.Routes[1].FuelCostLKR);
        Assert.True(response.Routes[1].FuelCostLKR <= response.Routes[2].FuelCostLKR);
    }

    [Fact]
    public async Task GetRankedRoutesAsync_RecommendedRouteMarkedCorrectly()
    {
        var routesJson = "{\"routes\":[{\"distanceMeters\":120000,\"duration\":\"7200s\",\"polyline\":{\"encodedPolyline\":\"a\"},\"routeLabels\":[\"DEFAULT_ROUTE\"]},{\"distanceMeters\":115000,\"duration\":\"8100s\",\"polyline\":{\"encodedPolyline\":\"b\"},\"routeLabels\":[\"DEFAULT_ROUTE_ALTERNATE\"]}]}";
        var service = CreateServiceWithFakeResponse(routesJson);

        var response = await service.GetRankedRoutesAsync("Colombo", "Kandy", CancellationToken.None);

        Assert.True(response.Success);
        var recommended = response.Routes.Single(r => r.IsRecommended);
        Assert.Equal(1, recommended.Rank);
        Assert.Equal(response.RecommendedRouteId, recommended.RouteId);
    }

    [Fact]
    public async Task GetRankedRoutesAsync_RankValuesAssignedCorrectly()
    {
        var routesJson = "{\"routes\":[{\"distanceMeters\":120000,\"duration\":\"7200s\",\"polyline\":{\"encodedPolyline\":\"a\"},\"routeLabels\":[]},{\"distanceMeters\":115000,\"duration\":\"8100s\",\"polyline\":{\"encodedPolyline\":\"b\"},\"routeLabels\":[]},{\"distanceMeters\":110000,\"duration\":\"6600s\",\"polyline\":{\"encodedPolyline\":\"c\"},\"routeLabels\":[]}]}";
        var service = CreateServiceWithFakeResponse(routesJson);

        var response = await service.GetRankedRoutesAsync("Colombo", "Kandy", CancellationToken.None);

        Assert.True(response.Success);
        Assert.Equal(3, response.Routes.Count);
        var ranks = response.Routes.Select(r => r.Rank).OrderBy(x => x).ToList();
        Assert.Equal(new[] { 1, 2, 3 }, ranks);
    }

    [Fact]
    public async Task GetRankedRoutesAsync_ThrowsRouteNotFoundException_WhenNoRoutesReturned()
    {
        var routesJson = "{\"routes\":[]}";
        var service = CreateServiceWithFakeResponse(routesJson);

        await Assert.ThrowsAsync<RouteNotFoundException>(
            () => service.GetRankedRoutesAsync("Colombo", "Kandy", CancellationToken.None));
    }

    private sealed class FakeRoutesHttpHandler : HttpMessageHandler
    {
        private readonly string _responseJson;

        public FakeRoutesHttpHandler(string responseJson) => _responseJson = responseJson;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseJson, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }
}
