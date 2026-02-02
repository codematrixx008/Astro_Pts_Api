using Astro.Domain.Auth;
using Astro.Infrastructure.Data;
using Dapper;

namespace Astro.Infrastructure.Repositories;

public sealed class UserRoleRepository : IUserRoleRepository
{
    private readonly IDbConnectionFactory _db;
    public UserRoleRepository(IDbConnectionFactory db) => _db = db;

    public async Task<IReadOnlyList<string>> GetRoleCodesAsync(long userId, CancellationToken ct)
    {
        const string sql = @"
SELECT r.Code
FROM dbo.UserRoles ur
JOIN dbo.Roles r ON r.RoleId = ur.RoleId
WHERE ur.UserId = @UserId AND r.IsActive = 1
ORDER BY r.Code;";

        using var conn = _db.Create();
        var rows = await conn.QueryAsync<string>(new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task EnsureUserHasRoleAsync(long userId, string roleCode, long? createdBy, CancellationToken ct)
    {
        const string sql = @"
DECLARE @RoleId INT = (SELECT TOP 1 RoleId FROM dbo.Roles WHERE Code = @Code AND IsActive = 1);
IF @RoleId IS NULL
    THROW 50000, 'Role code not found', 1;

IF NOT EXISTS (SELECT 1 FROM dbo.UserRoles WHERE UserId = @UserId AND RoleId = @RoleId)
BEGIN
    INSERT INTO dbo.UserRoles(UserId, RoleId, CreatedBy)
    VALUES (@UserId, @RoleId, @CreatedBy);
END";

        using var conn = _db.Create();
        await conn.ExecuteAsync(new CommandDefinition(sql, new { UserId = userId, Code = roleCode, CreatedBy = createdBy }, cancellationToken: ct));
    }
}
