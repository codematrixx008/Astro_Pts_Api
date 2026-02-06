using Astro.Application.Security;
using Astro.Domain.Auth;
using Astro.Infrastructure.Data;
using Dapper;

namespace Astro.Infrastructure.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IDbConnectionFactory _db;
    private readonly Pbkdf2Hasher _hasher;

    public RefreshTokenRepository(IDbConnectionFactory db, Pbkdf2Hasher hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    public async Task<long> CreateAsync(long userId, string tokenHash, DateTime expiresUtc, CancellationToken ct)
    {
        using var conn = _db.Create();
        var sql = @"INSERT INTO RefreshTokens(UserId, TokenHash, ExpiresUtc, CreatedUtc, RevokedUtc, ReplacedByTokenHash)
                    VALUES(@UserId, @TokenHash, @ExpiresUtc, @CreatedUtc, NULL, NULL);
                    SELECT CAST(SCOPE_IDENTITY() as bigint);";
        return await conn.ExecuteScalarAsync<long>(new CommandDefinition(sql, new
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresUtc = expiresUtc,
            CreatedUtc = DateTime.UtcNow
        }, cancellationToken: ct));
    }

    public async Task<RefreshToken?> GetValidByPlainAsync(long userId, string refreshTokenPlain, DateTime nowUtc, CancellationToken ct)
    {
        using var conn = _db.Create();
        // MSSQL uses TOP; LIMIT is not supported.
        var sql = @"SELECT TOP (25) RefreshTokenId, UserId, TokenHash, ExpiresUtc, CreatedUtc, RevokedUtc, ReplacedByTokenHash
                    FROM RefreshTokens
                    WHERE UserId = @UserId
                      AND RevokedUtc IS NULL
                      AND ExpiresUtc > @NowUtc
                    ORDER BY RefreshTokenId DESC;";

        var candidates = await conn.QueryAsync<RefreshToken>(new CommandDefinition(sql, new { UserId = userId, NowUtc = nowUtc }, cancellationToken: ct));
        foreach (var t in candidates)
        {
            if (_hasher.Verify(refreshTokenPlain, t.TokenHash))
                return t;
        }
        return null;
    }

    public async Task RevokeAsync(long refreshTokenId, DateTime revokedUtc, string? replacedByTokenHash, CancellationToken ct)
    {
        using var conn = _db.Create();
        var sql = @"UPDATE RefreshTokens
                    SET RevokedUtc = @RevokedUtc,
                        ReplacedByTokenHash = @ReplacedByTokenHash
                    WHERE RefreshTokenId = @RefreshTokenId;";
        await conn.ExecuteAsync(new CommandDefinition(sql, new { RefreshTokenId = refreshTokenId, RevokedUtc = revokedUtc, ReplacedByTokenHash = replacedByTokenHash }, cancellationToken: ct));
    }
}
