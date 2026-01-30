using System.Security.Claims;

namespace Astro.Application.Auth;

public interface IJwtTokenService
{
    AuthTokens CreateTokens(long userId, long orgId, string email, IReadOnlyList<string> roles, IReadOnlyList<string> scopes);
    ClaimsPrincipal? ValidateAccessToken(string token);
}
