namespace Admissions.Domain.Entities;

public sealed class AdmissionMethod
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "active";

    public ICollection<CutoffScore> CutoffScores { get; set; } = [];
}
