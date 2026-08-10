namespace Admissions.Application.Auth;

public sealed record RegisterRequest(
    string Email,
    string Password,
    string FullName,
    string? Phone);

public sealed record LoginRequest(
    string Email,
    string Password);

public sealed record RefreshTokenRequest(
    string RefreshToken);

public sealed record LogoutRequest(
    string RefreshToken);

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    UserSummary User);

public sealed record UserSummary(
    Guid Id,
    string Email,
    string FullName,
    string? Phone,
    string Status,
    IReadOnlyCollection<string> Roles);

public sealed record CreateStaffRequest(
    string Email,
    string FullName,
    string? Phone,
    string? Department,
    string? Position,
    IReadOnlyCollection<string> Roles,
    string TemporaryPassword);

public sealed record CreateManagedAccountRequest(
    string Email,
    string FullName,
    string? Phone,
    string Role,
    string TemporaryPassword,
    string? Department = null,
    string? Position = null);

public sealed record UpdateUserStatusRequest(
    string Status,
    string? Reason);

public sealed record UpdateUserRolesRequest(
    IReadOnlyCollection<string> Roles);