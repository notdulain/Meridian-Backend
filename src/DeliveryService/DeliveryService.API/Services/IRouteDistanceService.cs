namespace DeliveryService.API.Services;

public interface IRouteDistanceService
{
    Task<double?> GetDistanceInKilometersAsync(string origin, string destination, CancellationToken cancellationToken = default);
}
