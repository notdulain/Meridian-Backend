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
}
