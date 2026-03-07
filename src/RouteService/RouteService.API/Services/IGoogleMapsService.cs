using RouteService.API.Models;

namespace RouteService.API.Services;

public interface IGoogleMapsService
{
    Task<GoogleDirectionsResult> GetRouteAsync(string origin, string destination, CancellationToken cancellationToken = default);

    Task<List<RouteOption>> GetAlternativeRoutesAsync(
        string origin,
        string destination,
        CancellationToken cancellationToken);

    Task<List<RouteComparison>> GetRouteComparisonsAsync(
        string origin,
        string destination,
        CancellationToken cancellationToken);

    Task<RouteRankingResponse> GetRankedRoutesAsync(
        string origin,
        string destination,
        CancellationToken cancellationToken);
}
