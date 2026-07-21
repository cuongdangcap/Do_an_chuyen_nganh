namespace Admissions.Domain.Entities;

public sealed class ChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConversationId { get; set; }
    public Guid? UserId { get; set; }
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
    public string? RetrievalBackend { get; set; }
    public int? LatencyMs { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ChatConversation Conversation { get; set; } = null!;
    public User? User { get; set; }
    public ICollection<ChatMessageSource> Sources { get; set; } = [];
    public ICollection<ChatFeedback> Feedback { get; set; } = [];
}
