using Astro.Domain.Auth;
using Astro.Infrastructure.Data;
using Dapper;

namespace Astro.Infrastructure.Repositories;

public sealed class UserSessionRepository : IUserSessionRepository
{
    private readonly IDbConnectionFactory _db;
    public UserSessionRepository(IDbConnectionFactory db) => _db = db;

    public async Task<UserSession?> GetByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken ct)
    {
        const string sql = @"
SELECT SessionId, UserId, RefreshTokenHash, ExpiresUtc, CreatedUtc,
       RevokedUtc, ReplacedByTokenHash, UserAgent, IpAddress
FROM dbo.UserSessions
WHERE RefreshTokenHash = @RefreshTokenHash;";

        using var conn = _db.Create();
        return await conn.QuerySingleOrDefaultAsync<UserSession>(
            new CommandDefinition(sql, new { RefreshTokenHash = refreshTokenHash }, cancellationToken: ct));
    }

    public async Task<long> CreateAsync(UserSession session, CancellationToken ct)
    {
        const string sql = @"
INSERT INTO dbo.UserSessions
(UserId, RefreshTokenHash, ExpiresUtc, CreatedUtc, RevokedUtc, ReplacedByTokenHash, UserAgent, IpAddress)
OUTPUT INSERTED.SessionId
VALUES
(@UserId, @RefreshTokenHash, @ExpiresUtc, @CreatedUtc, NULL, NULL, @UserAgent, @IpAddress);";

        using var conn = _db.Create();
        return await conn.ExecuteScalarAsync<long>(
            new CommandDefinition(sql, new
            {
                session.UserId,
                session.RefreshTokenHash,
                session.ExpiresUtc,
                session.CreatedUtc,
                session.UserAgent,
                session.IpAddress
            }, cancellationToken: ct));
    }

    public async Task RevokeAsync(long sessionId, DateTime revokedUtc, CancellationToken ct)
    {
        const string sql = @"
UPDATE dbo.UserSessions
SET RevokedUtc = @RevokedUtc
WHERE SessionId = @SessionId AND RevokedUtc IS NULL;";

        using var conn = _db.Create();
        await conn.ExecuteAsync(new CommandDefinition(sql, new { SessionId = sessionId, RevokedUtc = revokedUtc }, cancellationToken: ct));
    }

    public async Task RotateAsync(long sessionId, string newRefreshTokenHash, DateTime newExpiryUtc, DateTime rotatedUtc, CancellationToken ct)
    {
        using var conn = _db.Create();

        const string revokeSql = @"
UPDATE dbo.UserSessions
SET RevokedUtc = @RotatedUtc,
    ReplacedByTokenHash = @NewHash
WHERE SessionId = @SessionId AND RevokedUtc IS NULL;";

        await conn.ExecuteAsync(new CommandDefinition(revokeSql, new
        {
            SessionId = sessionId,
            RotatedUtc = rotatedUtc,
            NewHash = newRefreshTokenHash
        }, cancellationToken: ct));

        const string createSql = @"
INSERT INTO dbo.UserSessions
(UserId, RefreshTokenHash, ExpiresUtc, CreatedUtc, RevokedUtc, ReplacedByTokenHash, UserAgent, IpAddress)
SELECT UserId, @NewHash, @NewExp, @NowUtc, NULL, NULL, UserAgent, IpAddress
FROM dbo.UserSessions
WHERE SessionId = @OldSessionId;";

        await conn.ExecuteAsync(new CommandDefinition(createSql, new
        {
            OldSessionId = sessionId,
            NewHash = newRefreshTokenHash,
            NewExp = newExpiryUtc,
            NowUtc = rotatedUtc
        }, cancellationToken: ct));
    }
}
