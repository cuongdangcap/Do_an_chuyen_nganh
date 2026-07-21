using Admissions.Api.Common;
using Admissions.Application.Admissions;
using Microsoft.AspNetCore.Mvc;

namespace Admissions.Api.Controllers;

[ApiController]
[Route("api/admissions")]
public sealed class AdmissionsController(IAdmissionsService admissionsService) : ControllerBase
{
    [HttpGet("cycles")]
    public async Task<IActionResult> ListCycles(CancellationToken cancellationToken)
    {
        var cycles = await admissionsService.ListAdmissionCyclesAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<AdmissionCycleDto>>.Ok(cycles, "OK", HttpContext.TraceIdentifier));
    }

    [HttpGet("faculties")]
    public async Task<IActionResult> ListFaculties(CancellationToken cancellationToken)
    {
        var faculties = await admissionsService.ListFacultiesAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<FacultyDto>>.Ok(faculties, "OK", HttpContext.TraceIdentifier));
    }

    [HttpGet("majors")]
    public async Task<IActionResult> ListMajors(
        [FromQuery] string? keyword = null,
        [FromQuery] Guid? facultyId = null,
        [FromQuery] string? subjectCombinationCode = null,
        [FromQuery] decimal? minScore = null,
        [FromQuery] decimal? maxScore = null,
        [FromQuery] decimal? maxTuition = null,
        [FromQuery] string? campus = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new MajorQuery(
            keyword,
            facultyId,
            subjectCombinationCode,
            minScore,
            maxScore,
            maxTuition,
            campus,
            page,
            pageSize);
        var majors = await admissionsService.ListMajorsAsync(query, cancellationToken);
        return Ok(ApiResponse<PagedResponse<MajorListItem>>.Ok(majors, "OK", HttpContext.TraceIdentifier));
    }

    [HttpGet("majors/{id:guid}")]
    public async Task<IActionResult> GetMajor(Guid id, CancellationToken cancellationToken)
    {
        var major = await admissionsService.GetMajorAsync(id, cancellationToken);
        if (major is null)
        {
            return NotFound(ApiResponse<object>.Fail("MAJOR_NOT_FOUND", "Major not found.", HttpContext.TraceIdentifier));
        }

        return Ok(ApiResponse<MajorDetailDto>.Ok(major, "OK", HttpContext.TraceIdentifier));
    }

    [HttpGet("subject-combinations")]
    public async Task<IActionResult> ListSubjectCombinations(CancellationToken cancellationToken)
    {
        var subjectCombinations = await admissionsService.ListSubjectCombinationsAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<SubjectCombinationDto>>.Ok(subjectCombinations, "OK", HttpContext.TraceIdentifier));
    }

    [HttpGet("methods")]
    public async Task<IActionResult> ListAdmissionMethods(CancellationToken cancellationToken)
    {
        var methods = await admissionsService.ListAdmissionMethodsAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<AdmissionMethodDto>>.Ok(methods, "OK", HttpContext.TraceIdentifier));
    }

    [HttpGet("faqs")]
    public async Task<IActionResult> ListFaqs([FromQuery] string? category, CancellationToken cancellationToken)
    {
        var faqs = await admissionsService.ListFaqsAsync(category, cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<FaqDto>>.Ok(faqs, "OK", HttpContext.TraceIdentifier));
    }

    [HttpPost("compare-programs")]
    public async Task<IActionResult> ComparePrograms(CompareProgramsRequest request, CancellationToken cancellationToken)
    {
        var comparison = await admissionsService.CompareProgramsAsync(request, cancellationToken);
        return Ok(ApiResponse<ProgramComparisonResponse>.Ok(comparison, "OK", HttpContext.TraceIdentifier));
    }
}
