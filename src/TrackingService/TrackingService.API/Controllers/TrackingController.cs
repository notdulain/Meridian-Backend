using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TrackingService.API.Models;
using TrackingService.API.Hubs;

namespace TrackingService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TrackingController : ControllerBase
{
    private readonly IHubContext<TrackingHub> _hubContext;

    public TrackingController(IHubContext<TrackingHub> hubContext)
    {
        _hubContext = hubContext;
    }

    [HttpPost("location")]
    public async Task<IActionResult> PostLocation([FromBody] LocationUpdate update)
    {
        // Placeholder implementation
        update.LocationUpdateId = 1;
        update.Timestamp = DateTime.UtcNow;

        // Broadcast to SignalR group
        await _hubContext.Clients.Group($"tracking-{update.AssignmentId}").SendAsync("ReceiveLocationUpdate", update);

        return Ok(new { success = true, data = update });
    }

    [HttpGet("assignment/{assignmentId}/history")]
    public IActionResult GetAssignmentHistory(int assignmentId)
    {
        // Placeholder implementation
        var history = new List<LocationUpdate>();
        return Ok(new { success = true, data = history });
    }

    [HttpGet("driver/{driverId}/last-known")]
    public IActionResult GetDriverLastKnown(int driverId)
    {
        // Placeholder implementation
        return Ok(new { success = true, data = new LocationUpdate { DriverId = driverId, Latitude = 0m, Longitude = 0m } });
    }
}
