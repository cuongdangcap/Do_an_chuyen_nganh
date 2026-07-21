namespace Admissions.Application.Admissions;

public sealed record FacultyDto(Guid Id, string Code, string Name, string? Description, string Status);

public sealed record AdmissionCycleDto(
    Guid Id,
    int Year,
    string Name,
    DateOnly? ApplicationStartDate,
    DateOnly? ApplicationEndDate,
    string Status);

public sealed record MajorListItem(
    Guid Id,
    string Code,
    string Name,
    string FacultyName,
    IReadOnlyCollection<ProgramSummaryDto> Programs);

public sealed record MajorDetailDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string? CareerOutcomes,
    string Status,
    FacultyDto Faculty,
    IReadOnlyCollection<ProgramDetailDto> Programs);

public sealed record ProgramSummaryDto(
    Guid Id,
    string Code,
    string Name,
    string? Campus,
    decimal? LatestCutoffScore,
    string? TuitionRange);

public sealed record ProgramDetailDto(
    Guid Id,
    string Code,
    string Name,
    string? DegreeType,
    string? Language,
    string? Campus,
    decimal? DurationYears,
    string? Description,
    string Status,
    IReadOnlyCollection<SubjectCombinationDto> SubjectCombinations,
    IReadOnlyCollection<CutoffScoreDto> CutoffScores,
    IReadOnlyCollection<TuitionFeeDto> TuitionFees);

public sealed record SubjectCombinationDto(Guid Id, string Code, string Subjects, string? Description);

public sealed record AdmissionMethodDto(Guid Id, string Code, string Name, string? Description, string Status);

public sealed record CutoffScoreDto(
    Guid Id,
    int Year,
    string MethodCode,
    string MethodName,
    string? SubjectCombinationCode,
    decimal Score,
    string? Note);

public sealed record TuitionFeeDto(
    Guid Id,
    string AcademicYear,
    decimal? AmountMin,
    decimal? AmountMax,
    string Currency,
    string Unit,
    string? Note);

public sealed record FaqDto(Guid Id, string? Category, string Question, string Answer, string Status);

public sealed record PagedResponse<T>(
    IReadOnlyCollection<T> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);

public sealed record MajorQuery(
    string? Keyword,
    Guid? FacultyId,
    string? SubjectCombinationCode,
    decimal? MinScore,
    decimal? MaxScore,
    decimal? MaxTuition,
    string? Campus,
    int Page,
    int PageSize);

public sealed record CreateFacultyRequest(string Code, string Name, string? Description, string Status);

public sealed record CreateAdmissionCycleRequest(
    int Year,
    string Name,
    DateOnly? ApplicationStartDate,
    DateOnly? ApplicationEndDate,
    string Status);

public sealed record CreateSubjectCombinationRequest(string Code, string Subjects, string? Description);

public sealed record CreateAdmissionMethodRequest(string Code, string Name, string? Description, string Status);

public sealed record CreateMajorRequest(
    Guid FacultyId,
    string Code,
    string Name,
    string? Description,
    string? CareerOutcomes,
    string Status);

public sealed record CreateProgramRequest(
    Guid MajorId,
    string Code,
    string Name,
    string? DegreeType,
    string? Language,
    string? Campus,
    decimal? DurationYears,
    string? Description,
    string Status,
    IReadOnlyCollection<Guid> SubjectCombinationIds);

public sealed record CreateCutoffScoreRequest(
    Guid ProgramId,
    Guid AdmissionCycleId,
    Guid AdmissionMethodId,
    Guid? SubjectCombinationId,
    decimal Score,
    string? Note);

public sealed record CreateTuitionFeeRequest(
    Guid ProgramId,
    string AcademicYear,
    decimal? AmountMin,
    decimal? AmountMax,
    string Currency,
    string Unit,
    string? Note);

public sealed record CreateFaqRequest(string? Category, string Question, string Answer, string Status);

public sealed record CompareProgramsRequest(IReadOnlyCollection<Guid> ProgramIds);

public sealed record ProgramComparisonResponse(IReadOnlyCollection<ProgramDetailDto> Items, string Summary);
