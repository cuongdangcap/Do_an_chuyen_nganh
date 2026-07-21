namespace Admissions.Application.Chat;

public sealed record ChatFileQuestionCommand(
    string Question,
    string? ClientSessionId,
    Guid? ConversationId,
    string FileName,
    string? ContentType,
    long FileSizeBytes,
    Stream Content,
    Guid? UserId);
