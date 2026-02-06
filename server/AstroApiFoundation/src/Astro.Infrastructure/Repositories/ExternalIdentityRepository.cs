using Astro.Domain.Auth;
using Astro.Infrastructure.Data;
using Dapper;

namespace Astro.Infrastructure.Repositories;

public sealed class ExternalIdentityRepository : IExternalIdentityRepository
{
    private readonly IDbConnectionFactory _db;

    public ExternalIdentityRepository(IDbConnectionFactory db) => _db = db;

    public async Task<ExternalIdentity?> GetAsync(
        string provider,
        string providerUserId,
        CancellationToken ct)
    {
        const string sql = @"
SELECT TOP 1
    ExternalIdentityId,
    UserId,
    Provider,
    ProviderUserId,
    EmailSnapshot,
    CreatedUtc
FROM dbo.ExternalIdentities
WHERE Provider = @Provider
  AND ProviderUserId = @ProviderUserId;";

        await using var conn = _db.Create();
        return await conn.QueryFirstOrDefaultAsync<ExternalIdentity>(
            new CommandDefinition(
                sql,
                new { Provider = provider, ProviderUserId = providerUserId },
                cancellationToken: ct));
    }

    public async Task<long> CreateAsync(
        long userId,
        string provider,
        string providerUserId,
        string emailSnapshot,
        CancellationToken ct)
    {
        const string sql = @"
INSERT INTO dbo.ExternalIdentities
    (UserId, Provider, ProviderUserId, EmailSnapshot, CreatedUtc)
VALUES
    (@UserId, @Provider, @ProviderUserId, @EmailSnapshot, @CreatedUtc);

SELECT CAST(SCOPE_IDENTITY() AS bigint);";

        await using var conn = _db.Create();
        return await conn.ExecuteScalarAsync<long>(
            new CommandDefinition(
                sql,
                new
                {
                    UserId = userId,
                    Provider = provider,
                    ProviderUserId = providerUserId,
                    EmailSnapshot = emailSnapshot,
                    CreatedUtc = DateTime.UtcNow
                },
                cancellationToken: ct));
    }
}
