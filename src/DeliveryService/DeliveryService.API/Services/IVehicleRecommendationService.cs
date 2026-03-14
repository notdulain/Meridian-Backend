using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DeliveryService.API.DTOs;

namespace DeliveryService.API.Services;

public interface IVehicleRecommendationService
{
    Task<IEnumerable<VehicleRecommendationDto>> GetRecommendedVehiclesAsync(int deliveryId, CancellationToken cancellationToken = default);
}
