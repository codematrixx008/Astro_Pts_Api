using Microsoft.Data.SqlClient;
using System.Data.Common;

namespace Astro.Infrastructure.Data;

public interface IDbConnectionFactory
{
    DbConnection Create();
}

public sealed class DbConnectionFactory : IDbConnectionFactory
{
    private readonly DbOptions _opts;

    public DbConnectionFactory(DbOptions opts) => _opts = opts;

    public DbConnection Create()
    {
        // SQL Server (MSSQL)
        return new SqlConnection(_opts.ConnectionString);
    }
}
