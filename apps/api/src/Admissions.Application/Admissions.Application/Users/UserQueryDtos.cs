namespace Admissions.Application.Users;

public sealed record UserListItem(
    Guid Id,
    string Email,
    string FullName,
    string Status,
    IReadOnlyCollection<string> Roles,
    DateTime CreatedAt);

public sealed record UserListResponse(
    IReadOnlyCollection<UserListItem> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);

public interface IUserQueryService
{
    Task<UserListResponse> ListAsync(
        int page,
        int pageSize,
        string? keyword,
        string? role,
        string? status,
        CancellationToken cancellationToken);
}
