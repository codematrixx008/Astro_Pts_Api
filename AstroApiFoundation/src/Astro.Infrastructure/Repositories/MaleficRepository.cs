using Astro.Domain.Interface;
using Astro.Domain.Models;
using Astro.Infrastructure.Data;
using Dapper;
using System.Data;

namespace Astro.Infrastructure.Repositories;

public sealed class MaleficRepository : IMaleficRepository
{
    private readonly IDbConnectionFactory _db;

    public MaleficRepository(IDbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Malefic>> GetMaleficsAsync(
        CancellationToken ct = default)
    {
        await using var connection = _db.Create();

        return await connection.QueryAsync<Malefic>(
            new CommandDefinition(
                commandText: "sp_GetMalefics",
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct
            )
        );
    }
}
