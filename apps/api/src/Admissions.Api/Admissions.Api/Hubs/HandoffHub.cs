using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Admissions.Api.Hubs;

[Authorize]
public sealed class HandoffHub : Hub
{
    public Task JoinStaffQueue()
    {
        if (!IsStaff())
        {
            throw new HubException("Only staff or admin can join the handoff staff queue.");
        }

        return Groups.AddToGroupAsync(Context.ConnectionId, "handoff:staff");
    }

    public Task LeaveStaffQueue()
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, "handoff:staff");
    }

    public Task JoinTicket(string ticketId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, $"handoff:{ticketId}");
    }

    public Task LeaveTicket(string ticketId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, $"handoff:{ticketId}");
    }

    private bool IsStaff()
    {
        return Context.User?.IsInRole("admin") == true || Context.User?.IsInRole("staff") == true;
    }
}
