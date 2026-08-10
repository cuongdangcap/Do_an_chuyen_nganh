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

        ValidatePassword(request.NewPassword);
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
            : request.Roles.Select(NormalizeRole).Distinct().ToArray();
        if (roles.Any(role => role is not RoleCodes.Staff and not RoleCodes.Admin))
        {
            throw new InvalidOperationException("Staff accounts can only receive staff/admin roles.");
        }

        ValidatePassword(request.TemporaryPassword);
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

    public async Task<UserSummary> CreateManagedAccountAsync(CreateManagedAccountRequest request, Guid? assignedBy, CancellationToken cancellationToken)
    {
        var role = NormalizeRole(request.Role);
        if (!RoleCodes.All.Contains(role, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Vai trò tài khoản không hợp lệ.");
        }

        ValidatePassword(request.TemporaryPassword);
        var user = await CreateUserAsync(
            request.Email,
            request.TemporaryPassword,
            request.FullName,
            request.Phone,
            [role],
            assignedBy,
            cancellationToken);

        ApplyProfileForRole(user, role, request.Department, request.Position);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToSummary(user);
    }

    public async Task<UserSummary> UpdateStatusAsync(Guid userId, UpdateUserStatusRequest request, Guid actorId, CancellationToken cancellationToken)
    {
        var user = await LoadUserByIdAsync(userId, cancellationToken) ?? throw new KeyNotFoundException("User not found.");
        var normalizedStatus = request.Status.Trim().ToLowerInvariant();
        if (normalizedStatus is not "active" and not "inactive")
        {
            throw new InvalidOperationException("Trạng thái tài khoản chỉ có thể là đang hoạt động hoặc đã khóa.");
        }

        var roles = GetRoleCodes(user);
        if (user.Id == actorId || roles.Contains(RoleCodes.Admin) || roles.Contains(RoleCodes.Staff))
        {
            throw new InvalidOperationException("Không thể khóa hoặc thay đổi trạng thái tài khoản quản trị trong màn hình tài khoản người dùng.");
        }

        user.Status = normalizedStatus;
        user.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToSummary(user);
    }

    public async Task<UserSummary> UpdateRolesAsync(Guid userId, UpdateUserRolesRequest request, Guid? assignedBy, CancellationToken cancellationToken)
    {
        var user = await LoadUserByIdAsync(userId, cancellationToken) ?? throw new KeyNotFoundException("User not found.");
        if (assignedBy == userId)
        {
            throw new InvalidOperationException("Không thể tự thay đổi vai trò của chính tài khoản đang đăng nhập.");
        }

        var roleCodes = request.Roles.Select(NormalizeRole).Distinct().ToArray();
        if (roleCodes.Length != 1)
        {
            throw new InvalidOperationException("Mỗi tài khoản quản lý từ màn hình này phải có đúng một vai trò chính.");
        }

        var requestedRole = roleCodes[0];
        if (!RoleCodes.All.Contains(requestedRole, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Vai trò tài khoản không hợp lệ.");
        }

        var currentRoles = GetRoleCodes(user);
        if (currentRoles.Contains(RoleCodes.Admin) && requestedRole != RoleCodes.Admin)
        {
            var otherActiveAdmins = await dbContext.UserRoles
                .Where(x => x.Role.Code == RoleCodes.Admin && x.UserId != userId && x.User.Status == "active")
                .CountAsync(cancellationToken);
            if (otherActiveAdmins == 0)
            {
                throw new InvalidOperationException("Không thể hạ vai trò quản trị viên cuối cùng của hệ thống.");
            }
        }

        var role = await dbContext.Roles.FirstOrDefaultAsync(x => x.Code == requestedRole, cancellationToken)
            ?? throw new InvalidOperationException("Vai trò tài khoản không tồn tại trong cơ sở dữ liệu.");

        user.UserRoles.Clear();
        user.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id,
            Role = role,
            AssignedBy = assignedBy,
        });

        SynchronizeProfiles(user, requestedRole);
        user.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToSummary(user);
    }

    private async Task<UserSummary> RegisterAsync(RegisterRequest request, string roleCode, CancellationToken cancellationToken)
    {
        ValidatePassword(request.Password);
        var user = await CreateUserAsync(
            request.Email,
            request.Password,
            request.FullName,
            request.Phone,
            [roleCode],
            assignedBy: null,
            cancellationToken);

        ApplyProfileForRole(user, roleCode, null, null);
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
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new InvalidOperationException("Họ tên không được để trống.");
        }

        var normalizedEmail = NormalizeEmail(email);
        if (string.IsNullOrWhiteSpace(normalizedEmail) || !normalizedEmail.Contains('@'))
        {
            throw new InvalidOperationException("Email không hợp lệ.");
        }

        if (await dbContext.Users.AnyAsync(x => x.Email == normalizedEmail, cancellationToken))
        {
            throw new InvalidOperationException("Email already exists.");
        }

        var normalizedRoles = roleCodes.Select(NormalizeRole).Distinct().ToArray();
        var roles = await dbContext.Roles
            .Where(x => normalizedRoles.Contains(x.Code))
            .ToListAsync(cancellationToken);

        if (roles.Count != normalizedRoles.Length)
        {
            throw new InvalidOperationException("One or more roles are invalid.");
        }

        var user = new User
        {
            Email = normalizedEmail,
            PasswordHash = passwordHasher.Hash(password),
            FullName = fullName.Trim(),
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            EmailVerifiedAt = DateTime.UtcNow,
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

    private void SynchronizeProfiles(User user, string role)
    {
        if (role == RoleCodes.Student)
        {
            if (user.ParentProfile is not null) dbContext.ParentProfiles.Remove(user.ParentProfile);
            if (user.StaffProfile is not null) dbContext.StaffProfiles.Remove(user.StaffProfile);
            user.ParentProfile = null;
            user.StaffProfile = null;
            user.StudentProfile ??= new StudentProfile { UserId = user.Id };
            return;
        }

        if (role == RoleCodes.Parent)
        {
            if (user.StudentProfile is not null) dbContext.StudentProfiles.Remove(user.StudentProfile);
            if (user.StaffProfile is not null) dbContext.StaffProfiles.Remove(user.StaffProfile);
            user.StudentProfile = null;
            user.StaffProfile = null;
            user.ParentProfile ??= new ParentProfile { UserId = user.Id };
            return;
        }

        if (user.StudentProfile is not null) dbContext.StudentProfiles.Remove(user.StudentProfile);
        if (user.ParentProfile is not null) dbContext.ParentProfiles.Remove(user.ParentProfile);
        user.StudentProfile = null;
        user.ParentProfile = null;
        user.StaffProfile ??= new StaffProfile
        {
            UserId = user.Id,
            Department = "Phòng Tuyển sinh CMCU",
            Position = role == RoleCodes.Admin ? "Quản trị viên" : "Nhân viên",
            CanManageDocuments = true,
            CanReplyChat = true,
        };
    }

    private static void ApplyProfileForRole(User user, string role, string? department, string? position)
    {
        if (role == RoleCodes.Student)
        {
            user.StudentProfile = new StudentProfile { UserId = user.Id };
        }
        else if (role == RoleCodes.Parent)
        {
            user.ParentProfile = new ParentProfile { UserId = user.Id };
        }
        else
        {
            user.StaffProfile = new StaffProfile
            {
                UserId = user.Id,
                Department = string.IsNullOrWhiteSpace(department) ? "Phòng Tuyển sinh CMCU" : department.Trim(),
                Position = string.IsNullOrWhiteSpace(position)
                    ? role == RoleCodes.Admin ? "Quản trị viên" : "Nhân viên"
                    : position.Trim(),
                CanManageDocuments = true,
                CanReplyChat = true,
            };
        }
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
            .Include(x => x.StudentProfile)
            .Include(x => x.ParentProfile)
            .Include(x => x.StaffProfile)
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

    private static string NormalizeRole(string role)
    {
        return role.Trim().ToLowerInvariant();
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            throw new InvalidOperationException("Mật khẩu phải có ít nhất 8 ký tự.");
        }
    }
}