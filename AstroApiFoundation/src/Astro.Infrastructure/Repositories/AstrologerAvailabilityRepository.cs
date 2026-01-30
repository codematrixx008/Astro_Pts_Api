using Astro.Domain.Marketplace;
using Astro.Infrastructure.Data;
using Dapper;

namespace Astro.Infrastructure.Repositories;

public sealed class AstrologerAvailabilityRepository : IAstrologerAvailabilityRepository
{
    private readonly IDbConnectionFactory _db;
    public AstrologerAvailabilityRepository(IDbConnectionFactory db) => _db = db;

    public async Task<IReadOnlyList<AstrologerAvailability>> GetByAstrologerAsync(long astrologerId, CancellationToken ct)
    {
        const string sql = @"
SELECT AvailabilityId, AstrologerId, DayOfWeek,
       CAST(StartTime AS time) AS StartTime,
       CAST(EndTime AS time) AS EndTime,
       IsActive, CreatedUtc
FROM dbo.AstrologerAvailability
WHERE AstrologerId = @AstrologerId
ORDER BY DayOfWeek, StartTime;";
        using var conn = _db.Create();
        var rows = await conn.QueryAsync<AstrologerAvailability>(
            new CommandDefinition(sql, new { AstrologerId = astrologerId }, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task AddAsync(AstrologerAvailability slot, CancellationToken ct)
    {
        const string sql = @"
INSERT INTO dbo.AstrologerAvailability
(AstrologerId, DayOfWeek, StartTime, EndTime, IsActive, CreatedUtc)
VALUES
(@AstrologerId, @DayOfWeek, @StartTime, @EndTime, @IsActive, @CreatedUtc);";
        using var conn = _db.Create();
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            slot.AstrologerId,
            slot.DayOfWeek,
            StartTime = slot.StartTime,
            EndTime = slot.EndTime,
            slot.IsActive,
            slot.CreatedUtc
        }, cancellationToken: ct));
    }

    public async Task DisableAsync(long availabilityId, long astrologerId, CancellationToken ct)
    {
        const string sql = @"
UPDATE dbo.AstrologerAvailability
SET IsActive = 0
WHERE AvailabilityId = @AvailabilityId AND AstrologerId = @AstrologerId;";
        using var conn = _db.Create();
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            AvailabilityId = availabilityId,
            AstrologerId = astrologerId
        }, cancellationToken: ct));
    }
}
