namespace Admissions.Domain.Entities;

public sealed class SubjectCombination
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Subjects { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<ProgramSubjectCombination> Programs { get; set; } = [];
}
