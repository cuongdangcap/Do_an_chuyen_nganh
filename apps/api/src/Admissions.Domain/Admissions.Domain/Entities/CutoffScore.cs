namespace Admissions.Domain.Entities;

public sealed class CutoffScore
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProgramId { get; set; }
    public AcademicProgram Program { get; set; } = null!;

    public Guid AdmissionCycleId { get; set; }
    public AdmissionCycle AdmissionCycle { get; set; } = null!;

    public Guid AdmissionMethodId { get; set; }
    public AdmissionMethod AdmissionMethod { get; set; } = null!;

    public Guid? SubjectCombinationId { get; set; }
    public SubjectCombination? SubjectCombination { get; set; }

    public decimal Score { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
