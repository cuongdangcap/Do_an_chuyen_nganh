using Admissions.Api.Common;
using Admissions.Application.Handoff;
using Admissions.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Admissions.Api.Controllers;

[ApiController]
public sealed class HandoffController(IHandoffService handoffService) : ControllerBase
{
    [HttpPost("api/handoff/tickets")]
    [AllowAnonymous]
    public async Task<IActionResult> Create(CreateHandoffTicketRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var ticket = await handoffService.CreateFromAssistantMessageAsync(request, User.GetUserId(), cancellationToken);
            return Ok(ApiResponse<HandoffTicketDto>.Ok(ticket, "Handoff ticket created.", HttpContext.TraceIdentifier));
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

    [HttpGet("api/admin/handoff/tickets")]
    [Authorize(Roles = RoleCodes.Admin + "," + RoleCodes.Staff)]
    public async Task<IActionResult> List(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tickets = await handoffService.ListAsync(status, page, pageSize, cancellationToken);
            return Ok(ApiResponse<HandoffTicketListResponse>.Ok(tickets, "OK", HttpContext.TraceIdentifier));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", ex.Message, HttpContext.TraceIdentifier));
        }
    }

    [HttpGet("api/admin/handoff/tickets/{id:guid}")]
    [Authorize(Roles = RoleCodes.Admin + "," + RoleCodes.Staff)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var ticket = await handoffService.GetAsync(id, cancellationToken);
        if (ticket is null)
        {
            return NotFound(ApiResponse<object>.Fail("HANDOFF_TICKET_NOT_FOUND", "Handoff ticket not found.", HttpContext.TraceIdentifier));
        }

        return Ok(ApiResponse<HandoffTicketDto>.Ok(ticket, "OK", HttpContext.TraceIdentifier));
    }

    [HttpPost("api/admin/handoff/tickets/{id:guid}/reply")]
    [Authorize(Roles = RoleCodes.Admin + "," + RoleCodes.Staff)]
    public async Task<IActionResult> Reply(Guid id, ReplyHandoffTicketRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var staffUserId = User.GetUserId() ?? throw new InvalidOperationException("Authenticated user id is missing.");
            var ticket = await handoffService.ReplyAsync(id, request, staffUserId, cancellationToken);
            return Ok(ApiResponse<HandoffTicketDto>.Ok(ticket, "Reply saved.", HttpContext.TraceIdentifier));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<object>.Fail("HANDOFF_TICKET_NOT_FOUND", "Handoff ticket not found.", HttpContext.TraceIdentifier));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", ex.Message, HttpContext.TraceIdentifier));
        }
    }

    [HttpPatch("api/admin/handoff/tickets/{id:guid}/status")]
    [Authorize(Roles = RoleCodes.Admin + "," + RoleCodes.Staff)]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateHandoffTicketStatusRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var staffUserId = User.GetUserId() ?? throw new InvalidOperationException("Authenticated user id is missing.");
            var ticket = await handoffService.UpdateStatusAsync(id, request, staffUserId, cancellationToken);
            return Ok(ApiResponse<HandoffTicketDto>.Ok(ticket, "Ticket status updated.", HttpContext.TraceIdentifier));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<object>.Fail("HANDOFF_TICKET_NOT_FOUND", "Handoff ticket not found.", HttpContext.TraceIdentifier));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", ex.Message, HttpContext.TraceIdentifier));
        }
    }
}
