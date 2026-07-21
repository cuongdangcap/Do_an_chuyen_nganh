namespace Admissions.Domain.Entities;

public sealed class EvaluationRun
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "running";
    public int TopK { get; set; } = 5;
    public int TotalQuestions { get; set; }
    public int CorrectQuestions { get; set; }
    public double HitRateAtK { get; set; }
    public double AverageKeywordHitRate { get; set; }
    public double AverageTopScore { get; set; }
    public double AverageLatencyMs { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedAt { get; set; }
    public string? ErrorMessage { get; set; }

    public ICollection<EvaluationResult> Results { get; set; } = [];
}
