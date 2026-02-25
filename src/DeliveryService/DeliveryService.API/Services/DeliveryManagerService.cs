using DeliveryService.API.DTOs;
using DeliveryService.API.Models;
using DeliveryService.API.Repositories;

namespace DeliveryService.API.Services;

public class DeliveryManagerService : IDeliveryManagerService
{
    private readonly DeliveryRepository _repository;

    public DeliveryManagerService(DeliveryRepository repository)
    {
        _repository = repository;
    }

    public async Task<DeliveryDto> CreateDeliveryAsync(CreateDeliveryRequestDto request, CancellationToken cancellationToken = default)
    {
        var delivery = new Delivery
        {
            PickupAddress = request.PickupAddress,
            DeliveryAddress = request.DeliveryAddress,
            PackageWeightKg = request.PackageWeightKg,
            PackageVolumeM3 = request.PackageVolumeM3,
            Deadline = request.Deadline,
            Status = "Pending",
            CreatedBy = request.CreatedBy
        };

        var created = await _repository.CreateAsync(delivery, cancellationToken);
        return ToDto(created);
    }

    public async Task<DeliveryDto?> GetDeliveryByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var delivery = await _repository.GetByIdAsync(id, cancellationToken);
        if (delivery is null) return null;
        return ToDto(delivery);
    }

    private static DeliveryDto ToDto(Delivery d) => new()
    {
        Id = d.Id,
        PickupAddress = d.PickupAddress,
        DeliveryAddress = d.DeliveryAddress,
        PackageWeightKg = d.PackageWeightKg,
        PackageVolumeM3 = d.PackageVolumeM3,
        Deadline = d.Deadline,
        Status = d.Status,
        AssignedVehicleId = d.AssignedVehicleId,
        AssignedDriverId = d.AssignedDriverId,
        CreatedAt = d.CreatedAt,
        UpdatedAt = d.UpdatedAt,
        CreatedBy = d.CreatedBy,
        StatusHistory = d.StatusHistory.Select(h => new DeliveryStatusHistoryDto
        {
            StatusHistoryId = h.StatusHistoryId,
            PreviousStatus = h.PreviousStatus,
            NewStatus = h.NewStatus,
            ChangedAt = h.ChangedAt,
            ChangedBy = h.ChangedBy,
            Notes = h.Notes
        }).ToList()
    };
}
