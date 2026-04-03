using RouteService.API.Models;
using RouteService.API.Repositories;

namespace RouteService.API.Services;

public sealed class FuelCostReportService(IRouteHistoryRepository routeHistoryRepository) : IFuelCostReportService
{
    public async Task<IReadOnlyList<FuelCostAggregate>> GetFuelCostReportAsync(CancellationToken cancellationToken = default)
    {
        return await routeHistoryRepository.GetFuelCostAggregatesAsync(null, null, cancellationToken);
    }
}