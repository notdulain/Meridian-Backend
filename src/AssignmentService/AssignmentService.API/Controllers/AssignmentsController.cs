using Microsoft.AspNetCore.Mvc;
using AssignmentService.API.Models;
using Meridian.VehicleGrpc;
using Meridian.DriverGrpc;
using System.Security.Claims;

namespace AssignmentService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssignmentsController : ControllerBase
{
    private readonly VehicleGrpc.VehicleGrpcClient _vehicleClient;
    private readonly DriverGrpc.DriverGrpcClient _driverClient;
    private readonly IHttpClientFactory _httpClientFactory;

    public AssignmentsController(
        VehicleGrpc.VehicleGrpcClient vehicleClient,
        DriverGrpc.DriverGrpcClient driverClient,
        IHttpClientFactory httpClientFactory)
    {
        _vehicleClient = vehicleClient;
        _driverClient = driverClient;
        _httpClientFactory = httpClientFactory;
    }

    [HttpPost]
    public IActionResult CreateAssignment([FromBody] CreateAssignmentRequest request)
    {
        // 1. Placeholder logic: In a full app, call _vehicleClient.CheckAvailability,
        // _driverClient.CheckAvailability, check delivery with _httpClientFactory,
        // Validate payload, then dispatch and return.

        var assignment = new Assignment
        {
            AssignmentId = 1,
            DeliveryId = request.DeliveryId,
            VehicleId = request.VehicleId,
            DriverId = request.DriverId,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "dispatcher-id",
            Status = "Active",
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return StatusCode(201, new { success = true, data = assignment });
    }

    [HttpGet]
    public IActionResult GetAssignments([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        // Placeholder implementation
        var assignments = new List<Assignment>();
        return Ok(new { success = true, data = assignments, meta = new { page, pageSize, totalCount = 0 } });
    }

    [HttpGet("{id}")]
    public IActionResult GetAssignment(int id)
    {
        // Placeholder implementation
        return Ok(new { success = true, data = new Assignment { AssignmentId = id, DeliveryId = 1, VehicleId = 1, DriverId = 1, AssignedBy = "admin", Status = "Active" } });
    }

    [HttpGet("delivery/{deliveryId}")]
    public IActionResult GetAssignmentByDelivery(int deliveryId)
    {
        // Placeholder implementation
        return Ok(new { success = true, data = new Assignment { AssignmentId = 1, DeliveryId = deliveryId, VehicleId = 1, DriverId = 1, AssignedBy = "admin", Status = "Active" } });
    }

    [HttpPut("{id}/complete")]
    public IActionResult CompleteAssignment(int id)
    {
        // Placeholder implementation
        return Ok(new { success = true, message = "Assignment completed successfully" });
    }

    [HttpPut("{id}/cancel")]
    public IActionResult CancelAssignment(int id)
    {
        // Placeholder implementation
        return Ok(new { success = true, message = "Assignment cancelled successfully" });
    }

    [HttpGet("driver/{driverId}/active")]
    public IActionResult GetActiveAssignmentByDriver(int driverId)
    {
        // Placeholder implementation
        return Ok(new { success = true, data = new Assignment { AssignmentId = 1, DeliveryId = 1, VehicleId = 1, DriverId = driverId, AssignedBy = "admin", Status = "Active" } });
    }
}

public class CreateAssignmentRequest
{
    public int DeliveryId { get; set; }
    public int VehicleId { get; set; }
    public int DriverId { get; set; }
    public string? Notes { get; set; }
}
