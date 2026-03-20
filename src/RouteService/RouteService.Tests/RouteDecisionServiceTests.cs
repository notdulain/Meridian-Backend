using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RouteService.API.Models;
using RouteService.API.Repositories;
using RouteService.API.Services;
using Xunit;

namespace RouteService.Tests;

public class RouteDecisionServiceTests
{
    private readonly Mock<IRouteHistoryRepository> _repositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IGoogleMapsService> _googleMapsServiceMock = new(MockBehavior.Strict);
    private readonly Mock<ILogger<RouteDecisionService>> _loggerMock = new();

    private static IDistributedCache CreateCache() =>
        new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RouteOptimization:FuelEfficiencyKmPerLitre"] = "12",
                ["RouteOptimization:FuelPriceLkr"] = "303"
            })
            .Build();
    }

    private RouteDecisionService CreateService(IDistributedCache? cache = null)
    {
        return new RouteDecisionService(
            _repositoryMock.Object,
            _googleMapsServiceMock.Object,
            cache ?? CreateCache(),
            _loggerMock.Object,
            CreateConfiguration());
    }

    [Fact]
    public async Task CompareRoutesAsync_RanksByWeightedScoreAscending()
    {
        _repositoryMock
            .Setup(x => x.GetByOriginDestinationAsync("Colombo", "Kandy", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new RouteHistory
                {
                    RouteId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Origin = "Colombo",
                    Destination = "Kandy",
                    DistanceKm = 110,
                    DurationMinutes = 130,
                    FuelCostLkr = 4000m,
                    FuelConsumptionLitres = 13.2m,
                    Polyline = "h1",
                    Selected = true,
                    CreatedAt = DateTime.UtcNow
                }
            ]);

        _googleMapsServiceMock
            .Setup(x => x.GetAlternativeRoutesAsync("Colombo", "Kandy", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new RouteOption
                {
                    RouteId = "s-1",
                    Summary = "Primary Route",
                    Distance = "100.0 km",
                    DistanceValue = 100000,
                    Duration = "120 min",
                    DurationValue = 7200,
                    FuelCost = 2800,
                    PolylinePoints = "s1"
                }
            ]);

        var response = await CreateService().CompareRoutesAsync("Colombo", "Kandy", CancellationToken.None);

        Assert.Equal(2, response.Comparison.Count);
        Assert.Equal("s-1", response.Comparison[0].RouteId);
        Assert.False(response.Comparison[0].IsHistorical);
        Assert.Equal(648m, response.Comparison[0].RankScore);
    }

    [Fact]
    public async Task GetHistoryAsync_UsesCacheAfterFirstRead()
    {
        _repositoryMock
            .Setup(x => x.GetByOriginDestinationAsync("Colombo", "Kandy", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new RouteHistory
                {
                    RouteId = Guid.NewGuid(),
                    Origin = "Colombo",
                    Destination = "Kandy",
                    DistanceKm = 100,
                    DurationMinutes = 120,
                    FuelCostLkr = 3000m,
                    FuelConsumptionLitres = 10m,
                    Polyline = "poly",
                    Selected = true,
                    CreatedAt = DateTime.UtcNow
                }
            ]);

        var cache = CreateCache();
        var service = CreateService(cache);

        var first = await service.GetHistoryAsync("Colombo", "Kandy", CancellationToken.None);
        var second = await service.GetHistoryAsync("Colombo", "Kandy", CancellationToken.None);

        Assert.Single(first);
        Assert.Single(second);
        _repositoryMock.Verify(x => x.GetByOriginDestinationAsync("Colombo", "Kandy", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompareRoutesAsync_ReturnsSuggestions_WhenHistoryEmpty()
    {
        _repositoryMock
            .Setup(x => x.GetByOriginDestinationAsync("Colombo", "Kandy", It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _googleMapsServiceMock
            .Setup(x => x.GetAlternativeRoutesAsync("Colombo", "Kandy", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new RouteOption
                {
                    RouteId = "s-1",
                    Summary = "Primary Route",
                    Distance = "90.0 km",
                    DistanceValue = 90000,
                    Duration = "100 min",
                    DurationValue = 6000,
                    FuelCost = 2500,
                    PolylinePoints = "s1"
                }
            ]);

        var response = await CreateService().CompareRoutesAsync("Colombo", "Kandy", CancellationToken.None);

        Assert.Empty(response.HistoryRoutes);
        Assert.Single(response.SuggestedRoutes);
        Assert.Single(response.Comparison);
        Assert.False(response.Comparison[0].IsHistorical);
    }

    [Fact]
    public async Task CompareRoutesAsync_FallsBackToHistory_WhenGoogleApiFails()
    {
        _repositoryMock
            .Setup(x => x.GetByOriginDestinationAsync("Colombo", "Kandy", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new RouteHistory
                {
                    RouteId = Guid.NewGuid(),
                    Origin = "Colombo",
                    Destination = "Kandy",
                    DistanceKm = 100,
                    DurationMinutes = 120,
                    FuelCostLkr = 3000m,
                    FuelConsumptionLitres = 10m,
                    Polyline = "h1",
                    Selected = true,
                    CreatedAt = DateTime.UtcNow
                }
            ]);

        _googleMapsServiceMock
            .Setup(x => x.GetAlternativeRoutesAsync("Colombo", "Kandy", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new GoogleMapsServiceException("google failed"));

        var response = await CreateService().CompareRoutesAsync("Colombo", "Kandy", CancellationToken.None);

        Assert.Single(response.HistoryRoutes);
        Assert.Empty(response.SuggestedRoutes);
        Assert.Single(response.Comparison);
        Assert.True(response.Comparison[0].IsHistorical);
    }
}
