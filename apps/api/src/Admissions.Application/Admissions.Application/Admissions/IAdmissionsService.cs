namespace Admissions.Application.Admissions;

public interface IAdmissionsService
{
    Task<PagedResponse<MajorListItem>> ListMajorsAsync(MajorQuery query, CancellationToken cancellationToken);
    Task<MajorDetailDto?> GetMajorAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AdmissionCycleDto>> ListAdmissionCyclesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<FacultyDto>> ListFacultiesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<SubjectCombinationDto>> ListSubjectCombinationsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AdmissionMethodDto>> ListAdmissionMethodsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<FaqDto>> ListFaqsAsync(string? category, CancellationToken cancellationToken);
    Task<ProgramComparisonResponse> CompareProgramsAsync(CompareProgramsRequest request, CancellationToken cancellationToken);

    Task<AdmissionCycleDto> CreateAdmissionCycleAsync(CreateAdmissionCycleRequest request, CancellationToken cancellationToken);
    Task<FacultyDto> CreateFacultyAsync(CreateFacultyRequest request, CancellationToken cancellationToken);
    Task<SubjectCombinationDto> CreateSubjectCombinationAsync(CreateSubjectCombinationRequest request, CancellationToken cancellationToken);
    Task<AdmissionMethodDto> CreateAdmissionMethodAsync(CreateAdmissionMethodRequest request, CancellationToken cancellationToken);
    Task<MajorDetailDto> CreateMajorAsync(CreateMajorRequest request, CancellationToken cancellationToken);
    Task<MajorDetailDto> UpdateMajorAsync(Guid id, CreateMajorRequest request, CancellationToken cancellationToken);
    Task DeleteMajorAsync(Guid id, CancellationToken cancellationToken);
    Task<ProgramDetailDto> CreateProgramAsync(CreateProgramRequest request, CancellationToken cancellationToken);
    Task<CutoffScoreDto> CreateCutoffScoreAsync(CreateCutoffScoreRequest request, CancellationToken cancellationToken);
    Task<TuitionFeeDto> CreateTuitionFeeAsync(CreateTuitionFeeRequest request, CancellationToken cancellationToken);
    Task<FaqDto> CreateFaqAsync(CreateFaqRequest request, CancellationToken cancellationToken);
}
