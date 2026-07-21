namespace Admissions.Domain.Entities;

public sealed class StaffProfile
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string? Department { get; set; }
    public string? Position { get; set; }
    public bool CanManageDocuments { get; set; }
    public bool CanReplyChat { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
