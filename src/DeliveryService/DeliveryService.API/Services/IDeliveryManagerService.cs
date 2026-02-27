using DeliveryService.API.DTOs;

namespace DeliveryService.API.Services;

public interface IDeliveryManagerService
{
    Task<DeliveryDto> CreateDeliveryAsync(CreateDeliveryRequestDto request, CancellationToken cancellationToken = default);
    Task<DeliveryDto?> GetDeliveryByIdAsync(int id, CancellationToken cancellationToken = default);

    // Returns deliveries matching the provided filters with paging
    Task<IEnumerable<DeliveryDto>> GetAllDeliveriesAsync(
        string? status = null,
        string? destination = null,
        DateTime? date = null,
        string? orderNumber = null,
        int page = 1,
        int pageSize = 50);
}
