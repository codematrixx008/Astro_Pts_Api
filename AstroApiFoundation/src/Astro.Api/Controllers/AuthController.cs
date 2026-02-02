using Astro.Application.Auth;
using Astro.Api.Security;
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
    private readonly JwtTokenService _jwt; // concrete for expired-token parsing
    private readonly IUserRoleRepository _roles;
    private readonly AuthCookieOptions _cookie;

    public AuthController(AuthService auth, IJwtTokenService jwt, IUserRoleRepository roles, IOptions<AuthCookieOptions> cookie)
    {
        _auth = auth;
        _jwt = (JwtTokenService)jwt;
        _roles = roles;
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

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthTokens>> Refresh(CancellationToken ct)
    {
        if (!Request.Cookies.TryGetValue(_cookie.RefreshCookieName, out var refreshPlain) || string.IsNullOrWhiteSpace(refreshPlain))
            return Unauthorized(new { error = "missing_refresh_cookie" });

        // Require (possibly expired) access token to identify user + org
        var bearer = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(bearer) || !bearer.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Unauthorized(new { error = "missing_bearer_token" });

        var accessToken = bearer["Bearer ".Length..].Trim();
        var principal = _jwt.GetPrincipalFromExpiredToken(accessToken);
        if (principal is null)
            return Unauthorized(new { error = "invalid_bearer_token" });

        var userId = long.Parse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var orgId = long.Parse(principal.FindFirstValue("org_id")!);
        var email = principal.FindFirstValue(JwtRegisteredClaimNames.Email) ?? "";

        var ua = Request.Headers.UserAgent.ToString();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        var (tokens, newRefresh) = await _auth.RefreshAsync(userId, orgId, email, refreshPlain, ua, ip, ct);
        SetRefreshCookie(newRefresh, tokens.RefreshTokenExpiresUtc);
        return Ok(tokens);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        if (Request.Cookies.TryGetValue(_cookie.RefreshCookieName, out var refreshPlain) && !string.IsNullOrWhiteSpace(refreshPlain))
            await _auth.LogoutAsync(refreshPlain, ct);

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
