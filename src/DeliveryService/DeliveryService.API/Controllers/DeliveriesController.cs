using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using DeliveryService.API.Services;
using DeliveryService.API.DTOs;

namespace DeliveryService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DeliveriesController : ControllerBase
{
	private readonly DeliveryManagerService _service;

	public DeliveriesController(DeliveryManagerService service)
	{
		_service = service;
	}

	[HttpGet]
	public async Task<ActionResult<IEnumerable<DeliveryDto>>> Get()
	{
		var deliveries = await _service.GetAllDeliveriesAsync();
		return Ok(deliveries);
	}
}
