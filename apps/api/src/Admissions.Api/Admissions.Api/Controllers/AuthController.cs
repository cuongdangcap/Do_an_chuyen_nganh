using Admissions.Api.Common;
using Admissions.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Admissions.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register-student")]
    [AllowAnonymous]
    public IActionResult RegisterStudent()
    {
        return BadRequest(ApiResponse<object>.Fail(
            "STUDENT_SELF_REGISTER_DISABLED",
            "Tài khoản sinh viên do nhà trường cấp, không tự đăng ký trên cổng này.",
            HttpContext.TraceIdentifier));
    }

    [HttpPost("register-parent")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterParent(RegisterRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await authService.RegisterParentAsync(request, cancellationToken);
            return Ok(ApiResponse<UserSummary>.Ok(user, "Register parent successfully.", HttpContext.TraceIdentifier));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<object>.Fail("RESOURCE_CONFLICT", ex.Message, HttpContext.TraceIdentifier));
        }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await authService.LoginAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
            return Ok(ApiResponse<AuthResponse>.Ok(response, "Login successfully.", HttpContext.TraceIdentifier));
        }
        catch (InvalidOperationException)
        {
            return Unauthorized(ApiResponse<object>.Fail("AUTH_INVALID_CREDENTIALS", "Invalid email or password.", HttpContext.TraceIdentifier));
        }
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await authService.RefreshAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
            return Ok(ApiResponse<AuthResponse>.Ok(response, "Token refreshed.", HttpContext.TraceIdentifier));
        }
        catch (InvalidOperationException)
        {
            return Unauthorized(ApiResponse<object>.Fail("AUTH_TOKEN_EXPIRED", "Refresh token is invalid or expired.", HttpContext.TraceIdentifier));
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(request, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Logout successfully.", HttpContext.TraceIdentifier));
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<object>.Fail("AUTH_INVALID_TOKEN", "Invalid token.", HttpContext.TraceIdentifier));
        }

        try
        {
            await authService.ChangePasswordAsync(userId.Value, request, cancellationToken);
            return Ok(ApiResponse<object>.Ok(new { }, "Password changed successfully.", HttpContext.TraceIdentifier));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<object>.Fail("USER_NOT_FOUND", "User not found.", HttpContext.TraceIdentifier));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail("PASSWORD_CHANGE_FAILED", ex.Message, HttpContext.TraceIdentifier));
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<object>.Fail("AUTH_INVALID_TOKEN", "Invalid token.", HttpContext.TraceIdentifier));
        }

        var user = await authService.GetCurrentUserAsync(userId.Value, cancellationToken);
        return user is null
            ? NotFound(ApiResponse<object>.Fail("USER_NOT_FOUND", "User not found.", HttpContext.TraceIdentifier))
            : Ok(ApiResponse<UserSummary>.Ok(user, "OK", HttpContext.TraceIdentifier));
    }
}
