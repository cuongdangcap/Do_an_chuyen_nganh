namespace Admissions.Application.Rag;

public interface IRagService
{
    Task<RagSearchResponse> SearchAsync(RagSearchRequest request, CancellationToken cancellationToken);
    Task<RagChatResponse> ChatAsync(RagChatRequest request, Guid? userId, CancellationToken cancellationToken);
    Task<ChatFeedbackDto> CreateFeedbackAsync(Guid assistantMessageId, CreateChatFeedbackRequest request, Guid? userId, CancellationToken cancellationToken);
    Task<ChatFeedbackListResponse> ListFeedbackAsync(string? rating, int page, int pageSize, CancellationToken cancellationToken);
}
