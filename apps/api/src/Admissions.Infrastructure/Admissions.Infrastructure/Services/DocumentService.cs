using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Admissions.Application.Documents;
using Admissions.Domain.Entities;
using Admissions.Infrastructure.Options;
using Admissions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Admissions.Infrastructure.Services;

public sealed class DocumentService(
    AdmissionsDbContext dbContext,
    DocumentIngestionClient ingestionClient,
    IOptions<DocumentStorageOptions> storageOptions) : IDocumentService
{
    private static readonly HashSet<string> AllowedFileTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "pdf",
        "docx",
        "png",
        "jpg",
        "jpeg",
        "txt",
        "md",
    };

    private readonly DocumentStorageOptions _storageOptions = storageOptions.Value;

    public async Task<DocumentListResponse> ListAsync(string? status, string? documentType, CancellationToken cancellationToken)
    {
        var query = dbContext.KnowledgeDocuments
            .Include(x => x.Versions)
            .ThenInclude(x => x.Chunks)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalized = status.Trim().ToLowerInvariant();
            query = query.Where(x => x.Status == normalized);
        }

        if (!string.IsNullOrWhiteSpace(documentType))
        {
            var normalized = documentType.Trim().ToLowerInvariant();
            query = query.Where(x => x.DocumentType == normalized);
        }

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return new DocumentListResponse(items.Select(ToDocumentDto).ToList(), items.Count);
    }

    public async Task<KnowledgeDocumentDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var document = await dbContext.KnowledgeDocuments
            .Include(x => x.Versions)
            .ThenInclude(x => x.Chunks)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return document is null ? null : ToDocumentDto(document);
    }

    public async Task<IReadOnlyCollection<DocumentChunkDto>> ListChunksAsync(Guid documentVersionId, CancellationToken cancellationToken)
    {
        var chunks = await dbContext.DocumentChunks
            .Where(x => x.DocumentVersionId == documentVersionId)
            .OrderBy(x => x.ChunkIndex)
            .ToListAsync(cancellationToken);
        return chunks.Select(ToChunkDto).ToList();
    }

    public async Task<DocumentVersionDto> UploadAsync(UploadKnowledgeDocumentCommand command, CancellationToken cancellationToken)
    {
        if (command.FileSizeBytes <= 0)
        {
            throw new InvalidOperationException("Uploaded file is empty.");
        }

        if (command.FileSizeBytes > _storageOptions.MaxFileSizeBytes)
        {
            throw new InvalidOperationException("Uploaded file is too large.");
        }

        var fileType = NormalizeFileType(command.FileName);
        if (!AllowedFileTypes.Contains(fileType))
        {
            throw new InvalidOperationException("Unsupported file type.");
        }

        var document = new KnowledgeDocument
        {
            Title = command.Title.Trim(),
            DocumentType = NormalizeDocumentType(command.DocumentType),
            Source = command.Source,
            Status = "processing",
            UploadedBy = command.UploadedBy,
        };
        var version = new DocumentVersion
        {
            DocumentId = document.Id,
            Document = document,
            VersionNo = 1,
            FileName = Path.GetFileName(command.FileName),
            FileType = fileType,
            ContentType = command.ContentType,
            FileSizeBytes = command.FileSizeBytes,
            ProcessingStatus = "pending",
        };
        var job = new IngestionJob
        {
            DocumentVersionId = version.Id,
            DocumentVersion = version,
            JobType = "parse_chunk_embed",
            Status = "pending",
        };

        var directory = Path.Combine(GetStorageRoot(), document.Id.ToString(), version.Id.ToString());
        Directory.CreateDirectory(directory);
        var filePath = Path.Combine(directory, version.FileName);
        version.FilePath = filePath;
        version.Checksum = await SaveFileAndHashAsync(command.Content, filePath, cancellationToken);

        dbContext.KnowledgeDocuments.Add(document);
        dbContext.DocumentVersions.Add(version);
        dbContext.IngestionJobs.Add(job);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToVersionDto(version, 0);
    }

    public async Task<ProcessDocumentResponse> ProcessAsync(Guid documentVersionId, CancellationToken cancellationToken)
    {
        var version = await dbContext.DocumentVersions
            .Include(x => x.Document)
            .Include(x => x.Chunks)
            .Include(x => x.IngestionJobs)
            .FirstOrDefaultAsync(x => x.Id == documentVersionId, cancellationToken)
            ?? throw new KeyNotFoundException("Document version not found.");

        var isNewJob = false;
        var job = version.IngestionJobs
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefault()
            ?? new IngestionJob
            {
                DocumentVersionId = version.Id,
                JobType = "parse_chunk_embed",
            };

        if (!version.IngestionJobs.Contains(job))
        {
            isNewJob = true;
        }

        if (isNewJob)
        {
            dbContext.IngestionJobs.Add(job);
        }

        version.ProcessingStatus = "processing";
        version.ErrorMessage = null;
        version.Document.Status = "processing";
        version.Document.UpdatedAt = DateTime.UtcNow;
        job.Status = "running";
        job.StartedAt = DateTime.UtcNow;
        job.FinishedAt = null;
        job.ErrorMessage = null;
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var result = await ingestionClient.ProcessAsync(version, cancellationToken);

            dbContext.DocumentChunks.RemoveRange(version.Chunks);
            var chunks = result.Chunks.Select(chunk => new DocumentChunk
            {
                DocumentVersionId = version.Id,
                ChunkIndex = chunk.ChunkIndex,
                PageNumber = chunk.PageNumber,
                SectionTitle = chunk.SectionTitle,
                Content = chunk.Content,
                TokenCount = chunk.TokenCount,
                QdrantCollection = result.VectorCollection,
                QdrantPointId = chunk.PointId,
                MetadataJson = JsonSerializer.Serialize(chunk.Metadata),
                IsActive = true,
            }).ToList();

            dbContext.DocumentChunks.AddRange(chunks);
            version.ProcessingStatus = "completed";
            version.Document.Status = "active";
            version.Document.UpdatedAt = DateTime.UtcNow;
            job.Status = "completed";
            job.FinishedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            await ingestionClient.UpsertAsync(chunks, cancellationToken);

            return new ProcessDocumentResponse(
                ToVersionDto(version, chunks.Count),
                ToJobDto(job),
                chunks.Select(ToChunkDto).ToList());
        }
        catch (Exception ex)
        {
            version.ProcessingStatus = "failed";
            version.ErrorMessage = ex.Message;
            version.Document.Status = "failed";
            version.Document.UpdatedAt = DateTime.UtcNow;
            job.Status = "failed";
            job.ErrorMessage = ex.Message;
            job.FinishedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private string GetStorageRoot()
    {
        return Path.IsPathRooted(_storageOptions.DocumentsPath)
            ? _storageOptions.DocumentsPath
            : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), _storageOptions.DocumentsPath));
    }

    private static async Task<string> SaveFileAndHashAsync(Stream input, string filePath, CancellationToken cancellationToken)
    {
        using var sha256 = SHA256.Create();
        await using var output = File.Create(filePath);
        var buffer = new byte[81920];
        int read;
        while ((read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            sha256.TransformBlock(buffer, 0, read, null, 0);
        }

        sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return Convert.ToHexString(sha256.Hash ?? Array.Empty<byte>()).ToLowerInvariant();
    }

    private static string NormalizeFileType(string fileName)
    {
        return Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
    }

    private static string NormalizeDocumentType(string documentType)
    {
        return string.IsNullOrWhiteSpace(documentType) ? "regulation" : documentType.Trim().ToLowerInvariant();
    }

    private static KnowledgeDocumentDto ToDocumentDto(KnowledgeDocument document)
    {
        return new KnowledgeDocumentDto(
            document.Id,
            document.Title,
            document.DocumentType,
            document.Source,
            document.Status,
            document.UploadedBy,
            document.CreatedAt,
            document.UpdatedAt,
            document.Versions.OrderByDescending(x => x.VersionNo).Select(x => ToVersionDto(x, x.Chunks.Count)).ToList());
    }

    private static DocumentVersionDto ToVersionDto(DocumentVersion version, int chunkCount)
    {
        return new DocumentVersionDto(
            version.Id,
            version.DocumentId,
            version.VersionNo,
            version.FileName,
            version.FileType,
            version.FileSizeBytes,
            version.Checksum,
            version.ProcessingStatus,
            version.ErrorMessage,
            version.CreatedAt,
            chunkCount);
    }

    private static DocumentChunkDto ToChunkDto(DocumentChunk chunk)
    {
        return new DocumentChunkDto(
            chunk.Id,
            chunk.DocumentVersionId,
            chunk.ChunkIndex,
            chunk.PageNumber,
            chunk.SectionTitle,
            chunk.Content,
            chunk.TokenCount,
            chunk.QdrantCollection,
            chunk.QdrantPointId,
            chunk.IsActive,
            chunk.CreatedAt);
    }

    private static IngestionJobDto ToJobDto(IngestionJob job)
    {
        return new IngestionJobDto(
            job.Id,
            job.DocumentVersionId,
            job.JobType,
            job.Status,
            job.StartedAt,
            job.FinishedAt,
            job.ErrorMessage,
            job.CreatedAt);
    }
}

