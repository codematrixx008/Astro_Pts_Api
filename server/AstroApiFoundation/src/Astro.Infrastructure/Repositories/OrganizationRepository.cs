using Astro.Domain.Auth;
using Astro.Infrastructure.Data;
using Dapper;

namespace Astro.Infrastructure.Repositories;

public sealed class OrganizationRepository : IOrganizationRepository
{
    private readonly IDbConnectionFactory _db;

    public OrganizationRepository(IDbConnectionFactory db) => _db = db;

    public async Task<long> CreateAsync(string name, CancellationToken ct)
    {
        using var conn = _db.Create();
        var sql = @"INSERT INTO Organizations(Name, CreatedUtc, IsActive)
                    VALUES(@Name, @CreatedUtc, 1);
                    SELECT CAST(SCOPE_IDENTITY() as bigint);";
        return await conn.ExecuteScalarAsync<long>(new CommandDefinition(sql, new { Name = name, CreatedUtc = DateTime.UtcNow }, cancellationToken: ct));
    }

    public async Task<Organization?> GetByIdAsync(long orgId, CancellationToken ct)
    {
        using var conn = _db.Create();
        var sql = @"SELECT TOP 1 OrgId, Name, CreatedUtc, IsActive
                    FROM Organizations
                    WHERE OrgId = @OrgId;
";
        return await conn.QueryFirstOrDefaultAsync<Organization>(new CommandDefinition(sql, new { OrgId = orgId }, cancellationToken: ct));
    }
}
