using Astro.Domain.Interface;
using Astro.Domain.Models;
using Astro.Infrastructure.Data;
using Dapper;
using System.Data;

namespace Astro.Infrastructure.Repositories;

public sealed class AvkahadaChakraRepository
    : IAvkahadaChakraRepository
{
    private readonly IDbConnectionFactory _db;

    public AvkahadaChakraRepository(IDbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<IEnumerable<AvkahadaChakra>> GetAvkahadaChakraAsync(
        CancellationToken ct = default)
    {
        await using var connection = _db.Create();

        return await connection.QueryAsync<AvkahadaChakra>(
            new CommandDefinition(
                commandText: "sp_GetAvkahadaChakra",
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct
            )
        );
    }
}


