namespace Admissions.Domain.Entities;

public sealed class StudentProfile
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string? HighSchool { get; set; }
    public string? Province { get; set; }
    public int? GraduationYear { get; set; }
    public decimal? ExpectedScore { get; set; }
    public decimal? ExamScore { get; set; }
    public string? InterestedSubjectGroup { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
