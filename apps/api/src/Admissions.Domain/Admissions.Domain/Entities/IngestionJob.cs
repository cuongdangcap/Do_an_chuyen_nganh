namespace Admissions.Domain.Entities;

public sealed class IngestionJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentVersionId { get; set; }
    public string JobType { get; set; } = "parse_chunk_embed";
    public string Status { get; set; } = "pending";
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DocumentVersion DocumentVersion { get; set; } = null!;
}
