using Astro.Api.Security;
using Astro.Application.Auth;
using Astro.Application.Common;
using Astro.Application.Security;
using Astro.Domain.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Astro.Api.Controllers;

/// <summary>
/// Google OAuth (Authorization Code + PKCE) entrypoint.
/// Clients (web/mobile) complete the Google authorize redirect and post the returned code + code_verifier here.
/// Backend exchanges the code, validates the ID token, provisions/links the user, and returns local tokens.
/// </summary>
[ApiController]
[Route("auth/google")]
public sealed class GoogleAuthController : ControllerBase
{
    private const string Provider = "google";

    private readonly GoogleOAuthClient _google;
    private readonly GoogleIdTokenValidator _validator;

    private readonly IUserRepository _users;
    private readonly IExternalIdentityRepository _external;
    private readonly IOrganizationRepository _orgs;
    private readonly IUserOrganizationRepository _userOrgs;
    private readonly IUserRoleRepository _roles;
    private readonly IUserSessionRepository _sessions;

    private readonly IJwtTokenService _jwt;
    private readonly RefreshTokenHasher _refreshHasher;
    private readonly IClock _clock;
    private readonly AuthCookieOptions _cookie;

    public GoogleAuthController(
        GoogleOAuthClient google,
        GoogleIdTokenValidator validator,
        IUserRepository users,
        IExternalIdentityRepository external,
        IOrganizationRepository orgs,
        IUserOrganizationRepository userOrgs,
        IUserRoleRepository roles,
        IUserSessionRepository sessions,
        IJwtTokenService jwt,
        RefreshTokenHasher refreshHasher,
        IClock clock,
        IOptions<AuthCookieOptions> cookie)
    {
        _google = google;
        _validator = validator;
        _users = users;
        _external = external;
        _orgs = orgs;
        _userOrgs = userOrgs;
        _roles = roles;
        _sessions = sessions;
        _jwt = jwt;
        _refreshHasher = refreshHasher;
        _clock = clock;
        _cookie = cookie.Value;
    }

    public sealed record GoogleExchangeRequest(
        string Code,
        string CodeVerifier,
        string RedirectUri,
        string? Nonce
    );

    [HttpPost("exchange")]
    public async Task<ActionResult<AuthTokens>> Exchange([FromBody] GoogleExchangeRequest req, CancellationToken ct)
    {
        // 1) Exchange code -> google tokens
        var tokenResp = await _google.ExchangeCodeAsync(req.Code, req.CodeVerifier, req.RedirectUri, ct);

        // 2) Validate ID token
        var payload = await _validator.ValidateAsync(tokenResp.id_token, ct);

        // Optional: validate nonce if client sends it
        if (!string.IsNullOrWhiteSpace(req.Nonce) && payload.Nonce != req.Nonce)
            return Unauthorized(new { error = "google_nonce_mismatch" });

        var sub = payload.Subject;
        var email = (payload.Email ?? "").Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(sub) || string.IsNullOrWhiteSpace(email))
            return Unauthorized(new { error = "google_missing_claims" });

        // 3) Resolve local user
        long userId;
        long orgId;

        var ext = await _external.GetAsync(Provider, sub, ct);
        if (ext is not null)
        {
            userId = ext.UserId;

            // Existing user: find primary org
            var userOrgList = await _userOrgs.GetForUserAsync(userId, ct);
            orgId = userOrgList.FirstOrDefault()?.OrgId
                ?? throw new UnauthorizedAccessException("No organization assigned.");
        }
        else
        {
            // Link by email if user exists; otherwise create user + org (registration-like)
            var existingUser = await _users.GetByEmailAsync(email, ct);
            if (existingUser is null)
            {
                userId = await _users.CreateExternalAsync(email, ct);

                // Create default org name (safe max length)
                var localPart = email.Split('@')[0];
                var orgName = (localPart.Length > 64 ? localPart[..64] : localPart) + "'s Org";
                orgId = await _orgs.CreateAsync(orgName, ct);
                await _userOrgs.AddOwnerAsync(userId, orgId, ct);

                // Default onboarding role
                await _roles.EnsureUserHasRoleAsync(userId, "consumer", createdBy: userId, ct);
            }
            else
            {
                userId = existingUser.UserId;

                // Use existing org assignment
                var userOrgList = await _userOrgs.GetForUserAsync(userId, ct);
                orgId = userOrgList.FirstOrDefault()?.OrgId
                    ?? throw new UnauthorizedAccessException("No organization assigned.");

                // Ensure default role exists
                var rc = await _roles.GetRoleCodesAsync(userId, ct);
                if (rc.Count == 0)
                    await _roles.EnsureUserHasRoleAsync(userId, "consumer", createdBy: userId, ct);
            }

            // Create external identity link
            await _external.CreateAsync(userId, Provider, sub, email, ct);
        }

        // 4) Validate user is active
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null || !user.IsActive)
            return Unauthorized(new { error = "user_inactive" });

        // 5) Roles for JWT
        var roleCodes = await _roles.GetRoleCodesAsync(userId, ct);
        if (roleCodes.Count == 0)
        {
            await _roles.EnsureUserHasRoleAsync(userId, "consumer", createdBy: userId, ct);
            roleCodes = await _roles.GetRoleCodesAsync(userId, ct);
        }

        // 6) Issue local tokens + create session row (cookie refresh)
        var (tokens, refreshPlain) = _jwt.CreateTokens(userId, orgId, email, roleCodes, Array.Empty<string>());

        var now = _clock.UtcNow;
        var refreshHash = _refreshHasher.Hash(refreshPlain);

        var ua = Request.Headers.UserAgent.ToString();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        await _sessions.CreateAsync(new UserSession(
            SessionId: 0,
            UserId: userId,
            RefreshTokenHash: refreshHash,
            ExpiresUtc: tokens.RefreshTokenExpiresUtc,
            CreatedUtc: now,
            RevokedUtc: null,
            ReplacedByTokenHash: null,
            UserAgent: ua,
            IpAddress: ip
        ), ct);

        SetRefreshCookie(refreshPlain, tokens.RefreshTokenExpiresUtc);
        return Ok(tokens);
    }

    private void SetRefreshCookie(string refreshTokenPlain, DateTime refreshExpiresUtc)
    {
        var sameSite = ParseSameSite(_cookie.SameSite);
        var secure = sameSite == SameSiteMode.None;

        Response.Cookies.Append(_cookie.RefreshCookieName, refreshTokenPlain, new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = sameSite,
            Path = "/auth",
            Expires = new DateTimeOffset(refreshExpiresUtc)
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
