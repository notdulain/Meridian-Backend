using Microsoft.AspNetCore.Mvc;
using DeliveryService.API.Services;
using DeliveryService.API.DTOs;

namespace DeliveryService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,Dispatcher")]
public class DeliveriesController : ControllerBase
{
    private readonly IDeliveryManagerService _deliveryManagerService;

    public DeliveriesController(IDeliveryManagerService deliveryManagerService)
    {
        _deliveryManagerService = deliveryManagerService;
    }

    /// <summary>
    /// Retrieve a paginated list of deliveries with optional filters.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<DeliveryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<DeliveryDto>>> Get(
        [FromQuery] string? status,
        [FromQuery] string? destination,
        [FromQuery] DateTime? date,
        [FromQuery] string? orderNumber,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var list = await _deliveryManagerService.GetAllDeliveriesAsync(status, destination, date, orderNumber, page, pageSize);
        return Ok(list);
    }

    /// <summary>Create a new delivery request to initiate logistics operations.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(DeliveryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DeliveryDto>> Create([FromBody] CreateDeliveryRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PickupAddress))
            return BadRequest("PickupAddress is required.");
        if (request.PickupAddress.Length > 500)
            return BadRequest("PickupAddress must be 500 characters or fewer.");

        if (string.IsNullOrWhiteSpace(request.DeliveryAddress))
            return BadRequest("DeliveryAddress is required.");
        if (request.DeliveryAddress.Length > 500)
            return BadRequest("DeliveryAddress must be 500 characters or fewer.");

        if (request.PackageWeightKg <= 0)
            return BadRequest("PackageWeightKg must be greater than 0.");
        if (request.PackageVolumeM3 <= 0)
            return BadRequest("PackageVolumeM3 must be greater than 0.");

        if (request.Deadline <= DateTime.UtcNow)
            return BadRequest("Deadline must be in the future.");

        if (string.IsNullOrWhiteSpace(request.CreatedBy))
            return BadRequest("CreatedBy is required.");

        var delivery = await _deliveryManagerService.CreateDeliveryAsync(request, cancellationToken);
        return Created($"/api/deliveries/{delivery.Id}", delivery);
    }

    /// <summary>Get full details of a delivery by ID, including its status history.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(DeliveryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeliveryDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var delivery = await _deliveryManagerService.GetDeliveryByIdAsync(id, cancellationToken);
        if (delivery is null) return NotFound();
        return Ok(delivery);
    }

    /// <summary>Update delivery details (addresses, weights, deadline).</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(DeliveryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeliveryDto>> Update(int id, [FromBody] UpdateDeliveryRequestDto request, CancellationToken cancellationToken)
    {
        // Validate PickupAddress
        if (request.PickupAddress != null)
        {
            if (string.IsNullOrWhiteSpace(request.PickupAddress))
                return BadRequest("PickupAddress cannot be empty or whitespace.");
            if (request.PickupAddress.Length < 3)
                return BadRequest("PickupAddress must be at least 3 characters.");
            if (request.PickupAddress.Length > 500)
                return BadRequest("PickupAddress must be 500 characters or fewer.");
        }

        // Validate DeliveryAddress
        if (request.DeliveryAddress != null)
        {
            if (string.IsNullOrWhiteSpace(request.DeliveryAddress))
                return BadRequest("DeliveryAddress cannot be empty or whitespace.");
            if (request.DeliveryAddress.Length < 3)
                return BadRequest("DeliveryAddress must be at least 3 characters.");
            if (request.DeliveryAddress.Length > 500)
                return BadRequest("DeliveryAddress must be 500 characters or fewer.");
        }

        // Validate PackageWeightKg
        if (request.PackageWeightKg.HasValue)
        {
            if (request.PackageWeightKg <= 0)
                return BadRequest("PackageWeightKg must be greater than 0.");
            if (request.PackageWeightKg > 1000)
                return BadRequest("PackageWeightKg must not exceed 1000 kg.");
        }

        // Validate PackageVolumeM3
        if (request.PackageVolumeM3.HasValue)
        {
            if (request.PackageVolumeM3 <= 0)
                return BadRequest("PackageVolumeM3 must be greater than 0.");
            if (request.PackageVolumeM3 > 100)
                return BadRequest("PackageVolumeM3 must not exceed 100 m³.");
        }

        // Validate Deadline
        if (request.Deadline.HasValue)
        {
            if (request.Deadline <= DateTime.UtcNow)
                return BadRequest("Deadline must be in the future.");
            if (request.Deadline > DateTime.UtcNow.AddYears(1))
                return BadRequest("Deadline cannot be more than 1 year in the future.");
        }

        var delivery = await _deliveryManagerService.UpdateDeliveryAsync(id, request, cancellationToken);
        if (delivery is null) return NotFound();
        return Ok(delivery);
    }
}
