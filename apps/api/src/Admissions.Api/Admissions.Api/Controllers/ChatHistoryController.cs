using Admissions.Api.Common;
using Admissions.Application.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Admissions.Api.Controllers;

[ApiController]
[Route("api/chat/conversations")]
[AllowAnonymous]
public sealed class ChatHistoryController(IChatHistoryService chatHistoryService) : ControllerBase
{
    [HttpPost("file-question")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> AskFile(
        [FromForm] string question,
        [FromForm] string? clientSessionId,
        [FromForm] Guid? conversationId,
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
            var result = await chatHistoryService.AskFileAsync(
                new ChatFileQuestionCommand(
                    question,
                    clientSessionId,
                    conversationId,
                    file.FileName,
                    file.ContentType,
                    file.Length,
                    stream,
                    User.GetUserId()),
                cancellationToken);
            return Ok(ApiResponse<ChatConversationDetailDto>.Ok(result, "File question answered.", HttpContext.TraceIdentifier));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail("CHAT_FILE_ERROR", ex.Message, HttpContext.TraceIdentifier));
        }
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? clientSessionId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await chatHistoryService.ListAsync(User.GetUserId(), clientSessionId, page, pageSize, cancellationToken);
        return Ok(ApiResponse<ChatConversationListResponse>.Ok(result, "OK", HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(
        Guid id,
        [FromQuery] string? clientSessionId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await chatHistoryService.GetAsync(id, User.GetUserId(), clientSessionId, cancellationToken);
        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail("CHAT_CONVERSATION_NOT_FOUND", "Chat conversation not found.", HttpContext.TraceIdentifier));
        }

        return Ok(ApiResponse<ChatConversationDetailDto>.Ok(result, "OK", HttpContext.TraceIdentifier));
    }
}
