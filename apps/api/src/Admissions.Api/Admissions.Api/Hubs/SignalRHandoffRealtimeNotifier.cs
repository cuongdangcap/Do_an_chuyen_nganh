using Admissions.Application.Handoff;
using Microsoft.AspNetCore.SignalR;

namespace Admissions.Api.Hubs;

public sealed class SignalRHandoffRealtimeNotifier(IHubContext<HandoffHub> hubContext) : IHandoffRealtimeNotifier
{
    public Task TicketCreatedAsync(HandoffTicketDto ticket, CancellationToken cancellationToken)
    {
        return Task.WhenAll(
            hubContext.Clients.Group("handoff:staff").SendAsync("handoffTicketCreated", ticket, cancellationToken),
            hubContext.Clients.Group($"handoff:{ticket.Id}").SendAsync("handoffTicketUpdated", ticket, cancellationToken));
    }

    public Task TicketUpdatedAsync(HandoffTicketDto ticket, CancellationToken cancellationToken)
    {
        return Task.WhenAll(
            hubContext.Clients.Group("handoff:staff").SendAsync("handoffTicketUpdated", ticket, cancellationToken),
            hubContext.Clients.Group($"handoff:{ticket.Id}").SendAsync("handoffTicketUpdated", ticket, cancellationToken));
    }
}
