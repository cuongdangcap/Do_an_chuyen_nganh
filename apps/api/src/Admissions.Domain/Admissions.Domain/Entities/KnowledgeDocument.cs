namespace Admissions.Domain.Entities;

public sealed class KnowledgeDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string DocumentType { get; set; } = "regulation";
    public string? Source { get; set; }
    public string Status { get; set; } = "processing";
    public Guid? UploadedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User? UploadedByUser { get; set; }
    public ICollection<DocumentVersion> Versions { get; set; } = [];
}
