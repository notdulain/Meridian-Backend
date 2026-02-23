using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DeliveryService.API.Models;

namespace DeliveryService.API.Repositories;

public class DeliveryRepository
{
	// Minimal in-memory repository for the GET endpoint. Replace with real DB access later.
	private readonly List<Delivery> _seed = new()
	{
		new Delivery { Id = 1, OrderNumber = "ORD-1001", Status = "Pending", Destination = "123 Main St", CreatedAt = DateTime.UtcNow.AddHours(-5) },
		new Delivery { Id = 2, OrderNumber = "ORD-1002", Status = "In Transit", Destination = "456 Oak Ave", CreatedAt = DateTime.UtcNow.AddHours(-2) },
	};

	public Task<IEnumerable<Delivery>> GetAllAsync()
	{
		return Task.FromResult<IEnumerable<Delivery>>(_seed);
	}
}
