using Admissions.Api.Common;
using Admissions.Application.Rag;
using Admissions.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Admissions.Api.Controllers;

[ApiController]
public sealed class ChatFeedbackController(IRagService ragService) : ControllerBase
{
    [HttpPost("api/chat/messages/{assistantMessageId:guid}/feedback")]
    [AllowAnonymous]
    public async Task<IActionResult> Create(Guid assistantMessageId, CreateChatFeedbackRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var feedback = await ragService.CreateFeedbackAsync(assistantMessageId, request, User.GetUserId(), cancellationToken);
            return Ok(ApiResponse<ChatFeedbackDto>.Ok(feedback, "Feedback saved.", HttpContext.TraceIdentifier));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<object>.Fail("MESSAGE_NOT_FOUND", "Assistant message not found.", HttpContext.TraceIdentifier));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", ex.Message, HttpContext.TraceIdentifier));
        }
    }

    [HttpGet("api/admin/chat/feedback")]
    [Authorize(Roles = RoleCodes.Admin + "," + RoleCodes.Staff)]
    public async Task<IActionResult> List(
        [FromQuery] string? rating = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var feedback = await ragService.ListFeedbackAsync(rating, page, pageSize, cancellationToken);
            return Ok(ApiResponse<ChatFeedbackListResponse>.Ok(feedback, "OK", HttpContext.TraceIdentifier));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", ex.Message, HttpContext.TraceIdentifier));
        }
    }
}
