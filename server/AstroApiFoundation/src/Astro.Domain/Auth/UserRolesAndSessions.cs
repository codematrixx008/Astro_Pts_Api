namespace Astro.Domain.Auth;

public interface IUserRoleRepository
{
    Task<IReadOnlyList<string>> GetRoleCodesAsync(long userId, CancellationToken ct);
    Task EnsureUserHasRoleAsync(long userId, string roleCode, long? createdBy, CancellationToken ct);
}

public sealed record UserSession(
    long SessionId,
    long UserId,
    string RefreshTokenHash,
    DateTime ExpiresUtc,
    DateTime CreatedUtc,
    DateTime? RevokedUtc,
    string? ReplacedByTokenHash,
    string? UserAgent,
    string? IpAddress
);

public interface IUserSessionRepository
{
    Task<UserSession?> GetByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken ct);
    Task<long> CreateAsync(UserSession session, CancellationToken ct);
    Task RevokeAsync(long sessionId, DateTime revokedUtc, CancellationToken ct);
    Task RotateAsync(long sessionId, string newRefreshTokenHash, DateTime newExpiryUtc, DateTime rotatedUtc, CancellationToken ct);
}
