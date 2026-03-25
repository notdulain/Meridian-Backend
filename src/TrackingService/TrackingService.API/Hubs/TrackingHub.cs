using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using TrackingService.API.Models;

namespace TrackingService.API.Hubs;

/// <summary>
/// SignalR hub for real-time driver location streaming.
///
/// Group naming: "tracking-{assignmentId}"
///
/// Flow:
///   - Dispatchers/Admins call JoinAssignmentGroup  → subscribe to live updates
///   - Drivers call SendLocationUpdate              → broadcasts to group subscribers
///   - Dispatchers/Admins call LeaveAssignmentGroup → unsubscribe
/// </summary>
[Authorize]
public class TrackingHub : Hub
{
    private readonly ILogger<TrackingHub> _logger;

    public TrackingHub(ILogger<TrackingHub> logger)
    {
        _logger = logger;
    }

    // ── Sub-task 239 + 240 ─────────────────────────────────────────────────────
    // Dispatchers and Admins join a group scoped to an assignmentId so they
    // receive all location updates broadcast for that assignment.

    /// <summary>Subscribe to live location updates for an assignment.</summary>
    [Authorize(Roles = "Admin,Dispatcher")]
    public async Task JoinAssignmentGroup(int assignmentId)
    {
        var groupName = $"tracking-{assignmentId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        _logger.LogInformation(
            "User {UserId} joined tracking group '{Group}'",
            Context.UserIdentifier, groupName);
    }

    /// <summary>Unsubscribe from live location updates for an assignment.</summary>
    [Authorize(Roles = "Admin,Dispatcher")]
    public async Task LeaveAssignmentGroup(int assignmentId)
    {
        var groupName = $"tracking-{assignmentId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        _logger.LogInformation(
            "User {UserId} left tracking group '{Group}'",
            Context.UserIdentifier, groupName);
    }

    // ── Sub-task 241 ───────────────────────────────────────────────────────────
    // Drivers call this hub method to push their current location.
    // The hub immediately broadcasts it to every Dispatcher/Admin subscribed
    // to the same assignment group.

    /// <summary>
    /// Driver pushes a location update; hub broadcasts it to the assignment group.
    /// </summary>
    [Authorize(Roles = "Driver")]
    public async Task SendLocationUpdate(LocationUpdate update)
    {
        update.Timestamp = DateTime.UtcNow;

        var groupName = $"tracking-{update.AssignmentId}";

        _logger.LogInformation(
            "Driver {UserId} sent location update for assignment {AssignmentId} " +
            "→ Lat: {Lat}, Lng: {Lng}",
            Context.UserIdentifier, update.AssignmentId, update.Latitude, update.Longitude);

        // Broadcast to all subscribers of this assignment group
        await Clients.Group(groupName).SendAsync("ReceiveLocationUpdate", update);
    }

    // ── Sub-task 242 ───────────────────────────────────────────────────────────
    // JWT is extracted from the query string (?access_token=...) in Program.cs.
    // Here we reject the connection early if the token produced no valid identity,
    // providing a clear server-side audit log.

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value ?? "unknown";

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning(
                "Rejected unauthenticated SignalR connection {ConnectionId}",
                Context.ConnectionId);

            Context.Abort();
            return;
        }

        _logger.LogInformation(
            "SignalR connection opened — ConnectionId: {ConnectionId}, User: {UserId}, Role: {Role}",
            Context.ConnectionId, userId, role);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation(
            "SignalR connection closed — ConnectionId: {ConnectionId}, User: {UserId}, Reason: {Reason}",
            Context.ConnectionId,
            Context.UserIdentifier,
            exception?.Message ?? "clean disconnect");

        await base.OnDisconnectedAsync(exception);
    }
}

