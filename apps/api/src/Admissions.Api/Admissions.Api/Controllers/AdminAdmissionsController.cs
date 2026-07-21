using Admissions.Api.Common;
using Admissions.Application.Admissions;
using Admissions.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Admissions.Api.Controllers;

[ApiController]
[Route("api/admin/admissions")]
[Authorize(Roles = RoleCodes.Admin + "," + RoleCodes.Staff)]
public sealed class AdminAdmissionsController(IAdmissionsService admissionsService) : ControllerBase
{
    [HttpPost("cycles")]
    public async Task<IActionResult> CreateCycle(CreateAdmissionCycleRequest request, CancellationToken cancellationToken)
    {
        var cycle = await admissionsService.CreateAdmissionCycleAsync(request, cancellationToken);
        return Ok(ApiResponse<AdmissionCycleDto>.Ok(cycle, "Admission cycle created.", HttpContext.TraceIdentifier));
    }

    [HttpPost("faculties")]
    public async Task<IActionResult> CreateFaculty(CreateFacultyRequest request, CancellationToken cancellationToken)
    {
        var faculty = await admissionsService.CreateFacultyAsync(request, cancellationToken);
        return Ok(ApiResponse<FacultyDto>.Ok(faculty, "Faculty created.", HttpContext.TraceIdentifier));
    }

    [HttpPost("subject-combinations")]
    public async Task<IActionResult> CreateSubjectCombination(CreateSubjectCombinationRequest request, CancellationToken cancellationToken)
    {
        var subjectCombination = await admissionsService.CreateSubjectCombinationAsync(request, cancellationToken);
        return Ok(ApiResponse<SubjectCombinationDto>.Ok(subjectCombination, "Subject combination created.", HttpContext.TraceIdentifier));
    }

    [HttpPost("methods")]
    public async Task<IActionResult> CreateAdmissionMethod(CreateAdmissionMethodRequest request, CancellationToken cancellationToken)
    {
        var method = await admissionsService.CreateAdmissionMethodAsync(request, cancellationToken);
        return Ok(ApiResponse<AdmissionMethodDto>.Ok(method, "Admission method created.", HttpContext.TraceIdentifier));
    }

    [HttpPost("majors")]
    public async Task<IActionResult> CreateMajor(CreateMajorRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var major = await admissionsService.CreateMajorAsync(request, cancellationToken);
            return Ok(ApiResponse<MajorDetailDto>.Ok(major, "Major created.", HttpContext.TraceIdentifier));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail("REFERENCE_NOT_FOUND", ex.Message, HttpContext.TraceIdentifier));
        }
    }

    [HttpPut("majors/{id:guid}")]
    public async Task<IActionResult> UpdateMajor(Guid id, CreateMajorRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var major = await admissionsService.UpdateMajorAsync(id, request, cancellationToken);
            return Ok(ApiResponse<MajorDetailDto>.Ok(major, "Major updated.", HttpContext.TraceIdentifier));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail("REFERENCE_NOT_FOUND", ex.Message, HttpContext.TraceIdentifier));
        }
    }

    [HttpDelete("majors/{id:guid}")]
    public async Task<IActionResult> DeleteMajor(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await admissionsService.DeleteMajorAsync(id, cancellationToken);
            return Ok(ApiResponse<object>.Ok(new { id }, "Major archived.", HttpContext.TraceIdentifier));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<object>.Fail("MAJOR_NOT_FOUND", "Major not found.", HttpContext.TraceIdentifier));
        }
    }

    [HttpPost("programs")]
    public async Task<IActionResult> CreateProgram(CreateProgramRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var program = await admissionsService.CreateProgramAsync(request, cancellationToken);
            return Ok(ApiResponse<ProgramDetailDto>.Ok(program, "Program created.", HttpContext.TraceIdentifier));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail("REFERENCE_NOT_FOUND", ex.Message, HttpContext.TraceIdentifier));
        }
    }

    [HttpPost("cutoff-scores")]
    public async Task<IActionResult> CreateCutoffScore(CreateCutoffScoreRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var cutoffScore = await admissionsService.CreateCutoffScoreAsync(request, cancellationToken);
            return Ok(ApiResponse<CutoffScoreDto>.Ok(cutoffScore, "Cutoff score created.", HttpContext.TraceIdentifier));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail("REFERENCE_NOT_FOUND", ex.Message, HttpContext.TraceIdentifier));
        }
    }

    [HttpPost("tuition-fees")]
    public async Task<IActionResult> CreateTuitionFee(CreateTuitionFeeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var tuitionFee = await admissionsService.CreateTuitionFeeAsync(request, cancellationToken);
            return Ok(ApiResponse<TuitionFeeDto>.Ok(tuitionFee, "Tuition fee created.", HttpContext.TraceIdentifier));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail("REFERENCE_NOT_FOUND", ex.Message, HttpContext.TraceIdentifier));
        }
    }

    [HttpPost("faqs")]
    public async Task<IActionResult> CreateFaq(CreateFaqRequest request, CancellationToken cancellationToken)
    {
        var faq = await admissionsService.CreateFaqAsync(request, cancellationToken);
        return Ok(ApiResponse<FaqDto>.Ok(faq, "FAQ created.", HttpContext.TraceIdentifier));
    }
}
