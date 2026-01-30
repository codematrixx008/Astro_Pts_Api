using System.Security.Claims;

namespace Astro.Api.Common;

public static class ClaimsExt
{
    public static long RequireUserId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? user.FindFirstValue("sub")
                  ?? user.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");

        if (!long.TryParse(sub, out var id))
            throw new InvalidOperationException("Invalid user id in token.");

        return id;
    }

    public static string? Role(this ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.Role) ?? user.FindFirstValue("role");
}
