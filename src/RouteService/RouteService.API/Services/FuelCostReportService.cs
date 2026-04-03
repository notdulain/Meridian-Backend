using RouteService.API.Models;
using RouteService.API.Repositories;

namespace RouteService.API.Services;

public sealed class FuelCostReportService(IRouteHistoryRepository routeHistoryRepository) : IFuelCostReportService
{
    public async Task<IReadOnlyList<FuelCostAggregate>> GetFuelCostReportAsync(
        int? vehicleId,
        DateTime? startDateUtc,
        DateTime? endDateUtc,
        CancellationToken cancellationToken = default)
    {
        return await routeHistoryRepository.GetFuelCostAggregatesAsync(vehicleId, startDateUtc, endDateUtc, cancellationToken);
    }
}