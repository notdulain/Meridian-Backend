using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TrackingService.API.Models;
using TrackingService.API.Hubs;
using TrackingService.API.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace TrackingService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TrackingController : ControllerBase
{
    private readonly IHubContext<TrackingHub> _hubContext;
    private readonly ITrackingRepository _repository;
    private readonly ILogger<TrackingController> _logger;

    public TrackingController(
        IHubContext<TrackingHub> hubContext,
        ITrackingRepository repository,
        ILogger<TrackingController> logger)
    {
        _hubContext = hubContext;
        _repository = repository;
        _logger = logger;
    }

    [HttpPost("location")]
    [Authorize(Roles = "Driver")]
    public async Task<IActionResult> PostLocation([FromBody] LocationUpdate update)
    {
        // Require timestamps and override server time if they are horribly desync
        if (update.Timestamp == default) 
            update.Timestamp = DateTime.UtcNow;

        // Save to SQL Database (MER-251)
        var savedUpdate = await _repository.LogLocationAsync(update);

        // Broadcast to SignalR group immediately so Dispatchers see it live (MER-250)
        await _hubContext.Clients.Group($"tracking-{update.AssignmentId}").SendAsync("ReceiveLocationUpdate", savedUpdate);

        _logger.LogInformation(
            "Broadcasted driver coordinates via SignalR. AssignmentId: {AssignmentId}, DriverId: {DriverId}, Latitude: {Latitude}, Longitude: {Longitude}, Timestamp: {Timestamp}",
            savedUpdate.AssignmentId,
            savedUpdate.DriverId,
            savedUpdate.Latitude,
            savedUpdate.Longitude,
            savedUpdate.Timestamp);

        return Ok(new { success = true, data = savedUpdate });
    }

    [HttpGet("assignment/{assignmentId}/history")]
    [Authorize(Roles = "Admin,Dispatcher")]
    public async Task<IActionResult> GetAssignmentHistory(int assignmentId)
    {
        var history = await _repository.GetHistoryAsync(assignmentId);
        return Ok(new { success = true, data = history });
    }

    [HttpGet("driver/{driverId}/last-known")]
    [Authorize(Roles = "Admin,Dispatcher")]
    public async Task<IActionResult> GetDriverLastKnown(int driverId)
    {
        var last = await _repository.GetLastKnownLocationAsync(driverId);
        if (last == null) return NotFound(new { success = false, message = "No tracking data found for driver" });

        return Ok(new { success = true, data = last });
    }
}
