using Astro.Domain.Auth;
using Astro.Infrastructure.Data;
using Dapper;

namespace Astro.Infrastructure.Repositories;

public sealed class ApiKeyRepository : IApiKeyRepository
{
    private readonly IDbConnectionFactory _db;

    public ApiKeyRepository(IDbConnectionFactory db) => _db = db;

    public async Task<long> CreateAsync(long orgId, string name, string prefix, string secretHash, string scopesCsv, CancellationToken ct)
    {
        using var conn = _db.Create();
        var sql = @"INSERT INTO ApiKeys(OrgId, Name, Prefix, SecretHash, ScopesCsv, IsActive, CreatedUtc, LastUsedUtc, RevokedUtc)
                    VALUES(@OrgId, @Name, @Prefix, @SecretHash, @ScopesCsv, 1, @CreatedUtc, NULL, NULL);
                    SELECT CAST(SCOPE_IDENTITY() as bigint);";
        return await conn.ExecuteScalarAsync<long>(new CommandDefinition(sql, new
        {
            OrgId = orgId,
            Name = name,
            Prefix = prefix,
            SecretHash = secretHash,
            ScopesCsv = scopesCsv,
            CreatedUtc = DateTime.UtcNow
        }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<ApiKey>> ListAsync(long orgId, CancellationToken ct)
    {
        using var conn = _db.Create();
        var sql = @"SELECT ApiKeyId, OrgId, Name, Prefix, SecretHash, ScopesCsv, IsActive, CreatedUtc, LastUsedUtc, RevokedUtc
                    FROM ApiKeys
                    WHERE OrgId = @OrgId
                    ORDER BY ApiKeyId DESC;";
        var rows = await conn.QueryAsync<ApiKey>(new CommandDefinition(sql, new { OrgId = orgId }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<ApiKey?> GetActiveByPrefixAsync(string prefix, CancellationToken ct)
    {
        using var conn = _db.Create();
        var sql = @"SELECT ApiKeyId, OrgId, Name, Prefix, SecretHash, ScopesCsv, IsActive, CreatedUtc, LastUsedUtc, RevokedUtc
                    FROM ApiKeys
                    WHERE Prefix = @Prefix
                      AND IsActive = 1
                      AND RevokedUtc IS NULL;
";
        return await conn.QueryFirstOrDefaultAsync<ApiKey>(new CommandDefinition(sql, new { Prefix = prefix }, cancellationToken: ct));
    }

    public async Task RevokeAsync(long apiKeyId, DateTime revokedUtc, CancellationToken ct)
    {
        using var conn = _db.Create();
        var sql = @"UPDATE ApiKeys
                    SET IsActive = 0,
                        RevokedUtc = @RevokedUtc
                    WHERE ApiKeyId = @ApiKeyId;";
        await conn.ExecuteAsync(new CommandDefinition(sql, new { ApiKeyId = apiKeyId, RevokedUtc = revokedUtc }, cancellationToken: ct));
    }

    public async Task TouchLastUsedAsync(long apiKeyId, DateTime lastUsedUtc, CancellationToken ct)
    {
        using var conn = _db.Create();
        var sql = @"UPDATE ApiKeys
                    SET LastUsedUtc = @LastUsedUtc
                    WHERE ApiKeyId = @ApiKeyId;";
        await conn.ExecuteAsync(new CommandDefinition(sql, new { ApiKeyId = apiKeyId, LastUsedUtc = lastUsedUtc }, cancellationToken: ct));
    }
}
