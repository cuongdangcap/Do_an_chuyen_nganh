using Admissions.Domain.Entities;

namespace Admissions.Application.Auth;

public interface ITokenService
{
    int AccessTokenSeconds { get; }
    string CreateAccessToken(User user, IReadOnlyCollection<string> roles);
    string CreateRefreshToken();
    string HashRefreshToken(string refreshToken);
    DateTime GetRefreshTokenExpiryUtc();
}
