using Dapper;
using System.Data.Common;

namespace Astro.Infrastructure.Data;

public sealed class DbInitializer
{
    private readonly IDbConnectionFactory _db;

    public DbInitializer(IDbConnectionFactory db) => _db = db;

    public async Task InitializeAsync(CancellationToken ct)
    {
        await using DbConnection conn = _db.Create();
        await conn.OpenAsync(ct);

        // SQL Server - idempotent DDL
        var sqlPath = Path.Combine(AppContext.BaseDirectory, "Sql", "sqlserver_init.sql");
        if (!File.Exists(sqlPath))
            throw new FileNotFoundException("Missing SQL init script at runtime.", sqlPath);

        var sql = await File.ReadAllTextAsync(sqlPath, ct);
        await conn.ExecuteAsync(new CommandDefinition(sql, cancellationToken: ct));
    }
}
