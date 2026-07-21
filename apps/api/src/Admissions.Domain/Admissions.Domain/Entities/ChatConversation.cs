namespace Admissions.Domain.Entities;

public sealed class ChatConversation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public string? ClientSessionId { get; set; }
    public string Title { get; set; } = "New conversation";
    public string Status { get; set; } = "active";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
    public ICollection<ChatMessage> Messages { get; set; } = [];
}
