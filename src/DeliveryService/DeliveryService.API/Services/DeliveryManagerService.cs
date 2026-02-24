using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeliveryService.API.DTOs;
using DeliveryService.API.Repositories;

namespace DeliveryService.API.Services;

public class DeliveryManagerService
{
	private readonly DeliveryRepository _repo;

	public DeliveryManagerService(DeliveryRepository repo)
	{
		_repo = repo;
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
}
