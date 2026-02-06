using Astro.Domain.Auth;
using Astro.Infrastructure.Data;
using Dapper;

namespace Astro.Infrastructure.Repositories;

public sealed class ApiUsageLogRepository : IApiUsageLogRepository
{
    private readonly IDbConnectionFactory _db;

    public ApiUsageLogRepository(IDbConnectionFactory db) => _db = db;

    public async Task CreateAsync(ApiUsageLog log, CancellationToken ct)
    {
        using var conn = _db.Create();
        var sql = @"INSERT INTO ApiUsageLogs(ApiKeyId, UserId, Method, Path, StatusCode, DurationMs, Ip, CreatedUtc)
                    VALUES(@ApiKeyId, @UserId, @Method, @Path, @StatusCode, @DurationMs, @Ip, @CreatedUtc);";
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            log.ApiKeyId,
            log.UserId,
            log.Method,
            log.Path,
            log.StatusCode,
            log.DurationMs,
            log.Ip,
            log.CreatedUtc
        }, cancellationToken: ct));
    }
}
