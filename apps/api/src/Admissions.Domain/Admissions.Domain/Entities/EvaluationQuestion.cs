namespace Admissions.Domain.Entities;

public sealed class EvaluationQuestion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string ExpectedAnswer { get; set; } = string.Empty;
    public string ExpectedKeywordsJson { get; set; } = "[]";
    public string? ExpectedSourceTitle { get; set; }
    public string? ExpectedDocumentType { get; set; }
    public string Category { get; set; } = "general";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<EvaluationResult> Results { get; set; } = [];
}
