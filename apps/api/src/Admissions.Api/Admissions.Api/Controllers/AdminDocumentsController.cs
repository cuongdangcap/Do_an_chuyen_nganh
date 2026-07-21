using Admissions.Api.Common;
using Admissions.Application.Documents;
using Admissions.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Admissions.Api.Controllers;

[ApiController]
[Route("api/admin/documents")]
[Authorize(Roles = RoleCodes.Admin + "," + RoleCodes.Staff)]
public sealed class AdminDocumentsController(IDocumentService documentService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? status = null,
        [FromQuery] string? documentType = null,
        CancellationToken cancellationToken = default)
    {
        var documents = await documentService.ListAsync(status, documentType, cancellationToken);
        return Ok(ApiResponse<DocumentListResponse>.Ok(documents, "OK", HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var document = await documentService.GetAsync(id, cancellationToken);
        if (document is null)
        {
            return NotFound(ApiResponse<object>.Fail("DOCUMENT_NOT_FOUND", "Document not found.", HttpContext.TraceIdentifier));
        }

        return Ok(ApiResponse<KnowledgeDocumentDto>.Ok(document, "OK", HttpContext.TraceIdentifier));
    }

    [HttpGet("versions/{versionId:guid}/chunks")]
    public async Task<IActionResult> ListChunks(Guid versionId, CancellationToken cancellationToken)
    {
        var chunks = await documentService.ListChunksAsync(versionId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<DocumentChunkDto>>.Ok(chunks, "OK", HttpContext.TraceIdentifier));
    }

    [HttpPost]
    [RequestSizeLimit(30 * 1024 * 1024)]
    public async Task<IActionResult> Upload(
        [FromForm] string title,
        [FromForm] string documentType,
        [FromForm] string? source,
        [FromForm] bool processNow,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest(ApiResponse<object>.Fail("EMPTY_FILE", "Uploaded file is empty.", HttpContext.TraceIdentifier));
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var version = await documentService.UploadAsync(
                new UploadKnowledgeDocumentCommand(
                    title,
                    documentType,
                    source,
                    file.FileName,
                    file.ContentType,
                    file.Length,
                    stream,
                    User.GetUserId()),
                cancellationToken);

            if (!processNow)
            {
                return Ok(ApiResponse<DocumentVersionDto>.Ok(version, "Document uploaded.", HttpContext.TraceIdentifier));
            }

            var processed = await documentService.ProcessAsync(version.Id, cancellationToken);
            return Ok(ApiResponse<ProcessDocumentResponse>.Ok(processed, "Document uploaded and processed.", HttpContext.TraceIdentifier));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail("DOCUMENT_VALIDATION_ERROR", ex.Message, HttpContext.TraceIdentifier));
        }
    }

    [HttpPost("versions/{versionId:guid}/process")]
    public async Task<IActionResult> Process(Guid versionId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await documentService.ProcessAsync(versionId, cancellationToken);
            return Ok(ApiResponse<ProcessDocumentResponse>.Ok(result, "Document processed.", HttpContext.TraceIdentifier));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<object>.Fail("DOCUMENT_VERSION_NOT_FOUND", "Document version not found.", HttpContext.TraceIdentifier));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail("INGESTION_FAILED", ex.Message, HttpContext.TraceIdentifier));
        }
    }
}
