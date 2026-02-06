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
SELECT AvailabilityId, AstrologerId, DayOfWeek, StartTime, EndTime, IsActive, CreatedUtc
FROM dbo.AstrologerAvailability
WHERE AstrologerId = @Id
ORDER BY DayOfWeek ASC, StartTime ASC;";

        using var conn = _db.Create();
        var rows = await conn.QueryAsync<AstrologerAvailability>(new CommandDefinition(sql, new { Id = astrologerId }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task AddAsync(AstrologerAvailability slot, CancellationToken ct)
    {
        const string sql = @"
INSERT INTO dbo.AstrologerAvailability
(AstrologerId, DayOfWeek, StartTime, EndTime, IsActive, CreatedUtc)
VALUES
(@AstrologerId, @DayOfWeek, @StartTime, @EndTime, @IsActive, @CreatedUtc);";

        using var conn = _db.Create();
        await conn.ExecuteAsync(new CommandDefinition(sql, slot, cancellationToken: ct));
    }

    public async Task DisableAsync(long availabilityId, long astrologerId, CancellationToken ct)
    {
        const string sql = @"
UPDATE dbo.AstrologerAvailability
SET IsActive = 0
WHERE AvailabilityId = @AvailabilityId AND AstrologerId = @AstrologerId;";

        using var conn = _db.Create();
        await conn.ExecuteAsync(new CommandDefinition(sql, new { AvailabilityId = availabilityId, AstrologerId = astrologerId }, cancellationToken: ct));
    }

    public async Task<bool> IsAvailableForRangeAsync(long astrologerId, DateTime startUtc, DateTime endUtc, CancellationToken ct)
    {
        // MVP rules:
        // - start/end must be on the same UTC date (no cross-midnight bookings)
        // - match by DayOfWeek (0=Sunday..6=Saturday) and TimeOfDay within an active slot
        if (startUtc.Date != endUtc.Date) return false;

        var day = (int)startUtc.DayOfWeek;
        var startTime = startUtc.TimeOfDay;
        var endTime = endUtc.TimeOfDay;

        const string sql = @"
SELECT CASE WHEN EXISTS(
    SELECT 1
    FROM dbo.AstrologerAvailability
    WHERE AstrologerId = @AstrologerId
      AND IsActive = 1
      AND DayOfWeek = @Day
      AND StartTime <= @StartTime
      AND EndTime >= @EndTime
) THEN 1 ELSE 0 END;";

        using var conn = _db.Create();
        var ok = await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, new
        {
            AstrologerId = astrologerId,
            Day = day,
            StartTime = startTime,
            EndTime = endTime
        }, cancellationToken: ct));

        return ok == 1;
    }

}
