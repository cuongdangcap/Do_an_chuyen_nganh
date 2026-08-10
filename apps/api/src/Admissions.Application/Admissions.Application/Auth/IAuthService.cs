namespace Admissions.Application.Auth;

public interface IAuthService
{
    Task<UserSummary> RegisterStudentAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<UserSummary> RegisterParentAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken);
    Task<UserSummary?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken);
    Task<UserSummary> CreateStaffAsync(CreateStaffRequest request, Guid? assignedBy, CancellationToken cancellationToken);
    Task<UserSummary> CreateManagedAccountAsync(CreateManagedAccountRequest request, Guid? assignedBy, CancellationToken cancellationToken);
    Task<UserSummary> UpdateStatusAsync(Guid userId, UpdateUserStatusRequest request, Guid actorId, CancellationToken cancellationToken);
    Task<UserSummary> UpdateRolesAsync(Guid userId, UpdateUserRolesRequest request, Guid? assignedBy, CancellationToken cancellationToken);
}