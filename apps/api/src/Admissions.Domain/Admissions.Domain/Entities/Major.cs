namespace Admissions.Domain.Entities;

public sealed class Major
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FacultyId { get; set; }
    public Faculty Faculty { get; set; } = null!;

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CareerOutcomes { get; set; }
    public string Status { get; set; } = "active";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<AcademicProgram> Programs { get; set; } = [];
}
