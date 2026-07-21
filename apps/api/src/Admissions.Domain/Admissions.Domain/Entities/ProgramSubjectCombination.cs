namespace Admissions.Domain.Entities;

public sealed class ProgramSubjectCombination
{
    public Guid ProgramId { get; set; }
    public AcademicProgram Program { get; set; } = null!;

    public Guid SubjectCombinationId { get; set; }
    public SubjectCombination SubjectCombination { get; set; } = null!;
}
