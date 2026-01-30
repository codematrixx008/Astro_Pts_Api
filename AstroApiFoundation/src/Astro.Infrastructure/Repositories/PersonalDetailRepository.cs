using Astro.Domain.Interface;
using Astro.Domain.Models;
using Astro.Infrastructure.Data;
using Dapper;
using System.Data;

namespace Astro.Infrastructure.Repositories;

public sealed class PersonalDetailRepository : IPersonalDetailRepository
{
    private readonly IDbConnectionFactory _db;

    public PersonalDetailRepository(IDbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<IEnumerable<PersonalDetail>> GetPersonalDetailsAsync(
        CancellationToken ct = default)
    {
        await using var connection = _db.Create();

        return await connection.QueryAsync<PersonalDetail>(
            new CommandDefinition(
                commandText: "sp_GetPersonalDetails",
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct
            )
        );
    }
}