public sealed class DocumentIngestionClient(HttpClient httpClient)
{
    public async Task<AiHealthResponse?> GetHealthAsync(CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync("/health", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<AiHealthResponse>(cancellationToken: cancellationToken);
    }

    public async Task<AiIngestionResponse> ProcessAsync(DocumentVersion version, CancellationToken cancellationToken)
    {
        await using var fileStream = File.OpenRead(version.FilePath);
        return await ProcessFileAsync(
            fileStream,
            version.FileName,
            version.ContentType,
            version.Document.Id,
            version.Id,
            version.Document.Title,
            version.Document.DocumentType,
            cancellationToken);
    }

    public async Task<AiIngestionResponse> ProcessFileAsync(
        Stream fileStream,
        string fileName,
        string? contentType,
        Guid documentId,
        Guid documentVersionId,
        string title,
        string documentType,
        CancellationToken cancellationToken)
    {
        using var form = new MultipartFormDataContent();
        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType ?? "application/octet-stream");
        form.Add(fileContent, "file", fileName);
        form.Add(new StringContent(documentId.ToString()), "document_id");
        form.Add(new StringContent(documentVersionId.ToString()), "document_version_id");
        form.Add(new StringContent(title), "title");
        form.Add(new StringContent(documentType), "document_type");

        var response = await httpClient.PostAsync("/internal/ingestion/process", form, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<AiIngestionResponse>(cancellationToken: cancellationToken);
        if (!response.IsSuccessStatusCode || payload is null || !payload.Success)
        {
            throw new InvalidOperationException(payload?.Message ?? $"AI ingestion failed with HTTP {(int)response.StatusCode}.");
        }

        if (payload.Chunks.Count == 0)
        {
            throw new InvalidOperationException("AI ingestion produced no chunks.");
        }

        return payload;
    }

    public async Task<AiUpsertResponse> UpsertAsync(IReadOnlyCollection<DocumentChunk> chunks, CancellationToken cancellationToken)
    {
        var request = new AiUpsertRequest(
            "admissions_docs",
            chunks.Select(chunk => new AiUpsertChunk(
                chunk.QdrantPointId ?? chunk.Id.ToString(),
                chunk.Content,
                JsonSerializer.Deserialize<Dictionary<string, object?>>(chunk.MetadataJson ?? "{}") ?? new Dictionary<string, object?>())).ToList());

        var response = await httpClient.PostAsJsonAsync("/internal/rag/upsert", request, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<AiUpsertResponse>(cancellationToken: cancellationToken);
        if (!response.IsSuccessStatusCode || payload is null || !payload.Success)
        {
            throw new InvalidOperationException(payload?.Message ?? $"Vector upsert failed with HTTP {(int)response.StatusCode}.");
        }

        return payload;
    }

    public async Task<AiSearchResponse> SearchAsync(string query, int topK, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync(
            "/internal/rag/search",
            new AiSearchRequest(query, Math.Clamp(topK, 1, 20), "admissions_docs"),
            cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<AiSearchResponse>(cancellationToken: cancellationToken);
        if (!response.IsSuccessStatusCode || payload is null || !payload.Success)
        {
            throw new InvalidOperationException(payload?.Message ?? $"Vector search failed with HTTP {(int)response.StatusCode}.");
        }

        return payload;
    }
}

public sealed record AiIngestionResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("extracted_text")] string ExtractedText,
    [property: JsonPropertyName("vector_collection")] string VectorCollection,
    [property: JsonPropertyName("chunks")] IReadOnlyCollection<AiChunkResponse> Chunks);

public sealed record AiChunkResponse(
    [property: JsonPropertyName("chunk_index")] int ChunkIndex,
    [property: JsonPropertyName("page_number")] int? PageNumber,
    [property: JsonPropertyName("section_title")] string? SectionTitle,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("token_count")] int TokenCount,
    [property: JsonPropertyName("point_id")] string PointId,
    [property: JsonPropertyName("metadata")] Dictionary<string, object?> Metadata);

