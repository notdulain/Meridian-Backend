using RouteService.API.Models;

namespace RouteService.API.Services;

public interface IRouteDecisionService
{
    Task<HistoryRouteDto> SaveSelectedRouteAsync(SelectRouteRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<HistoryRouteDto>> GetHistoryAsync(
        string origin,
        string destination,
        CancellationToken cancellationToken);

    Task<CompareRoutesResponse> CompareRoutesAsync(
        string origin,
        string destination,
        CancellationToken cancellationToken);
}
