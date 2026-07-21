namespace Admissions.Domain.Entities;

public sealed class AcademicProgram
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MajorId { get; set; }
    public Major Major { get; set; } = null!;

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? DegreeType { get; set; }
    public string? Language { get; set; }
    public string? Campus { get; set; }
    public decimal? DurationYears { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "active";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ProgramSubjectCombination> SubjectCombinations { get; set; } = [];
    public ICollection<CutoffScore> CutoffScores { get; set; } = [];
    public ICollection<TuitionFee> TuitionFees { get; set; } = [];
}
