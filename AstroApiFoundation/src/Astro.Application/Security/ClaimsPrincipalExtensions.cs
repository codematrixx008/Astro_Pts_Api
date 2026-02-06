using System.Security.Claims;

namespace Astro.Application.Security
{
    public static class ClaimsPrincipalExtensions
    {
        public static int GetUserId(this ClaimsPrincipal user)
        {
            var id =
                user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                user.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(id))
                throw new UnauthorizedAccessException("UserId claim missing");

            return int.Parse(id);
        }
    }
}
