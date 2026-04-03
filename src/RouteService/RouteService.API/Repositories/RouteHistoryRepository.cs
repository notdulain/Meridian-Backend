using Microsoft.EntityFrameworkCore;
using RouteService.API.Data;
using RouteService.API.Models;

namespace RouteService.API.Repositories;

public sealed class RouteHistoryRepository(RouteServiceDbContext dbContext) : IRouteHistoryRepository
{
    public async Task<RouteHistory> AddAsync(RouteHistory routeHistory, CancellationToken cancellationToken)
    {
        dbContext.RouteHistories.Add(routeHistory);
        await dbContext.SaveChangesAsync(cancellationToken);
        return routeHistory;
    }

    public async Task<IReadOnlyList<RouteHistory>> GetByOriginDestinationAsync(
        string origin,
        string destination,
        CancellationToken cancellationToken)
    {
        var normalizedOrigin = origin.Trim();
        var normalizedDestination = destination.Trim();

        return await dbContext.RouteHistories
            .AsNoTracking()
            .Where(x => x.Origin == normalizedOrigin && x.Destination == normalizedDestination)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FuelCostAggregate>> GetFuelCostAggregatesAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken)
    {
        var query = dbContext.RouteHistories
            .AsNoTracking()
            .Where(x => x.VehicleId.HasValue && x.DriverId.HasValue);

        if (fromUtc.HasValue)
            query = query.Where(x => x.CreatedAt >= fromUtc.Value);

        if (toUtc.HasValue)
            query = query.Where(x => x.CreatedAt <= toUtc.Value);

        return await query
            .GroupBy(x => new
            {
                VehicleId = x.VehicleId!.Value,
                DriverId = x.DriverId!.Value,
                PeriodStartUtc = x.CreatedAt.Date
            })
            .Select(g => new FuelCostAggregate
            {
                VehicleId = g.Key.VehicleId,
                DriverId = g.Key.DriverId,
                PeriodStartUtc = g.Key.PeriodStartUtc,
                TripCount = g.Count(),
                TotalDistanceKm = g.Sum(x => x.DistanceKm),
                TotalFuelConsumptionLitres = g.Sum(x => x.FuelConsumptionLitres),
                TotalFuelCostLkr = g.Sum(x => x.FuelCostLkr)
            })
            .OrderByDescending(x => x.PeriodStartUtc)
            .ThenBy(x => x.VehicleId)
            .ThenBy(x => x.DriverId)
            .ToListAsync(cancellationToken);
    }
}
