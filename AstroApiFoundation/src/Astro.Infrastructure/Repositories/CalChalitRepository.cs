using Astro.Domain.Interface;
using Astro.Domain.Models;
using Astro.Infrastructure.Data;
using Dapper;
using System.Data;

public sealed class CalChalitRepository : ICalChalitRepository
{
    private readonly IDbConnectionFactory _db;

    public CalChalitRepository(IDbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<CalChalitResponse> GetCalChalitAsync(
        CancellationToken ct = default)
    {
        await using var connection = _db.Create();

        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(
                "sp_GetCalChalit",
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));

        var header = await multi.ReadFirstAsync<string>();
        var columns = await multi.ReadAsync<CalChalitColumn>();
        var rows = await multi.ReadAsync<CalChalitRow>();

        return new CalChalitResponse
        {
            MasterHeading = header,
            Columns = columns,
            Rows = rows
        };
    }
}
