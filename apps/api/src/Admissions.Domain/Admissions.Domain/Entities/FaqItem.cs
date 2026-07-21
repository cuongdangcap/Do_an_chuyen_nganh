namespace Admissions.Domain.Entities;

public sealed class FaqItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? Category { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
