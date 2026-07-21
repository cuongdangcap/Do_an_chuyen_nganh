namespace Admissions.Domain.Entities;

public sealed class HandoffTicket
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ConversationId { get; set; }
    public Guid? SourceMessageId { get; set; }
    public Guid? FeedbackId { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public string Status { get; set; } = "open";
    public string Priority { get; set; } = "normal";
    public string Reason { get; set; } = "negative_feedback";
    public string Question { get; set; } = string.Empty;
    public string AiAnswer { get; set; } = string.Empty;
    public string? StaffReplyPreview { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }

    public ChatConversation? Conversation { get; set; }
    public ChatMessage? SourceMessage { get; set; }
    public ChatFeedback? Feedback { get; set; }
    public User? CreatedByUser { get; set; }
    public User? AssignedToUser { get; set; }
    public ICollection<HandoffMessage> Messages { get; set; } = [];
}
