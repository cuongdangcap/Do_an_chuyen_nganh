using Admissions.Application.Users;
using Admissions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Admissions.Infrastructure.Services;

public sealed class UserQueryService(AdmissionsDbContext dbContext) : IUserQueryService
{
    public async Task<UserListResponse> ListAsync(
        int page,
        int pageSize,
        string? keyword,
        string? role,
        string? status,
        CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = dbContext.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .Where(x => x.UserRoles.Any(userRole =>
                userRole.Role.Code == "student" || userRole.Role.Code == "parent"))
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var normalizedKeyword = keyword.Trim().ToLowerInvariant();
            query = query.Where(x => x.Email.Contains(normalizedKeyword) || x.FullName.ToLower().Contains(normalizedKeyword));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToLowerInvariant();
            query = query.Where(x => x.Status == normalizedStatus);
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            var normalizedRole = role.Trim().ToLowerInvariant();
            query = query.Where(x => x.UserRoles.Any(userRole => userRole.Role.Code == normalizedRole));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new UserListItem(
                x.Id,
                x.Email,
                x.FullName,
                x.Status,
                x.UserRoles.Select(userRole => userRole.Role.Code).OrderBy(code => code).ToArray(),
                x.CreatedAt))
            .ToListAsync(cancellationToken);

        return new UserListResponse(
            items,
            page,
            pageSize,
            total,
            (int)Math.Ceiling(total / (double)pageSize));
    }
}
