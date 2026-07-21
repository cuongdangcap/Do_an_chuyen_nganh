namespace Admissions.Application.Handoff;

public sealed record HandoffMessageDto(
    Guid Id,
    Guid TicketId,
    Guid? SenderUserId,
    string? SenderEmail,
    string SenderRole,
    string Content,
    DateTime CreatedAt);

public sealed record HandoffTicketDto(
    Guid Id,
    Guid? ConversationId,
    Guid? SourceMessageId,
    Guid? FeedbackId,
    Guid? CreatedByUserId,
    string? CreatedByEmail,
    Guid? AssignedToUserId,
    string? AssignedToEmail,
    string Status,
    string Priority,
    string Reason,
    string Question,
    string AiAnswer,
    string? StaffReplyPreview,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? ResolvedAt,
    IReadOnlyCollection<HandoffMessageDto> Messages);

public sealed record HandoffTicketListResponse(
    IReadOnlyCollection<HandoffTicketDto> Items,
    int TotalItems);

public sealed record CreateHandoffTicketRequest(
    Guid AssistantMessageId,
    string? Reason,
    string? Note);

public sealed record ReplyHandoffTicketRequest(
    string Content,
    bool Resolve = false);

public sealed record UpdateHandoffTicketStatusRequest(
    string Status);
