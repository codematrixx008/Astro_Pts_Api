using Astro.Domain.Auth;
using Astro.Infrastructure.Data;
using Dapper;

namespace Astro.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _db;

    public UserRepository(IDbConnectionFactory db) => _db = db;

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct)
    {
        await using var conn = _db.Create();
        var sql = @"SELECT TOP 1 UserId, Email, PasswordHash, CreatedUtc, IsActive
                    FROM Users
                    WHERE Email = @Email;";
        return await conn.QueryFirstOrDefaultAsync<User>(new CommandDefinition(sql, new { Email = email }, cancellationToken: ct));
    }

    public async Task<User?> GetByIdAsync(long userId, CancellationToken ct)
    {
        await using var conn = _db.Create();
        var sql = @"SELECT TOP 1 UserId, Email, PasswordHash, CreatedUtc, IsActive
                    FROM Users
                    WHERE UserId = @UserId;";
        return await conn.QueryFirstOrDefaultAsync<User>(new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));
    }

    public async Task<long> CreateAsync(string email, string passwordHash, CancellationToken ct)
    {
        await using var conn = _db.Create();
        var sql = @"INSERT INTO Users(Email, PasswordHash, CreatedUtc, IsActive)
                    VALUES(@Email, @PasswordHash, @CreatedUtc, 1);
                    SELECT CAST(SCOPE_IDENTITY() as bigint);";
        return await conn.ExecuteScalarAsync<long>(new CommandDefinition(sql, new { Email = email, PasswordHash = passwordHash, CreatedUtc = DateTime.UtcNow }, cancellationToken: ct));
    }
}
