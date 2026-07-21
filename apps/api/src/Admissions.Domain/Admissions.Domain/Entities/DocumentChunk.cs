namespace Admissions.Domain.Entities;

public sealed class DocumentChunk
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentVersionId { get; set; }
    public int ChunkIndex { get; set; }
    public int? PageNumber { get; set; }
    public string? SectionTitle { get; set; }
    public string Content { get; set; } = string.Empty;
    public int? TokenCount { get; set; }
    public string QdrantCollection { get; set; } = "admissions_docs";
    public string? QdrantPointId { get; set; }
    public string? MetadataJson { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DocumentVersion DocumentVersion { get; set; } = null!;
}
