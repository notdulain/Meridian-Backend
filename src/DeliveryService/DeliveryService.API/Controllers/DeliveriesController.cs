using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using DeliveryService.API.Services;
using DeliveryService.API.DTOs;
using System;

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
	public async Task<ActionResult<IEnumerable<DeliveryDto>>> Get(
		[FromQuery] string? status,
		[FromQuery] string? destination,
		[FromQuery] DateTime? date,
		[FromQuery] string? orderNumber,
		[FromQuery] int page = 1,
		[FromQuery] int pageSize = 50)
	{
		var deliveries = await _service.GetAllDeliveriesAsync(status, destination, date, orderNumber, page, pageSize);
		return Ok(deliveries);
	}
}
