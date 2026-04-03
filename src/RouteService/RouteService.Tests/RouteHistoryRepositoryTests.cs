using Microsoft.EntityFrameworkCore;
using RouteService.API.Data;
using RouteService.API.Models;
using RouteService.API.Repositories;
using Xunit;

namespace RouteService.Tests;

public class RouteHistoryRepositoryTests
{
    private static RouteServiceDbContext CreateDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<RouteServiceDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new RouteServiceDbContext(options);
    }

    [Fact]
    public async Task GetFuelCostAggregatesAsync_GroupsByVehicleDriverAndDay()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        var dayOne = new DateTime(2026, 04, 01, 8, 0, 0, DateTimeKind.Utc);
        var dayTwo = new DateTime(2026, 04, 02, 8, 0, 0, DateTimeKind.Utc);

        await using (var seededContext = CreateDbContext(databaseName))
        {
            seededContext.RouteHistories.AddRange(
                new RouteHistory
                {
                    RouteId = Guid.NewGuid(),
                    Origin = "Colombo",
                    Destination = "Kandy",
                    VehicleId = 11,
                    DriverId = 21,
                    DistanceKm = 100,
                    DurationMinutes = 120,
                    FuelCostLkr = 3000m,
                    FuelConsumptionLitres = 10m,
                    Polyline = "p1",
                    Selected = true,
                    CreatedAt = dayOne
                },
                new RouteHistory
                {
                    RouteId = Guid.NewGuid(),
                    Origin = "Colombo",
                    Destination = "Kandy",
                    VehicleId = 11,
                    DriverId = 21,
                    DistanceKm = 120,
                    DurationMinutes = 140,
                    FuelCostLkr = 3600m,
                    FuelConsumptionLitres = 12m,
                    Polyline = "p2",
                    Selected = true,
                    CreatedAt = dayOne.AddHours(3)
                },
                new RouteHistory
                {
                    RouteId = Guid.NewGuid(),
                    Origin = "Colombo",
                    Destination = "Galle",
                    VehicleId = 11,
                    DriverId = 22,
                    DistanceKm = 80,
                    DurationMinutes = 100,
                    FuelCostLkr = 2400m,
                    FuelConsumptionLitres = 8m,
                    Polyline = "p3",
                    Selected = true,
                    CreatedAt = dayTwo
                },
                new RouteHistory
                {
                    RouteId = Guid.NewGuid(),
                    Origin = "Colombo",
                    Destination = "Matara",
                    VehicleId = null,
                    DriverId = 22,
                    DistanceKm = 50,
                    DurationMinutes = 60,
                    FuelCostLkr = 1500m,
                    FuelConsumptionLitres = 5m,
                    Polyline = "p4",
                    Selected = true,
                    CreatedAt = dayTwo
                });

            await seededContext.SaveChangesAsync();
        }

        await using var queryContext = CreateDbContext(databaseName);
        var repository = new RouteHistoryRepository(queryContext);

        var result = await repository.GetFuelCostAggregatesAsync(null, null, CancellationToken.None);

        Assert.Equal(2, result.Count);

        var first = Assert.Single(result, x => x.VehicleId == 11 && x.DriverId == 21 && x.PeriodStartUtc.Date == dayOne.Date);
        Assert.Equal(2, first.TripCount);
        Assert.Equal(220, first.TotalDistanceKm);
        Assert.Equal(22m, first.TotalFuelConsumptionLitres);
        Assert.Equal(6600m, first.TotalFuelCostLkr);

        var second = Assert.Single(result, x => x.VehicleId == 11 && x.DriverId == 22 && x.PeriodStartUtc.Date == dayTwo.Date);
        Assert.Equal(1, second.TripCount);
        Assert.Equal(80, second.TotalDistanceKm);
        Assert.Equal(8m, second.TotalFuelConsumptionLitres);
        Assert.Equal(2400m, second.TotalFuelCostLkr);
    }

    [Fact]
    public async Task GetFuelCostAggregatesAsync_AppliesDateRangeFilters()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        var dayOne = new DateTime(2026, 04, 01, 8, 0, 0, DateTimeKind.Utc);
        var dayTwo = new DateTime(2026, 04, 02, 8, 0, 0, DateTimeKind.Utc);

        await using (var seededContext = CreateDbContext(databaseName))
        {
            seededContext.RouteHistories.AddRange(
                new RouteHistory
                {
                    RouteId = Guid.NewGuid(),
                    Origin = "A",
                    Destination = "B",
                    VehicleId = 1,
                    DriverId = 1,
                    DistanceKm = 10,
                    DurationMinutes = 20,
                    FuelCostLkr = 200m,
                    FuelConsumptionLitres = 1m,
                    Polyline = "p1",
                    Selected = true,
                    CreatedAt = dayOne
                },
                new RouteHistory
                {
                    RouteId = Guid.NewGuid(),
                    Origin = "A",
                    Destination = "C",
                    VehicleId = 1,
                    DriverId = 1,
                    DistanceKm = 30,
                    DurationMinutes = 40,
                    FuelCostLkr = 600m,
                    FuelConsumptionLitres = 3m,
                    Polyline = "p2",
                    Selected = true,
                    CreatedAt = dayTwo
                });

            await seededContext.SaveChangesAsync();
        }

        await using var queryContext = CreateDbContext(databaseName);
        var repository = new RouteHistoryRepository(queryContext);

        var result = await repository.GetFuelCostAggregatesAsync(dayTwo, null, CancellationToken.None);

        var aggregate = Assert.Single(result);
        Assert.Equal(dayTwo.Date, aggregate.PeriodStartUtc.Date);
        Assert.Equal(600m, aggregate.TotalFuelCostLkr);
    }
}
