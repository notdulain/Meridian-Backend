using System;
using System.Linq;
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

	public async Task<IEnumerable<DeliveryDto>> GetAllDeliveriesAsync(
		string? status = null,
		string? destination = null,
		DateTime? date = null,
		string? orderNumber = null,
		int page = 1,
		int pageSize = 50)
	{
		var models = await _repository.GetAllAsync();
		var query = models.AsQueryable();

		if (!string.IsNullOrWhiteSpace(status))
		{
			var s = status.Trim();
			query = query.Where(m => !string.IsNullOrEmpty(m.Status) && m.Status.Equals(s, StringComparison.OrdinalIgnoreCase));
		}

		if (!string.IsNullOrWhiteSpace(destination))
		{
			var d = destination.Trim();
			// filter against delivery address
			query = query.Where(m => !string.IsNullOrEmpty(m.DeliveryAddress) && m.DeliveryAddress.IndexOf(d, StringComparison.OrdinalIgnoreCase) >= 0);
		}

		if (date.HasValue)
		{
			var dt = date.Value.Date;
			query = query.Where(m => m.CreatedAt.Date == dt);
		}

		if (!string.IsNullOrWhiteSpace(orderNumber))
		{
			// interpret orderNumber as numeric id when possible
			if (int.TryParse(orderNumber.Trim(), out var id))
			{
				query = query.Where(m => m.Id == id);
			}
		}

		page = Math.Max(1, page);
		pageSize = Math.Clamp(pageSize, 1, 100);

		var paged = query.Skip((page - 1) * pageSize).Take(pageSize);

		return paged.Select(ToDto);
	}

	public async Task<DeliveryDto?> GetDeliveryByIdAsync(int id, CancellationToken cancellationToken = default)
	{
		var delivery = await _repository.GetByIdAsync(id, cancellationToken);
		return delivery is null ? null : ToDto(delivery);
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
