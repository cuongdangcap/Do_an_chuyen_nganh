using Admissions.Api.Common;
using Admissions.Application.Rag;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Admissions.Api.Controllers;

[ApiController]
[Route("api/rag")]
public sealed class RagController(IRagService ragService) : ControllerBase
{
    [HttpPost("search")]
    [AllowAnonymous]
    public async Task<IActionResult> Search(RagSearchRequest request, CancellationToken cancellationToken)
    {
        var result = await ragService.SearchAsync(request, cancellationToken);
        return Ok(ApiResponse<RagSearchResponse>.Ok(result, "OK", HttpContext.TraceIdentifier));
    }

    [HttpPost("chat")]
    [AllowAnonymous]
    public async Task<IActionResult> Chat(RagChatRequest request, CancellationToken cancellationToken)
    {
        var result = await ragService.ChatAsync(request, User.GetUserId(), cancellationToken);
        return Ok(ApiResponse<RagChatResponse>.Ok(result, "OK", HttpContext.TraceIdentifier));
    }
}
