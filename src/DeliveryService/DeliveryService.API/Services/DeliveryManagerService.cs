using System;
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
		var models = await _repo.GetAllAsync();
		var query = models.AsQueryable();

		if (!string.IsNullOrWhiteSpace(status))
		{
			var s = status.Trim();
			query = query.Where(m => !string.IsNullOrEmpty(m.Status) && m.Status.Equals(s, StringComparison.OrdinalIgnoreCase));
		}

		if (!string.IsNullOrWhiteSpace(destination))
		{
			var d = destination.Trim();
			query = query.Where(m => !string.IsNullOrEmpty(m.Destination) && m.Destination.IndexOf(d, StringComparison.OrdinalIgnoreCase) >= 0);
		}

		if (date.HasValue)
		{
			var dt = date.Value.Date;
			query = query.Where(m => m.CreatedAt.Date == dt);
		}

		if (!string.IsNullOrWhiteSpace(orderNumber))
		{
			var o = orderNumber.Trim();
			query = query.Where(m => !string.IsNullOrEmpty(m.OrderNumber) && m.OrderNumber.Equals(o, StringComparison.OrdinalIgnoreCase));
		}

		page = Math.Max(1, page);
		pageSize = Math.Clamp(pageSize, 1, 100);

		var paged = query.Skip((page - 1) * pageSize).Take(pageSize);

		return paged.Select(m => new DeliveryDto
		{
			Id = m.Id,
			OrderNumber = m.OrderNumber,
			Status = m.Status,
			Destination = m.Destination,
			CreatedAt = m.CreatedAt
		});
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
        CreatedBy = d.CreatedBy
    };
}
