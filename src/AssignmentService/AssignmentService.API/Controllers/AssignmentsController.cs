using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AssignmentService.API.Models;
using AssignmentService.API.Repositories;
using Meridian.VehicleGrpc;
using Meridian.DriverGrpc;
using System.Security.Claims;

namespace AssignmentService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AssignmentsController : ControllerBase
{
    private readonly VehicleGrpc.VehicleGrpcClient _vehicleClient;
    private readonly DriverGrpc.DriverGrpcClient _driverClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAssignmentRepository _repository;

    public AssignmentsController(
        VehicleGrpc.VehicleGrpcClient vehicleClient,
        DriverGrpc.DriverGrpcClient driverClient,
        IHttpClientFactory httpClientFactory,
        IAssignmentRepository repository)
    {
        _vehicleClient = vehicleClient;
        _driverClient = driverClient;
        _httpClientFactory = httpClientFactory;
        _repository = repository;
    }

    // POST /api/assignments
    [HttpPost]
    [Authorize(Roles = "Admin,Dispatcher")]
    public async Task<IActionResult> CreateAssignment([FromBody] CreateAssignmentRequest request)
    {
        try
        {
            // MER-163: Validate vehicle availability via gRPC
            var vehicleAvailability = await _vehicleClient.CheckAvailabilityAsync(
                new VehicleRequest { VehicleId = request.VehicleId });
            if (!vehicleAvailability.IsAvailable)
                return Conflict(new { success = false, message = vehicleAvailability.Message });

            // MER-163: Validate driver availability via gRPC
            var driverAvailability = await _driverClient.CheckAvailabilityAsync(
                new DriverRequest { DriverId = request.DriverId });
            if (!driverAvailability.IsAvailable)
                return Conflict(new { success = false, message = driverAvailability.Message });

            var assignedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            var now = DateTime.UtcNow;

            var assignment = new Assignment
            {
                DeliveryId = request.DeliveryId,
                VehicleId = request.VehicleId,
                DriverId = request.DriverId,
                AssignedAt = now,
                AssignedBy = assignedBy,
                Status = "Active",
                Notes = request.Notes,
                CreatedAt = now,
                UpdatedAt = now
            };

            var created = await _repository.CreateAsync(assignment);

            // MER-163: Mark vehicle as OnTrip
            await _vehicleClient.UpdateStatusAsync(new UpdateStatusRequest
            {
                VehicleId = request.VehicleId,
                NewStatus = "OnTrip"
            });

            // MER-164: Update delivery status to Assigned
            await PatchDeliveryStatusAsync(request.DeliveryId, "Assigned");

            return StatusCode(201, new { success = true, data = created });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Failed to create assignment", errors = new[] { ex.Message } });
        }
    }

    // GET /api/assignments
    [HttpGet]
    [Authorize(Roles = "Admin,Dispatcher")]
    public async Task<IActionResult> GetAssignments([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var (assignments, totalCount) = await _repository.GetAllAsync(page, pageSize);
            return Ok(new { success = true, data = assignments, meta = new { page, pageSize, totalCount } });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Failed to fetch assignments", errors = new[] { ex.Message } });
        }
    }

    // GET /api/assignments/{id}
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Dispatcher")]
    public async Task<IActionResult> GetAssignment(int id)
    {
        try
        {
            var assignment = await _repository.GetByIdAsync(id);
            if (assignment == null)
                return NotFound(new { success = false, message = "Assignment not found", errors = Array.Empty<string>() });

            return Ok(new { success = true, data = assignment });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Failed to fetch assignment", errors = new[] { ex.Message } });
        }
    }

    // GET /api/assignments/delivery/{deliveryId}
    [HttpGet("delivery/{deliveryId}")]
    [Authorize(Roles = "Admin,Dispatcher")]
    public async Task<IActionResult> GetAssignmentByDelivery(int deliveryId)
    {
        try
        {
            var assignment = await _repository.GetByDeliveryIdAsync(deliveryId);
            if (assignment == null)
                return NotFound(new { success = false, message = "Assignment not found for delivery", errors = Array.Empty<string>() });

            return Ok(new { success = true, data = assignment });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Failed to fetch assignment", errors = new[] { ex.Message } });
        }
    }

    // GET /api/assignments/driver/{driverId}/active
    [HttpGet("driver/{driverId}/active")]
    [Authorize(Roles = "Admin,Dispatcher,Driver")]
    public async Task<IActionResult> GetActiveAssignmentByDriver(int driverId)
    {
        try
        {
            var assignment = await _repository.GetActiveByDriverIdAsync(driverId);
            if (assignment == null)
                return NotFound(new { success = false, message = "No active assignment for driver", errors = Array.Empty<string>() });

            return Ok(new { success = true, data = assignment });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Failed to fetch driver assignment", errors = new[] { ex.Message } });
        }
    }

    // PUT /api/assignments/{id}/complete
    [HttpPut("{id}/complete")]
    [Authorize(Roles = "Admin,Dispatcher")]
    public async Task<IActionResult> CompleteAssignment(int id)
    {
        try
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { success = false, message = "Assignment not found", errors = Array.Empty<string>() });

            var updated = await _repository.UpdateStatusAsync(id, "Completed");

            // MER-164: Update delivery status to Completed
            await PatchDeliveryStatusAsync(existing.DeliveryId, "Completed");

            // Mark vehicle back to Available
            await _vehicleClient.UpdateStatusAsync(new UpdateStatusRequest
            {
                VehicleId = existing.VehicleId,
                NewStatus = "Available"
            });

            return Ok(new { success = true, message = "Assignment completed successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Failed to complete assignment", errors = new[] { ex.Message } });
        }
    }

    // PUT /api/assignments/{id}/cancel
    [HttpPut("{id}/cancel")]
    [Authorize(Roles = "Admin,Dispatcher")]
    public async Task<IActionResult> CancelAssignment(int id)
    {
        try
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { success = false, message = "Assignment not found", errors = Array.Empty<string>() });

            await _repository.UpdateStatusAsync(id, "Cancelled");

            // MER-164: Revert delivery status to Pending
            await PatchDeliveryStatusAsync(existing.DeliveryId, "Pending");

            // Mark vehicle back to Available
            await _vehicleClient.UpdateStatusAsync(new UpdateStatusRequest
            {
                VehicleId = existing.VehicleId,
                NewStatus = "Available"
            });

            return Ok(new { success = true, message = "Assignment cancelled successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Failed to cancel assignment", errors = new[] { ex.Message } });
        }
    }

    // Helper: PATCH delivery status via HTTP
    private async Task PatchDeliveryStatusAsync(int deliveryId, string status)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("DeliveryService");
            var payload = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(new { status }),
                System.Text.Encoding.UTF8,
                "application/json");
            await client.PatchAsync($"/api/deliveries/{deliveryId}/status", payload);
        }
        catch
        {
            // Log and swallow — delivery status update is best-effort
        }
    }
}

public class CreateAssignmentRequest
{
    public int DeliveryId { get; set; }
    public int VehicleId { get; set; }
    public int DriverId { get; set; }
    public string? Notes { get; set; }
}
