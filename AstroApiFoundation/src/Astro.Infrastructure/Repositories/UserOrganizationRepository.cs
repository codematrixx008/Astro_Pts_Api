using Astro.Domain.Auth;
using Astro.Infrastructure.Data;
using Dapper;

namespace Astro.Infrastructure.Repositories;

public sealed class UserOrganizationRepository : IUserOrganizationRepository
{
    private readonly IDbConnectionFactory _db;

    public UserOrganizationRepository(IDbConnectionFactory db) => _db = db;

    public async Task AddOwnerAsync(long userId, long orgId, CancellationToken ct)
    {
        using var conn = _db.Create();
        var sql = @"INSERT INTO UserOrganizations(UserId, OrgId, Role)
                    VALUES(@UserId, @OrgId, @Role);";
        await conn.ExecuteAsync(new CommandDefinition(sql, new { UserId = userId, OrgId = orgId, Role = "Owner" }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<UserOrganization>> GetForUserAsync(long userId, CancellationToken ct)
    {
        using var conn = _db.Create();
        var sql = @"SELECT UserId, OrgId, Role
                    FROM UserOrganizations
                    WHERE UserId = @UserId;";
        var rows = await conn.QueryAsync<UserOrganization>(new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));
        return rows.AsList();
    }
}
