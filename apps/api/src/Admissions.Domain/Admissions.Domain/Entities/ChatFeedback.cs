namespace Admissions.Domain.Entities;

public sealed class ChatFeedback
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MessageId { get; set; }
    public Guid? UserId { get; set; }
    public string Rating { get; set; } = "positive";
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ChatMessage Message { get; set; } = null!;
    public User? User { get; set; }
}
