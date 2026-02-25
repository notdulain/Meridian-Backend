using DeliveryService.API.DTOs;

namespace DeliveryService.API.Services;

public interface IDeliveryManagerService
{
    Task<DeliveryDto> CreateDeliveryAsync(CreateDeliveryRequestDto request, CancellationToken cancellationToken = default);
    Task<DeliveryDto?> GetDeliveryByIdAsync(int id, CancellationToken cancellationToken = default);
}
