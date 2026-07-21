namespace Admissions.Application.Profiles;

public sealed record ProfileResponse(
    object User,
    StudentProfileDto? StudentProfile,
    ParentProfileDto? ParentProfile,
    StaffProfileDto? StaffProfile);

public sealed class UpdateProfileRequest
{
    public string? FullName { get; init; }
    public string? Phone { get; init; }
    public StudentProfileDto? StudentProfile { get; init; }
    public ParentProfileDto? ParentProfile { get; init; }
}

public sealed class StudentProfileDto
{
    public string? HighSchool { get; init; }
    public string? Province { get; init; }
    public int? GraduationYear { get; init; }
    public decimal? ExpectedScore { get; init; }
    public decimal? ExamScore { get; init; }
    public string? InterestedSubjectGroup { get; init; }
    public string? Notes { get; init; }
}

public sealed class ParentProfileDto
{
    public string? Occupation { get; init; }
    public string? Province { get; init; }
    public string? ContactPreference { get; init; }
}

public sealed record StaffProfileDto(
    string? Department,
    string? Position,
    bool CanManageDocuments,
    bool CanReplyChat);
