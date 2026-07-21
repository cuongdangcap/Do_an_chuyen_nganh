using Admissions.Application.Auth;
using Admissions.Domain.Constants;
using Admissions.Domain.Entities;
using Admissions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Admissions.Infrastructure.Services;

public sealed class AuthService(
    AdmissionsDbContext dbContext,
    IPasswordHasher passwordHasher,
    ITokenService tokenService) : IAuthService
{
    public Task<UserSummary> RegisterStudentAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        return RegisterAsync(request, RoleCodes.Student, cancellationToken);
    }

    public Task<UserSummary> RegisterParentAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        return RegisterAsync(request, RoleCodes.Parent, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        var user = await LoadUserByEmailAsync(email, cancellationToken);
        if (user is null || user.Status != "active" || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new InvalidOperationException("Invalid credentials.");
        }

        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        var roles = GetRoleCodes(user);
        var refreshToken = tokenService.CreateRefreshToken();
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokenService.HashRefreshToken(refreshToken),
            ExpiresAt = tokenService.GetRefreshTokenExpiryUtc(),
            CreatedByIp = ipAddress,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return CreateAuthResponse(user, roles, refreshToken);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        var tokenHash = tokenService.HashRefreshToken(request.RefreshToken);
        var storedToken = await dbContext.RefreshTokens
            .Include(x => x.User)
            .ThenInclude(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null || storedToken.RevokedAt is not null || storedToken.ExpiresAt <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("Invalid refresh token.");
        }

        storedToken.RevokedAt = DateTime.UtcNow;

        var refreshToken = tokenService.CreateRefreshToken();
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = storedToken.UserId,
            TokenHash = tokenService.HashRefreshToken(refreshToken),
            ExpiresAt = tokenService.GetRefreshTokenExpiryUtc(),
            CreatedByIp = ipAddress,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return CreateAuthResponse(storedToken.User, GetRoleCodes(storedToken.User), refreshToken);
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken)
    {
        var tokenHash = tokenService.HashRefreshToken(request.RefreshToken);
        var storedToken = await dbContext.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
        if (storedToken is not null && storedToken.RevokedAt is null)
        {
            storedToken.RevokedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await LoadUserByIdAsync(userId, cancellationToken) ?? throw new KeyNotFoundException("User not found.");
        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new InvalidOperationException("Current password is incorrect.");
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
        {
            throw new InvalidOperationException("New password must have at least 8 characters.");
        }

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserSummary?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await LoadUserByIdAsync(userId, cancellationToken);
        return user is null ? null : ToSummary(user);
    }

    public async Task<UserSummary> CreateStaffAsync(CreateStaffRequest request, Guid? assignedBy, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<string> roles = request.Roles.Count == 0
            ? [RoleCodes.Staff]
            : request.Roles;
        if (roles.Any(role => role is not RoleCodes.Staff and not RoleCodes.Admin))
        {
            throw new InvalidOperationException("Staff accounts can only receive staff/admin roles.");
        }

        var user = await CreateUserAsync(
            request.Email,
            request.TemporaryPassword,
            request.FullName,
            request.Phone,
            roles,
            assignedBy,
            cancellationToken);

        user.StaffProfile = new StaffProfile
        {
            UserId = user.Id,
            Department = request.Department,
            Position = request.Position,
            CanManageDocuments = true,
            CanReplyChat = true,
        };

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToSummary(user);
    }

    public async Task<UserSummary> UpdateStatusAsync(Guid userId, UpdateUserStatusRequest request, CancellationToken cancellationToken)
    {
        var user = await LoadUserByIdAsync(userId, cancellationToken) ?? throw new KeyNotFoundException("User not found.");
        user.Status = request.Status.Trim().ToLowerInvariant();
        user.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToSummary(user);
    }

    public async Task<UserSummary> UpdateRolesAsync(Guid userId, UpdateUserRolesRequest request, Guid? assignedBy, CancellationToken cancellationToken)
    {
        var user = await LoadUserByIdAsync(userId, cancellationToken) ?? throw new KeyNotFoundException("User not found.");
        var roleCodes = request.Roles.Select(x => x.Trim().ToLowerInvariant()).Distinct().ToArray();
        var roles = await dbContext.Roles.Where(x => roleCodes.Contains(x.Code)).ToListAsync(cancellationToken);
        if (roles.Count != roleCodes.Length)
        {
            throw new InvalidOperationException("One or more roles are invalid.");
        }

        user.UserRoles.Clear();
        foreach (var role in roles)
        {
            user.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id,
                Role = role,
                AssignedBy = assignedBy,
            });
        }

        user.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToSummary(user);
    }

    private async Task<UserSummary> RegisterAsync(RegisterRequest request, string roleCode, CancellationToken cancellationToken)
    {
        var user = await CreateUserAsync(
            request.Email,
            request.Password,
            request.FullName,
            request.Phone,
            [roleCode],
            assignedBy: null,
            cancellationToken);

        if (roleCode == RoleCodes.Student)
        {
            user.StudentProfile = new StudentProfile { UserId = user.Id };
        }
        else if (roleCode == RoleCodes.Parent)
        {
            user.ParentProfile = new ParentProfile { UserId = user.Id };
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToSummary(user);
    }

    private async Task<User> CreateUserAsync(
        string email,
        string password,
        string fullName,
        string? phone,
        IReadOnlyCollection<string> roleCodes,
        Guid? assignedBy,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (await dbContext.Users.AnyAsync(x => x.Email == normalizedEmail, cancellationToken))
        {
            throw new InvalidOperationException("Email already exists.");
        }

        var roles = await dbContext.Roles
            .Where(x => roleCodes.Contains(x.Code))
            .ToListAsync(cancellationToken);

        if (roles.Count != roleCodes.Count)
        {
            throw new InvalidOperationException("One or more roles are invalid.");
        }

        var user = new User
        {
            Email = normalizedEmail,
            PasswordHash = passwordHasher.Hash(password),
            FullName = fullName.Trim(),
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
        };

        foreach (var role in roles)
        {
            user.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id,
                Role = role,
                AssignedBy = assignedBy,
            });
        }

        dbContext.Users.Add(user);
        return user;
    }

    private Task<User?> LoadUserByEmailAsync(string email, CancellationToken cancellationToken)
    {
        return dbContext.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
    }

    private Task<User?> LoadUserByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return dbContext.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
    }

    private AuthResponse CreateAuthResponse(User user, IReadOnlyCollection<string> roles, string refreshToken)
    {
        return new AuthResponse(
            tokenService.CreateAccessToken(user, roles),
            refreshToken,
            tokenService.AccessTokenSeconds,
            ToSummary(user));
    }

    private static UserSummary ToSummary(User user)
    {
        return new UserSummary(user.Id, user.Email, user.FullName, user.Phone, user.Status, GetRoleCodes(user));
    }

    private static IReadOnlyCollection<string> GetRoleCodes(User user)
    {
        return user.UserRoles.Select(x => x.Role.Code).OrderBy(x => x).ToArray();
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
