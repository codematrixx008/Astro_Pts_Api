using Astro.Domain.Interface;
using Astro.Domain.Models;
using Astro.Infrastructure.Data;
using Dapper;
using System.Data;

namespace Astro.Infrastructure.Repositories;

public sealed class FavourablePointRepository : IFavourablePointRepository
{
    private readonly IDbConnectionFactory _db;

    public FavourablePointRepository(IDbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<IEnumerable<FavourablePoint>> GetFavourablePointsAsync(
        CancellationToken ct = default)
    {
        await using var connection = _db.Create();

        return await connection.QueryAsync<FavourablePoint>(
            new CommandDefinition(
                commandText: "sp_GetFavourablePoints",
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct
            )
        );
    }
}
