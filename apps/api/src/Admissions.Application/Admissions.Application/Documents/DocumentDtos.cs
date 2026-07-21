using System.IO;

namespace Admissions.Application.Documents;

public sealed record UploadKnowledgeDocumentCommand(
    string Title,
    string DocumentType,
    string? Source,
    string FileName,
    string? ContentType,
    long FileSizeBytes,
    Stream Content,
    Guid? UploadedBy);

public sealed record KnowledgeDocumentDto(
    Guid Id,
    string Title,
    string DocumentType,
    string? Source,
    string Status,
    Guid? UploadedBy,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyCollection<DocumentVersionDto> Versions);

public sealed record DocumentVersionDto(
    Guid Id,
    Guid DocumentId,
    int VersionNo,
    string FileName,
    string FileType,
    long FileSizeBytes,
    string? Checksum,
    string ProcessingStatus,
    string? ErrorMessage,
    DateTime CreatedAt,
    int ChunkCount);

public sealed record DocumentChunkDto(
    Guid Id,
    Guid DocumentVersionId,
    int ChunkIndex,
    int? PageNumber,
    string? SectionTitle,
    string Content,
    int? TokenCount,
    string QdrantCollection,
    string? QdrantPointId,
    bool IsActive,
    DateTime CreatedAt);

public sealed record IngestionJobDto(
    Guid Id,
    Guid DocumentVersionId,
    string JobType,
    string Status,
    DateTime? StartedAt,
    DateTime? FinishedAt,
    string? ErrorMessage,
    DateTime CreatedAt);

public sealed record DocumentListResponse(
    IReadOnlyCollection<KnowledgeDocumentDto> Items,
    int TotalItems);

public sealed record ProcessDocumentResponse(
    DocumentVersionDto Version,
    IngestionJobDto Job,
    IReadOnlyCollection<DocumentChunkDto> Chunks);
