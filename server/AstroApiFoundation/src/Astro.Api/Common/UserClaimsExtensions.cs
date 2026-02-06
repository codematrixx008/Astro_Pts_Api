using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Astro.Api.Common;

public static class UserClaimsExtensions
{
    public static long RequireUserId(this ClaimsPrincipal user)
    {
        var v = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(v, out var userId))
            throw new InvalidOperationException("Missing or invalid user id claim.");
        return userId;
    }
}
