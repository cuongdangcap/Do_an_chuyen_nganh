namespace Admissions.Application.Rag;

/// <summary>
/// Carries the active conversation identity through one RAG request so the
/// answer generator can reuse recent messages without changing the public RAG DTOs.
/// The service is registered as scoped and therefore never leaks between requests.
/// </summary>
public sealed class ConversationMemoryContext
{
    public Guid? ConversationId { get; set; }
    public Guid? UserId { get; set; }
    public string? ClientSessionId { get; set; }
}