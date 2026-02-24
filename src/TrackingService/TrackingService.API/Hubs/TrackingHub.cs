using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace TrackingService.API.Hubs;

[Authorize]
public class TrackingHub : Hub
{
    public async Task JoinAssignmentGroup(int assignmentId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"tracking-{assignmentId}");
    }

    public async Task LeaveAssignmentGroup(int assignmentId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tracking-{assignmentId}");
    }
}
