namespace Admissions.Application.Evaluation;

public sealed record EvaluationQuestionDto(
    Guid Id,
    string Code,
    string Question,
    string ExpectedAnswer,
    IReadOnlyCollection<string> ExpectedKeywords,
    string? ExpectedSourceTitle,
    string? ExpectedDocumentType,
    string Category,
    bool IsActive,
    DateTime CreatedAt);

public sealed record CreateEvaluationQuestionRequest(
    string Code,
    string Question,
    string ExpectedAnswer,
    IReadOnlyCollection<string> ExpectedKeywords,
    string? ExpectedSourceTitle,
    string? ExpectedDocumentType,
    string Category,
    bool IsActive = true);

public sealed record RunEvaluationRequest(
    string? Name,
    int TopK = 5,
    string? Category = null);

public sealed record EvaluationSourceDto(
    string PointId,
    double Score,
    string Content,
    string? Title,
    string? DocumentType,
    int? PageNumber);

public sealed record EvaluationResultDto(
    Guid Id,
    Guid EvaluationQuestionId,
    string QuestionCode,
    string Question,
    string RetrievalBackend,
    int TopK,
    double TopScore,
    bool HitAtK,
    double KeywordHitRate,
    bool IsCorrect,
    int LatencyMs,
    string AnswerPreview,
    IReadOnlyCollection<string> MatchedKeywords,
    IReadOnlyCollection<EvaluationSourceDto> Sources,
    string? ErrorMessage);

public sealed record EvaluationRunDto(
    Guid Id,
    string Name,
    string Status,
    int TopK,
    int TotalQuestions,
    int CorrectQuestions,
    double HitRateAtK,
    double AverageKeywordHitRate,
    double AverageTopScore,
    double AverageLatencyMs,
    DateTime StartedAt,
    DateTime? FinishedAt,
    string? ErrorMessage,
    IReadOnlyCollection<EvaluationResultDto> Results);

public sealed record EvaluationRunListResponse(
    IReadOnlyCollection<EvaluationRunDto> Items,
    int TotalItems);
