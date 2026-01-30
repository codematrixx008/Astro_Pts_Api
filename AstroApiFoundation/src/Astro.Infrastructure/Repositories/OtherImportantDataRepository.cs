using Astro.Domain.Interface;
using Astro.Domain.Models;
using Astro.Infrastructure.Data;
using Dapper;
using System.Data;

namespace Astro.Infrastructure.Repositories;

public sealed class OtherImportantDataRepository
    : IOtherImportantDataRepository
{
    private readonly IDbConnectionFactory _db;

    public OtherImportantDataRepository(IDbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<IEnumerable<OtherImportantData>> GetOtherImportantDataAsync(
        CancellationToken ct = default)
    {
        await using var connection = _db.Create();

        return await connection.QueryAsync<OtherImportantData>(
            new CommandDefinition(
                commandText: "sp_GetOtherImportantData",
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct
            )
        );
    }
}
