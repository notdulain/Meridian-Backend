using Microsoft.AspNetCore.Mvc;
using Moq;
using RouteService.API.Controllers;
using RouteService.API.Models;
using RouteService.API.Services;
using Xunit;

namespace RouteService.Tests;

public class RoutesControllerTests
{
    private readonly Mock<IGoogleMapsService> _googleMapsServiceMock = new(MockBehavior.Strict);
    private readonly Mock<IRouteDecisionService> _routeDecisionServiceMock = new(MockBehavior.Strict);

    private RoutesController CreateController() =>
        new(_googleMapsServiceMock.Object, _routeDecisionServiceMock.Object);

    [Fact]
    public async Task GetAlternativeRoutes_ReturnsOk_WhenRoutesFound()
    {
        _googleMapsServiceMock
            .Setup(x => x.GetAlternativeRoutesAsync("Colombo", "Kandy", It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new RouteOption
                {
                    RouteId = "r-1",
                    Summary = "Primary Route",
                    Distance = "115.0 km",
                    DistanceValue = 115000,
                    Duration = "2 hr",
                    DurationValue = 7200,
                    FuelCost = 2900,
                    PolylinePoints = "poly-1"
                }
            ]);

        var result = await CreateController().GetAlternativeRoutes("Colombo", "Kandy", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }

    [Fact]
    public async Task SelectRoute_ReturnsBadRequest_ForInvalidBody()
    {
        var result = await CreateController().SelectRoute(
            new SelectRouteRequest
            {
                Origin = string.Empty,
                Destination = "Kandy",
                Route = new RouteOption
                {
                    RouteId = "id",
                    Summary = "s",
                    Distance = "1 km",
                    Duration = "1 min",
                    PolylinePoints = "p"
                }
            },
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task SelectRoute_ReturnsSavedRoute()
    {
        var request = new SelectRouteRequest
        {
            Origin = "Colombo",
            Destination = "Kandy",
            Route = new RouteOption
            {
                RouteId = "id",
                Summary = "s",
                Distance = "100 km",
                DistanceValue = 100000,
                Duration = "2 hr",
                DurationValue = 7200,
                FuelCost = 2500,
                PolylinePoints = "p"
            }
        };

        _routeDecisionServiceMock
            .Setup(x => x.SaveSelectedRouteAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HistoryRouteDto
            {
                RouteId = Guid.NewGuid(),
                Origin = "Colombo",
                Destination = "Kandy",
                DistanceKm = 100,
                DurationMinutes = 120,
                FuelCostLkr = 2500,
                FuelConsumptionLitres = 8.2m,
                Polyline = "p",
                Selected = true,
                CreatedAt = DateTime.UtcNow
            });

        var result = await CreateController().SelectRoute(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }

    [Fact]
    public async Task GetRouteHistory_ReturnsBadRequest_WhenQueryMissing()
    {
        var result = await CreateController().GetRouteHistory(" ", "Kandy", CancellationToken.None);
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task CompareRoutes_ReturnsCombinedResponse()
    {
        _routeDecisionServiceMock
            .Setup(x => x.CompareRoutesAsync("Colombo", "Kandy", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompareRoutesResponse
            {
                HistoryRoutes = [],
                SuggestedRoutes =
                [
                    new SuggestedRouteDto
                    {
                        RouteId = "r1",
                        Summary = "Primary Route",
                        DistanceKm = 100,
                        DurationMinutes = 120,
                        FuelCostLkr = 3000,
                        Polyline = "p"
                    }
                ],
                Comparison =
                [
                    new ComparisonRouteItem
                    {
                        RouteId = "r1",
                        IsHistorical = false,
                        DistanceKm = 100,
                        DurationMinutes = 120,
                        FuelCostLkr = 3000,
                        RankScore = 688
                    }
                ]
            });

        var result = await CreateController().CompareRoutes("Colombo", "Kandy", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        var response = Assert.IsType<CompareRoutesResponse>(ok.Value);
        Assert.Single(response.SuggestedRoutes);
        Assert.Single(response.Comparison);
    }
}
