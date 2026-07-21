namespace Admissions.Application.Chat;

public sealed record ChatMessageSourceDto(
    Guid Id,
    string PointId,
    double Score,
    string Content,
    string? Title,
    string? DocumentType,
    int? PageNumber,
    string? SectionTitle);

public sealed record ChatMessageDto(
    Guid Id,
    string Role,
    string Content,
    string? RetrievalBackend,
    int? LatencyMs,
    DateTime CreatedAt,
    IReadOnlyCollection<ChatMessageSourceDto> Sources);

public sealed record ChatConversationSummaryDto(
    Guid Id,
    string Title,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string? LastMessagePreview);

public sealed record ChatConversationDetailDto(
    Guid Id,
    string Title,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyCollection<ChatMessageDto> Messages);

public sealed record ChatConversationListResponse(
    IReadOnlyCollection<ChatConversationSummaryDto> Items,
    int TotalItems);
