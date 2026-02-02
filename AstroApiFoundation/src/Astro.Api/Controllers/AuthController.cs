using Astro.Api.Security;
using Astro.Application.Auth;
using Astro.Application.Security;
using Astro.Domain.Auth;
using Microsoft.AspNetCore.Authorization;
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
    private readonly IUserRoleRepository _userRoles; // NEW (for /me)

    public AuthController(AuthService auth, IJwtTokenService jwt, IUserRoleRepository userRoles)
    {
        _auth = auth;
        _jwt = (JwtTokenService)jwt;
        _userRoles = userRoles;
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
        var bearer = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(bearer) || !bearer.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Unauthorized(new { error = "missing_bearer_token" });

        var accessToken = bearer["Bearer ".Length..].Trim();

        // Read expired token principal (ValidateLifetime=false)
        var principal = _jwt.GetPrincipalFromExpiredToken(accessToken);
        if (principal is null)
            return Unauthorized(new { error = "invalid_bearer_token" });

        var userId = long.Parse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var orgId = long.Parse(principal.FindFirstValue("org_id")!);
        var email = principal.FindFirstValue(JwtRegisteredClaimNames.Email) ?? "";

        // ✅ NEW: roles are loaded from DB inside AuthService.RefreshAsync
        var tokens = await _auth.RefreshAsync(userId, orgId, email, req.RefreshToken, ct);
        return Ok(tokens);
    }

    [HttpPost("logout")]
    public ActionResult Logout()
    {
        // MVP: client-side delete stored tokens.
        return Ok(new { ok = true });
    }

    // ✅ NEW: "Me" endpoint for React UI
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<MeResponse>> Me(CancellationToken ct)
    {
        // from JWT
        var userId = long.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var orgId = long.Parse(User.FindFirstValue("org_id")!);
        var email = User.FindFirstValue(JwtRegisteredClaimNames.Email) ?? "";

        // roles from DB (source of truth)
        var roles = await _userRoles.GetRoleCodesAsync(userId, ct);

        return Ok(new MeResponse
        {
            UserId = userId,
            OrgId = orgId,
            Email = email,
            Roles = roles.ToArray()
        });
    }
}

public sealed class MeResponse
{
    public long UserId { get; init; }
    public long OrgId { get; init; }
    public string Email { get; init; } = "";
    public string[] Roles { get; init; } = Array.Empty<string>();
}
