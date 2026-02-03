using Astro.Api.Security;
using Astro.Application.Auth;
using Astro.Application.Security;
using Astro.Domain.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Astro.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthService _auth;
    private readonly IUserRoleRepository _roles;

    // NEW: repos needed for cookie-only refresh
    private readonly IUserSessionRepository _sessions;                 // ✅ NEW
    private readonly IUserRepository _users;                           // ✅ NEW
    private readonly IUserOrganizationRepository _userOrgs;            // ✅ NEW
    private readonly RefreshTokenHasher _refreshHasher;                // ✅ NEW

    private readonly AuthCookieOptions _cookie;

    public AuthController(
        AuthService auth,
        IUserRoleRepository roles,
        IUserSessionRepository sessions,                 // ✅ NEW
        IUserRepository users,                           // ✅ NEW
        IUserOrganizationRepository userOrgs,            // ✅ NEW
        RefreshTokenHasher refreshHasher,                // ✅ NEW
        IOptions<AuthCookieOptions> cookie)
    {
        _auth = auth;
        _roles = roles;
        _sessions = sessions;
        _users = users;
        _userOrgs = userOrgs;
        _refreshHasher = refreshHasher;
        _cookie = cookie.Value;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthTokens>> Register(RegisterRequest req, CancellationToken ct)
    {
        var ua = Request.Headers.UserAgent.ToString();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        var (_, _, tokens, refreshPlain) = await _auth.RegisterAsync(req, ua, ip, ct);
        SetRefreshCookie(refreshPlain, tokens.RefreshTokenExpiresUtc);
        return Ok(tokens);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthTokens>> Login(LoginRequest req, CancellationToken ct)
    {
        var ua = Request.Headers.UserAgent.ToString();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        var (tokens, refreshPlain) = await _auth.LoginAsync(req, ua, ip, ct);
        SetRefreshCookie(refreshPlain, tokens.RefreshTokenExpiresUtc);
        return Ok(tokens);
    }

    // ✅ UPDATED: cookie-only refresh (no Bearer needed)
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthTokens>> Refresh(CancellationToken ct)
    {
        if (!Request.Cookies.TryGetValue(_cookie.RefreshCookieName, out var refreshPlain) ||
            string.IsNullOrWhiteSpace(refreshPlain))
        {
            return Unauthorized(new { error = "missing_refresh_cookie" });
        }

        try
        {
            var now = DateTime.UtcNow;

            // 1) Lookup session by refresh token hash
            var hash = _refreshHasher.Hash(refreshPlain);
            var session = await _sessions.GetByRefreshTokenHashAsync(hash, ct);

            if (session is null)
                return Unauthorized(new { error = "invalid_refresh_cookie" });

            if (session.RevokedUtc is not null)
                return Unauthorized(new { error = "refresh_revoked" });

            if (session.ExpiresUtc <= now)
                return Unauthorized(new { error = "refresh_expired" });

            // 2) Load user
            var user = await _users.GetByIdAsync(session.UserId, ct);
            if (user is null || !user.IsActive)
                return Unauthorized(new { error = "user_inactive" });

            // 3) Load primary org
            var orgList = await _userOrgs.GetForUserAsync(user.UserId, ct);
            var orgId = orgList.FirstOrDefault()?.OrgId;

            if (orgId is null)
                return Unauthorized(new { error = "no_org_assigned" });

            // 4) Rotate via AuthService (keeps your rotation logic)
            var ua = Request.Headers.UserAgent.ToString();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var (tokens, newRefresh) = await _auth.RefreshAsync(
                userId: user.UserId,
                orgId: orgId.Value,
                email: user.Email,
                refreshTokenPlain: refreshPlain,
                userAgent: ua,
                ip: ip,
                ct: ct);

            SetRefreshCookie(newRefresh, tokens.RefreshTokenExpiresUtc);
            return Ok(tokens);
        }
        catch (UnauthorizedAccessException)
        {
            // AuthService throws these for invalid tokens; map to 401 instead of 500
            return Unauthorized(new { error = "invalid_refresh_cookie" });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        if (Request.Cookies.TryGetValue(_cookie.RefreshCookieName, out var refreshPlain) &&
            !string.IsNullOrWhiteSpace(refreshPlain))
        {
            await _auth.LogoutAsync(refreshPlain, ct);
        }

        DeleteRefreshCookie();
        return Ok(new { ok = true });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var userIdStr = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                      ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!long.TryParse(userIdStr, out var userId))
            return Unauthorized(new { error = "invalid_token" });

        var orgIdStr = User.FindFirstValue("org_id") ?? "0";
        _ = long.TryParse(orgIdStr, out var orgId);

        var email = User.FindFirstValue(JwtRegisteredClaimNames.Email) ?? "";
        var roles = await _roles.GetRoleCodesAsync(userId, ct);

        return Ok(new { userId, orgId, email, roles });
    }

    private void SetRefreshCookie(string refreshTokenPlain, DateTime refreshExpiresUtc)
    {
        var sameSite = ParseSameSite(_cookie.SameSite);
        var secure = sameSite == SameSiteMode.None; // browser requires Secure for SameSite=None

        Response.Cookies.Append(_cookie.RefreshCookieName, refreshTokenPlain, new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = sameSite,
            Path = "/auth",
            Expires = new DateTimeOffset(refreshExpiresUtc)
        });
    }

    private void DeleteRefreshCookie()
    {
        var sameSite = ParseSameSite(_cookie.SameSite);
        var secure = sameSite == SameSiteMode.None;

        Response.Cookies.Delete(_cookie.RefreshCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = sameSite,
            Path = "/auth"
        });
    }

    private static SameSiteMode ParseSameSite(string? value)
        => value?.Trim().ToLowerInvariant() switch
        {
            "none" => SameSiteMode.None,
            "lax" => SameSiteMode.Lax,
            "strict" => SameSiteMode.Strict,
            _ => SameSiteMode.Lax
        };
}
