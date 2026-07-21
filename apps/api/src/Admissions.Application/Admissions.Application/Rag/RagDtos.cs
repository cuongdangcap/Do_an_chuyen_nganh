namespace Admissions.Application.Rag;

public sealed record RagSearchRequest(string Query, int TopK);

public sealed record RagSearchResult(
    string PointId,
    double Score,
    string Content,
    string? Title,
    string? DocumentType,
    int? PageNumber,
    string? SectionTitle);

public sealed record RagSearchResponse(
    string Backend,
    IReadOnlyCollection<RagSearchResult> Results);

public sealed record RagChatRequest(string Question, int TopK, Guid? ConversationId = null, string? ClientSessionId = null);

public sealed record RagChatResponse(
    string Answer,
    string Backend,
    IReadOnlyCollection<RagSearchResult> Sources,
    Guid? ConversationId = null,
    Guid? UserMessageId = null,
    Guid? AssistantMessageId = null,
    int? LatencyMs = null);

public sealed record CreateChatFeedbackRequest(
    string Rating,
    string? Note);

public sealed record ChatFeedbackDto(
    Guid Id,
    Guid MessageId,
    Guid? UserId,
    string? UserEmail,
    string Rating,
    string? Note,
    string Answer,
    string Question,
    DateTime CreatedAt,
    Guid? HandoffTicketId = null);

public sealed record ChatFeedbackListResponse(
    IReadOnlyCollection<ChatFeedbackDto> Items,
    int TotalItems);
