using Admissions.Api.Common;
using Admissions.Application.Evaluation;
using Admissions.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Admissions.Api.Controllers;

[ApiController]
[Route("api/admin/evaluation")]
[Authorize(Roles = RoleCodes.Admin + "," + RoleCodes.Staff)]
public sealed class AdminEvaluationController(IEvaluationService evaluationService) : ControllerBase
{
    [HttpGet("questions")]
    public async Task<IActionResult> ListQuestions(
        [FromQuery] bool activeOnly = true,
        [FromQuery] string? category = null,
        CancellationToken cancellationToken = default)
    {
        var questions = await evaluationService.ListQuestionsAsync(activeOnly, category, cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<EvaluationQuestionDto>>.Ok(questions, "OK", HttpContext.TraceIdentifier));
    }

    [HttpPost("questions")]
    public async Task<IActionResult> CreateQuestion(CreateEvaluationQuestionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var question = await evaluationService.CreateQuestionAsync(request, cancellationToken);
            return Ok(ApiResponse<EvaluationQuestionDto>.Ok(question, "Golden question created.", HttpContext.TraceIdentifier));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", ex.Message, HttpContext.TraceIdentifier));
        }
    }

    [HttpPost("questions/seed-defaults")]
    public async Task<IActionResult> SeedDefaults(CancellationToken cancellationToken)
    {
        var questions = await evaluationService.SeedDefaultQuestionsAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<EvaluationQuestionDto>>.Ok(questions, "Default golden questions seeded.", HttpContext.TraceIdentifier));
    }

    [HttpGet("runs")]
    public async Task<IActionResult> ListRuns(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var runs = await evaluationService.ListRunsAsync(page, pageSize, cancellationToken);
        return Ok(ApiResponse<EvaluationRunListResponse>.Ok(runs, "OK", HttpContext.TraceIdentifier));
    }

    [HttpGet("runs/{id:guid}")]
    public async Task<IActionResult> GetRun(Guid id, CancellationToken cancellationToken)
    {
        var run = await evaluationService.GetRunAsync(id, cancellationToken);
        if (run is null)
        {
            return NotFound(ApiResponse<object>.Fail("EVALUATION_RUN_NOT_FOUND", "Evaluation run not found.", HttpContext.TraceIdentifier));
        }

        return Ok(ApiResponse<EvaluationRunDto>.Ok(run, "OK", HttpContext.TraceIdentifier));
    }

    [HttpPost("runs")]
    public async Task<IActionResult> Run(RunEvaluationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var run = await evaluationService.RunAsync(request, cancellationToken);
            return Ok(ApiResponse<EvaluationRunDto>.Ok(run, "Evaluation completed.", HttpContext.TraceIdentifier));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail("EVALUATION_ERROR", ex.Message, HttpContext.TraceIdentifier));
        }
    }
}
