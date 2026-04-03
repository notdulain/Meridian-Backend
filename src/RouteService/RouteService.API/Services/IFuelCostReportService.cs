using RouteService.API.Models;

namespace RouteService.API.Services;

public interface IFuelCostReportService
{
    Task<IReadOnlyList<FuelCostAggregate>> GetFuelCostReportAsync(CancellationToken cancellationToken = default);
}