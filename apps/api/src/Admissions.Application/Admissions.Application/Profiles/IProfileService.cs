namespace Admissions.Application.Profiles;

public interface IProfileService
{
    Task<ProfileResponse?> GetAsync(Guid userId, CancellationToken cancellationToken);
    Task<ProfileResponse> UpdateAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken);
}
