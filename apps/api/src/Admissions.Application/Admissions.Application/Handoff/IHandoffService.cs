namespace Admissions.Application.Handoff;

public interface IHandoffService
{
    Task<HandoffTicketDto> CreateFromAssistantMessageAsync(CreateHandoffTicketRequest request, Guid? userId, CancellationToken cancellationToken);
    Task<HandoffTicketDto> CreateFromNegativeFeedbackAsync(Guid feedbackId, CancellationToken cancellationToken);
    Task<HandoffTicketListResponse> ListAsync(string? status, int page, int pageSize, CancellationToken cancellationToken);
    Task<HandoffTicketDto?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<HandoffTicketDto> ReplyAsync(Guid ticketId, ReplyHandoffTicketRequest request, Guid staffUserId, CancellationToken cancellationToken);
    Task<HandoffTicketDto> UpdateStatusAsync(Guid ticketId, UpdateHandoffTicketStatusRequest request, Guid staffUserId, CancellationToken cancellationToken);
}
