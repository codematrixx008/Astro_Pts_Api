using System.Security.Claims;

namespace Astro.Application.Auth;

public interface IJwtTokenService
{
    (AuthTokens tokens, string refreshTokenPlain) CreateTokens(
        long userId,
        long orgId,
        string email,
        IReadOnlyList<string> roles,
        IReadOnlyList<string> scopes);

    ClaimsPrincipal? ValidateAccessToken(string token);

    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
