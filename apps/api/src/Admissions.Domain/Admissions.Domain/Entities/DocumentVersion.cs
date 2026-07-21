namespace Admissions.Domain.Entities;

public sealed class DocumentVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public int VersionNo { get; set; } = 1;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long FileSizeBytes { get; set; }
    public string? Checksum { get; set; }
    public string ProcessingStatus { get; set; } = "pending";
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public KnowledgeDocument Document { get; set; } = null!;
    public ICollection<DocumentChunk> Chunks { get; set; } = [];
    public ICollection<IngestionJob> IngestionJobs { get; set; } = [];
}
