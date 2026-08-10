using Admissions.Api.Common;
using Admissions.Application.Auth;
using Admissions.Application.Users;
using Admissions.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Admissions.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = RoleCodes.Admin)]
public sealed class AdminUsersController(
    IAuthService authService,
    IUserQueryService userQueryService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? keyword = null,
        [FromQuery] string? role = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var users = await userQueryService.ListAsync(page, pageSize, keyword, role, status, cancellationToken);
        return Ok(ApiResponse<UserListResponse>.Ok(users, "OK", HttpContext.TraceIdentifier));
    }

    [HttpPost]
    public async Task<IActionResult> CreateManagedAccount(CreateManagedAccountRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var actorId = User.GetUserId() ?? throw new InvalidOperationException("Authenticated user id is missing.");
            var user = await authService.CreateManagedAccountAsync(request, actorId, cancellationToken);
            return Ok(ApiResponse<UserSummary>.Ok(user, "Tài khoản đã được tạo.", HttpContext.TraceIdentifier));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", ex.Message, HttpContext.TraceIdentifier));
        }
    }

    [HttpPost("staff")]
    public async Task<IActionResult> CreateStaff(CreateStaffRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var actorId = User.GetUserId() ?? throw new InvalidOperationException("Authenticated user id is missing.");
            var user = await authService.CreateStaffAsync(request, actorId, cancellationToken);
            return Ok(ApiResponse<UserSummary>.Ok(user, "Staff account created.", HttpContext.TraceIdentifier));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", ex.Message, HttpContext.TraceIdentifier));
        }
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateUserStatusRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var actorId = User.GetUserId() ?? throw new InvalidOperationException("Authenticated user id is missing.");
            var user = await authService.UpdateStatusAsync(id, request, actorId, cancellationToken);
            return Ok(ApiResponse<UserSummary>.Ok(user, "User status updated.", HttpContext.TraceIdentifier));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<object>.Fail("USER_NOT_FOUND", "User not found.", HttpContext.TraceIdentifier));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail("PROTECTED_ACCOUNT", ex.Message, HttpContext.TraceIdentifier));
        }
    }

    [HttpPut("{id:guid}/roles")]
    public async Task<IActionResult> UpdateRoles(Guid id, UpdateUserRolesRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var actorId = User.GetUserId() ?? throw new InvalidOperationException("Authenticated user id is missing.");
            var user = await authService.UpdateRolesAsync(id, request, actorId, cancellationToken);
            return Ok(ApiResponse<UserSummary>.Ok(user, "User roles updated.", HttpContext.TraceIdentifier));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<object>.Fail("USER_NOT_FOUND", "User not found.", HttpContext.TraceIdentifier));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", ex.Message, HttpContext.TraceIdentifier));
        }
    }
}