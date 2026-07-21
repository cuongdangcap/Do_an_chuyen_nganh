namespace Admissions.Application.Documents;

public interface IDocumentService
{
    Task<DocumentListResponse> ListAsync(string? status, string? documentType, CancellationToken cancellationToken);
    Task<KnowledgeDocumentDto?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DocumentChunkDto>> ListChunksAsync(Guid documentVersionId, CancellationToken cancellationToken);
    Task<DocumentVersionDto> UploadAsync(UploadKnowledgeDocumentCommand command, CancellationToken cancellationToken);
    Task<ProcessDocumentResponse> ProcessAsync(Guid documentVersionId, CancellationToken cancellationToken);
}
