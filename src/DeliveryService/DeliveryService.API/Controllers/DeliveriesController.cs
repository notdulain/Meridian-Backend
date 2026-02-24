using Microsoft.AspNetCore.Mvc;
using DeliveryService.API.DTOs;
using DeliveryService.API.Services;

namespace DeliveryService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DeliveriesController : ControllerBase
{
    private readonly IDeliveryManagerService _deliveryManagerService;

    public DeliveriesController(IDeliveryManagerService deliveryManagerService)
    {
        _deliveryManagerService = deliveryManagerService;
    }

    /// <summary>Create a new delivery request to initiate logistics operations.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(DeliveryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DeliveryDto>> Create([FromBody] CreateDeliveryRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PickupAddress))
            return BadRequest("PickupAddress is required.");
        if (string.IsNullOrWhiteSpace(request.DeliveryAddress))
            return BadRequest("DeliveryAddress is required.");
        if (request.PackageWeightKg <= 0)
            return BadRequest("PackageWeightKg must be greater than 0.");
        if (request.PackageVolumeM3 <= 0)
            return BadRequest("PackageVolumeM3 must be greater than 0.");
        if (string.IsNullOrWhiteSpace(request.CreatedBy))
            return BadRequest("CreatedBy is required.");

        var delivery = await _deliveryManagerService.CreateDeliveryAsync(request, cancellationToken);
        return Created($"/api/deliveries/{delivery.Id}", delivery);
    }
}
