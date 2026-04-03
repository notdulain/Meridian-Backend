using RouteService.API.Models;

namespace RouteService.API.Repositories;

public interface IRouteHistoryRepository
{
    Task<RouteHistory> AddAsync(RouteHistory routeHistory, CancellationToken cancellationToken);

    Task<IReadOnlyList<RouteHistory>> GetByOriginDestinationAsync(
        string origin,
        string destination,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<FuelCostAggregate>> GetFuelCostAggregatesAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken);
}
