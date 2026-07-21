namespace Admissions.Domain.Entities;

public sealed class AdmissionCycle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int Year { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string Status { get; set; } = "active";

    public ICollection<CutoffScore> CutoffScores { get; set; } = [];
}
