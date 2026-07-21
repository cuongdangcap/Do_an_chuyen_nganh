namespace Admissions.Domain.Entities;

public sealed class ParentProfile
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string? Occupation { get; set; }
    public string? Province { get; set; }
    public string? ContactPreference { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
