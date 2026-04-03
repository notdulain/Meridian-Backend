using RouteService.API.Models;

namespace RouteService.API.Services;

public interface IFuelCostReportService
{
    Task<IReadOnlyList<FuelCostAggregate>> GetFuelCostReportAsync(
        int? vehicleId,
        DateTime? startDateUtc,
        DateTime? endDateUtc,
        CancellationToken cancellationToken = default);
}