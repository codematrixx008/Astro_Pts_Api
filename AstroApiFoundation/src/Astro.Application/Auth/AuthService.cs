using Astro.Application.Common;
using Astro.Application.Security;
using Astro.Domain.Auth;

namespace Astro.Application.Auth;

public sealed class AuthService
{
    private readonly IUserRepository _users;
    private readonly IOrganizationRepository _orgs;
    private readonly IUserOrganizationRepository _userOrgs;

    private readonly IUserRoleRepository _roles;
    private readonly IUserSessionRepository _sessions;

    private readonly IJwtTokenService _jwt;
    private readonly Pbkdf2Hasher _passwordHasher;
    private readonly RefreshTokenHasher _refreshHasher;
    private readonly IClock _clock;

    public AuthService(
        IUserRepository users,
        IOrganizationRepository orgs,
        IUserOrganizationRepository userOrgs,
        IUserRoleRepository roles,
        IUserSessionRepository sessions,
        IJwtTokenService jwt,
        Pbkdf2Hasher passwordHasher,
        RefreshTokenHasher refreshHasher,
        IClock clock)
    {
        _users = users;
        _orgs = orgs;
        _userOrgs = userOrgs;
        _roles = roles;
        _sessions = sessions;
        _jwt = jwt;
        _passwordHasher = passwordHasher;
        _refreshHasher = refreshHasher;
        _clock = clock;
    }

    public async Task<(long userId, long orgId, AuthTokens tokens, string refreshTokenPlain)> RegisterAsync(
        RegisterRequest req,
        string? userAgent,
        string? ip,
        CancellationToken ct)
    {
        var email = req.Email.Trim().ToLowerInvariant();

        var existing = await _users.GetByEmailAsync(email, ct);
        if (existing is not null) throw new InvalidOperationException("User already exists.");

        var pwdHash = _passwordHasher.Hash(req.Password);
        var userId = await _users.CreateAsync(email, pwdHash, ct);

        var orgId = await _orgs.CreateAsync(req.OrganizationName.Trim(), ct);
        await _userOrgs.AddOwnerAsync(userId, orgId, ct);

        // Default onboarding role for everyone
        await _roles.EnsureUserHasRoleAsync(userId, "consumer", createdBy: userId, ct);

        var roleCodes = await _roles.GetRoleCodesAsync(userId, ct);
        var (tokens, refreshPlain) = _jwt.CreateTokens(userId, orgId, email, roleCodes, Array.Empty<string>());

        var now = _clock.UtcNow;
        var refreshHash = _refreshHasher.Hash(refreshPlain);

        await _sessions.CreateAsync(new UserSession(
            SessionId: 0,
            UserId: userId,
            RefreshTokenHash: refreshHash,
            ExpiresUtc: tokens.RefreshTokenExpiresUtc,
            CreatedUtc: now,
            RevokedUtc: null,
            ReplacedByTokenHash: null,
            UserAgent: userAgent,
            IpAddress: ip
        ), ct);

        return (userId, orgId, tokens, refreshPlain);
    }

    public async Task<(AuthTokens tokens, string refreshTokenPlain)> LoginAsync(
        LoginRequest req,
        string? userAgent,
        string? ip,
        CancellationToken ct)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        var user = await _users.GetByEmailAsync(email, ct)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!user.IsActive) throw new UnauthorizedAccessException("User inactive.");

        if (!_passwordHasher.Verify(req.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        var userOrgList = await _userOrgs.GetForUserAsync(user.UserId, ct);
        var primaryOrgId = userOrgList.FirstOrDefault()?.OrgId
            ?? throw new UnauthorizedAccessException("No organization assigned.");

        var roleCodes = await _roles.GetRoleCodesAsync(user.UserId, ct);
        if (roleCodes.Count == 0)
        {
            await _roles.EnsureUserHasRoleAsync(user.UserId, "consumer", createdBy: user.UserId, ct);
            roleCodes = await _roles.GetRoleCodesAsync(user.UserId, ct);
        }

        var (tokens, refreshPlain) = _jwt.CreateTokens(user.UserId, primaryOrgId, user.Email, roleCodes, Array.Empty<string>());

        var now = _clock.UtcNow;
        var refreshHash = _refreshHasher.Hash(refreshPlain);

        await _sessions.CreateAsync(new UserSession(
            SessionId: 0,
            UserId: user.UserId,
            RefreshTokenHash: refreshHash,
            ExpiresUtc: tokens.RefreshTokenExpiresUtc,
            CreatedUtc: now,
            RevokedUtc: null,
            ReplacedByTokenHash: null,
            UserAgent: userAgent,
            IpAddress: ip
        ), ct);

        return (tokens, refreshPlain);
    }

    public async Task<(AuthTokens tokens, string refreshTokenPlain)> RefreshAsync(
        long userId,
        long orgId,
        string email,
        string refreshTokenPlain,
        string? userAgent,
        string? ip,
        CancellationToken ct)
    {
        var now = _clock.UtcNow;

        var oldHash = _refreshHasher.Hash(refreshTokenPlain);
        var session = await _sessions.GetByRefreshTokenHashAsync(oldHash, ct)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        if (session.RevokedUtc is not null)
            throw new UnauthorizedAccessException("Refresh token revoked.");

        if (session.ExpiresUtc <= now)
            throw new UnauthorizedAccessException("Refresh token expired.");

        // Safety: ensure refresh token belongs to the same user
        if (session.UserId != userId)
            throw new UnauthorizedAccessException("Refresh token user mismatch.");

        var roleCodes = await _roles.GetRoleCodesAsync(userId, ct);
        if (roleCodes.Count == 0)
        {
            await _roles.EnsureUserHasRoleAsync(userId, "consumer", createdBy: userId, ct);
            roleCodes = await _roles.GetRoleCodesAsync(userId, ct);
        }

        var (tokens, newRefreshPlain) = _jwt.CreateTokens(userId, orgId, email, roleCodes, Array.Empty<string>());
        var newHash = _refreshHasher.Hash(newRefreshPlain);

        await _sessions.RotateAsync(session.SessionId, newHash, tokens.RefreshTokenExpiresUtc, now, ct);

        return (tokens, newRefreshPlain);
    }

    public async Task LogoutAsync(string refreshTokenPlain, CancellationToken ct)
    {
        var hash = _refreshHasher.Hash(refreshTokenPlain);
        var session = await _sessions.GetByRefreshTokenHashAsync(hash, ct);
        if (session is null) return;

        await _sessions.RevokeAsync(session.SessionId, _clock.UtcNow, ct);
    }
}
