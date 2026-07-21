namespace Admissions.Application.Chat;

public interface IChatHistoryService
{
    Task<ChatConversationListResponse> ListAsync(Guid? userId, string? clientSessionId, int page, int pageSize, CancellationToken cancellationToken);
    Task<ChatConversationDetailDto?> GetAsync(Guid id, Guid? userId, string? clientSessionId, CancellationToken cancellationToken);
    Task<ChatConversationDetailDto> AskFileAsync(ChatFileQuestionCommand command, CancellationToken cancellationToken);
}
