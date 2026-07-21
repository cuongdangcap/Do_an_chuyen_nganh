namespace Admissions.Application.Evaluation;

public interface IEvaluationService
{
    Task<IReadOnlyCollection<EvaluationQuestionDto>> ListQuestionsAsync(bool activeOnly, string? category, CancellationToken cancellationToken);
    Task<EvaluationQuestionDto> CreateQuestionAsync(CreateEvaluationQuestionRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<EvaluationQuestionDto>> SeedDefaultQuestionsAsync(CancellationToken cancellationToken);
    Task<EvaluationRunDto> RunAsync(RunEvaluationRequest request, CancellationToken cancellationToken);
    Task<EvaluationRunListResponse> ListRunsAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<EvaluationRunDto?> GetRunAsync(Guid id, CancellationToken cancellationToken);
}
