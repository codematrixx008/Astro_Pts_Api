using Astro.Application.Auth;
using Astro.Api.Security;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Astro.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthService _auth;
    private readonly JwtTokenService _jwt; // concrete for expired-token parsing

    public AuthController(AuthService auth, IJwtTokenService jwt)
    {
        _auth = auth;
        _jwt = (JwtTokenService)jwt;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthTokens>> Register(RegisterRequest req, CancellationToken ct)
    {
        var (_, _, tokens) = await _auth.RegisterAsync(req, ct);
        return Ok(tokens);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthTokens>> Login(LoginRequest req, CancellationToken ct)
    {
        var tokens = await _auth.LoginAsync(req, ct);
        return Ok(tokens);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthTokens>> Refresh(RefreshRequest req, CancellationToken ct)
    {
        // Require (possibly expired) access token to identify the user
        var bearer = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(bearer) || !bearer.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Unauthorized(new { error = "missing_bearer_token" });

        var accessToken = bearer["Bearer ".Length..].Trim();
        var principal = _jwt.GetPrincipalFromExpiredToken(accessToken);
        if (principal is null) return Unauthorized(new { error = "invalid_bearer_token" });

        var userId = long.Parse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var orgId = long.Parse(principal.FindFirstValue("org_id")!);
        var email = principal.FindFirstValue(JwtRegisteredClaimNames.Email) ?? "";
        var role = principal.FindFirstValue(ClaimTypes.Role) ?? "Member";

        var tokens = await _auth.RefreshAsync(userId, orgId, email, role, req.RefreshToken, ct);
        return Ok(tokens);
    }

    [HttpPost("logout")]
    public ActionResult Logout()
    {
        // MVP: client-side delete stored tokens.
        // (If you want server-side invalidation, add a RefreshTokenRepository.RevokeByPlainAsync and call it here.)
        return Ok(new { ok = true });
    }
}
