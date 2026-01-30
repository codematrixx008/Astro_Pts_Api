using Astro.Application.Common;
using Astro.Application.Security;
using Astro.Domain.Auth;

namespace Astro.Application.Auth;

public sealed class AuthService
{
    private readonly IUserRepository _users;
    private readonly IOrganizationRepository _orgs;
    private readonly IUserOrganizationRepository _userOrgs;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IJwtTokenService _jwt;
    private readonly Pbkdf2Hasher _hasher;
    private readonly IClock _clock;

    public AuthService(
        IUserRepository users,
        IOrganizationRepository orgs,
        IUserOrganizationRepository userOrgs,
        IRefreshTokenRepository refreshTokens,
        IJwtTokenService jwt,
        Pbkdf2Hasher hasher,
        IClock clock)
    {
        _users = users;
        _orgs = orgs;
        _userOrgs = userOrgs;
        _refreshTokens = refreshTokens;
        _jwt = jwt;
        _hasher = hasher;
        _clock = clock;
    }

    public async Task<(long userId, long orgId, AuthTokens tokens)> RegisterAsync(RegisterRequest req, CancellationToken ct)
    {
        var email = req.Email.Trim().ToLowerInvariant();

        var existing = await _users.GetByEmailAsync(email, ct);
        if (existing is not null) throw new InvalidOperationException("User already exists.");

        var pwdHash = _hasher.Hash(req.Password);
        var userId = await _users.CreateAsync(email, pwdHash, ct);

        var orgId = await _orgs.CreateAsync(req.OrganizationName.Trim(), ct);
        await _userOrgs.AddOwnerAsync(userId, orgId, ct);

        var roles = new[] { "Owner" };
        var scopes = Array.Empty<string>(); // JWT scopes for portal, not public API

        var tokens = _jwt.CreateTokens(userId, orgId, email, roles, scopes);

        // Store refresh token (hashed) for rotation
        var refreshHash = _hasher.Hash(tokens.RefreshToken);
        await _refreshTokens.CreateAsync(userId, refreshHash, tokens.RefreshTokenExpiresUtc, ct);

        // Return plaintext refresh to client only once
        return (userId, orgId, tokens);
    }

    public async Task<AuthTokens> LoginAsync(LoginRequest req, CancellationToken ct)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        var user = await _users.GetByEmailAsync(email, ct) ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!user.IsActive) throw new UnauthorizedAccessException("User inactive.");

        if (!_hasher.Verify(req.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        var userOrgList = await _userOrgs.GetForUserAsync(user.UserId, ct);
        var primaryOrgId = userOrgList.FirstOrDefault()?.OrgId
            ?? throw new UnauthorizedAccessException("No organization assigned.");

        var role = userOrgList.First(x => x.OrgId == primaryOrgId).Role;

        var tokens = _jwt.CreateTokens(user.UserId, primaryOrgId, user.Email, new[] { role }, Array.Empty<string>());

        // Store refresh token (hashed)
        var refreshHash = _hasher.Hash(tokens.RefreshToken);
        await _refreshTokens.CreateAsync(user.UserId, refreshHash, tokens.RefreshTokenExpiresUtc, ct);

        return tokens;
    }

    public async Task<AuthTokens> RefreshAsync(long userId, long orgId, string email, string role, string refreshTokenPlain, CancellationToken ct)
    {
        var now = _clock.UtcNow;

        // Validate provided refresh token by hashing and searching (we store salted hashes; cannot re-hash to match)
        // So: store hash AND also store a token "fingerprint" hash (HMAC) - but to keep this project simple:
        // We'll store PBKDF2 hashes and verify by scanning active tokens for the user.
        // (Ok for MVP / small token counts; switch to deterministic HMAC for scale.)
        //
        // Implementation detail: repo will return active tokens, then we verify.
        var token = await _refreshTokens.GetValidByPlainAsync(userId, refreshTokenPlain, now, ct)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        // rotate: revoke old token, create new
        var newTokens = _jwt.CreateTokens(userId, orgId, email, new[] { role }, Array.Empty<string>());

        await _refreshTokens.RevokeAsync(token.RefreshTokenId, now, replacedByTokenHash: null, ct);

        var newRefreshHash = _hasher.Hash(newTokens.RefreshToken);
        await _refreshTokens.CreateAsync(userId, newRefreshHash, newTokens.RefreshTokenExpiresUtc, ct);

        return newTokens;
    }
}
