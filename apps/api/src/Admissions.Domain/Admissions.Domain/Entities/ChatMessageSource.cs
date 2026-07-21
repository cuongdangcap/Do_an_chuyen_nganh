namespace Admissions.Domain.Entities;

public sealed class ChatMessageSource
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MessageId { get; set; }
    public string PointId { get; set; } = string.Empty;
    public double Score { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? DocumentType { get; set; }
    public int? PageNumber { get; set; }
    public string? SectionTitle { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ChatMessage Message { get; set; } = null!;
}
