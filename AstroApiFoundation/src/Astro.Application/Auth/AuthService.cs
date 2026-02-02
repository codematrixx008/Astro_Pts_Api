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
    private readonly IUserRoleRepository _userRoles;     // NEW
    private readonly IJwtTokenService _jwt;
    private readonly Pbkdf2Hasher _hasher;
    private readonly IClock _clock;

    public AuthService(
        IUserRepository users,
        IOrganizationRepository orgs,
        IUserOrganizationRepository userOrgs,
        IRefreshTokenRepository refreshTokens,
        IUserRoleRepository userRoles,                 // NEW
        IJwtTokenService jwt,
        Pbkdf2Hasher hasher,
        IClock clock)
    {
        _users = users;
        _orgs = orgs;
        _userOrgs = userOrgs;
        _refreshTokens = refreshTokens;
        _userRoles = userRoles;                        // NEW
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

        // NEW: assign multi-roles
        // Everyone is a consumer

        //await _userRoles.EnsureUserHasRoleAsync(astrologerId, "astrologer", createdBy: adminUserId, ct);


        await _userRoles.EnsureUserHasRoleAsync(userId, "consumer", createdBy: userId, ct);

        // Optional: if you want org creator also to be admin at platform level:
        // await _userRoles.EnsureUserHasRoleAsync(userId, "admin", createdBy: userId, ct);

        var roles = await _userRoles.GetRoleCodesAsync(userId, ct);
        var scopes = Array.Empty<string>(); // JWT scopes for portal

        var tokens = _jwt.CreateTokens(userId, orgId, email, roles, scopes);

        var refreshHash = _hasher.Hash(tokens.RefreshToken);
        await _refreshTokens.CreateAsync(userId, refreshHash, tokens.RefreshTokenExpiresUtc, ct);

        return (userId, orgId, tokens);
    }

    public async Task<AuthTokens> LoginAsync(LoginRequest req, CancellationToken ct)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        var user = await _users.GetByEmailAsync(email, ct)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!user.IsActive) throw new UnauthorizedAccessException("User inactive.");

        if (!_hasher.Verify(req.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        var userOrgList = await _userOrgs.GetForUserAsync(user.UserId, ct);
        var primaryOrgId = userOrgList.FirstOrDefault()?.OrgId
            ?? throw new UnauthorizedAccessException("No organization assigned.");

        // NEW: roles come from UserRoles table
        var roles = await _userRoles.GetRoleCodesAsync(user.UserId, ct);

        // Safety: always ensure at least consumer role exists
        if (roles.Count == 0)
        {
            await _userRoles.EnsureUserHasRoleAsync(user.UserId, "consumer", createdBy: user.UserId, ct);
            roles = await _userRoles.GetRoleCodesAsync(user.UserId, ct);
        }

        var tokens = _jwt.CreateTokens(user.UserId, primaryOrgId, user.Email, roles, Array.Empty<string>());

        var refreshHash = _hasher.Hash(tokens.RefreshToken);
        await _refreshTokens.CreateAsync(user.UserId, refreshHash, tokens.RefreshTokenExpiresUtc, ct);

        return tokens;
    }

    // CHANGE: do NOT accept a single role string anymore.
    // Fetch roles from DB to reflect upgrades (eg admin verified astrologer)
    public async Task<AuthTokens> RefreshAsync(long userId, long orgId, string email, string refreshTokenPlain, CancellationToken ct)
    {
        var now = _clock.UtcNow;

        var token = await _refreshTokens.GetValidByPlainAsync(userId, refreshTokenPlain, now, ct)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        var roles = await _userRoles.GetRoleCodesAsync(userId, ct);
        if (roles.Count == 0)
        {
            await _userRoles.EnsureUserHasRoleAsync(userId, "consumer", createdBy: userId, ct);
            roles = await _userRoles.GetRoleCodesAsync(userId, ct);
        }

        var newTokens = _jwt.CreateTokens(userId, orgId, email, roles, Array.Empty<string>());

        await _refreshTokens.RevokeAsync(token.RefreshTokenId, now, replacedByTokenHash: null, ct);

        var newRefreshHash = _hasher.Hash(newTokens.RefreshToken);
        await _refreshTokens.CreateAsync(userId, newRefreshHash, newTokens.RefreshTokenExpiresUtc, ct);

        return newTokens;
    }
}
