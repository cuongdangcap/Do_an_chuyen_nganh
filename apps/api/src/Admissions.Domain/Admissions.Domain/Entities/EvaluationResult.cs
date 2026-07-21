namespace Admissions.Domain.Entities;

public sealed class EvaluationResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EvaluationRunId { get; set; }
    public Guid EvaluationQuestionId { get; set; }
    public string RetrievalBackend { get; set; } = string.Empty;
    public int TopK { get; set; }
    public double TopScore { get; set; }
    public bool HitAtK { get; set; }
    public double KeywordHitRate { get; set; }
    public bool IsCorrect { get; set; }
    public int LatencyMs { get; set; }
    public string AnswerPreview { get; set; } = string.Empty;
    public string MatchedKeywordsJson { get; set; } = "[]";
    public string SourcesJson { get; set; } = "[]";
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public EvaluationRun EvaluationRun { get; set; } = null!;
    public EvaluationQuestion EvaluationQuestion { get; set; } = null!;
}
