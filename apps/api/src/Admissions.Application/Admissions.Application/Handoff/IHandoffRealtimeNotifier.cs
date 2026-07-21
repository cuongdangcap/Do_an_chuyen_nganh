namespace Admissions.Application.Handoff;

public interface IHandoffRealtimeNotifier
{
    Task TicketCreatedAsync(HandoffTicketDto ticket, CancellationToken cancellationToken);

    Task TicketUpdatedAsync(HandoffTicketDto ticket, CancellationToken cancellationToken);
}
