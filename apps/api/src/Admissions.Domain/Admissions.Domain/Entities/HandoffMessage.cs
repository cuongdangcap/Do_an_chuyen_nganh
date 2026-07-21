namespace Admissions.Domain.Entities;

public sealed class HandoffMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TicketId { get; set; }
    public Guid? SenderUserId { get; set; }
    public string SenderRole { get; set; } = "staff";
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public HandoffTicket Ticket { get; set; } = null!;
    public User? SenderUser { get; set; }
}