public sealed record AiUpsertRequest(
    [property: JsonPropertyName("collection")] string Collection,
    [property: JsonPropertyName("chunks")] IReadOnlyCollection<AiUpsertChunk> Chunks);

public sealed record AiUpsertChunk(
    [property: JsonPropertyName("point_id")] string PointId,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("metadata")] Dictionary<string, object?> Metadata);

public sealed record AiUpsertResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("backend")] string Backend,
    [property: JsonPropertyName("count")] int Count);

public sealed record AiSearchRequest(
    [property: JsonPropertyName("query")] string Query,
    [property: JsonPropertyName("top_k")] int TopK,
    [property: JsonPropertyName("collection")] string Collection);

public sealed record AiSearchResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("backend")] string Backend,
    [property: JsonPropertyName("results")] IReadOnlyCollection<AiSearchResult> Results);

public sealed record AiSearchResult(
    [property: JsonPropertyName("point_id")] string PointId,
    [property: JsonPropertyName("score")] double Score,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("metadata")] Dictionary<string, object?> Metadata);

public sealed record AiHealthResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("service")] string Service,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("vector")] AiVectorStatusResponse? Vector,
    [property: JsonPropertyName("utc_now")] DateTime? UtcNow);

public sealed record AiVectorStatusResponse(
    [property: JsonPropertyName("backend")] string Backend,
    [property: JsonPropertyName("qdrant_available")] bool QdrantAvailable,
    [property: JsonPropertyName("qdrant_url")] string QdrantUrl,
    [property: JsonPropertyName("local_path")] string LocalPath);
