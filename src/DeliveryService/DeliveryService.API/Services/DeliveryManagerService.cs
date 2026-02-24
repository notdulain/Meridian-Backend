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

	public async Task<IEnumerable<DeliveryDto>> GetAllDeliveriesAsync()
	{
		var models = await _repo.GetAllAsync();
		return models.Select(m => new DeliveryDto
		{
			Id = m.Id,
			OrderNumber = m.OrderNumber,
			Status = m.Status,
			Destination = m.Destination,
			CreatedAt = m.CreatedAt
		});
	}
}
