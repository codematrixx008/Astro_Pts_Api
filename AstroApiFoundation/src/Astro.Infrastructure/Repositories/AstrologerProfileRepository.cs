using Astro.Domain.Marketplace;
using Astro.Infrastructure.Data;
using Dapper;

namespace Astro.Infrastructure.Repositories;

public sealed class AstrologerProfileRepository : IAstrologerProfileRepository
{
    private readonly IDbConnectionFactory _db;
    public AstrologerProfileRepository(IDbConnectionFactory db) => _db = db;

    public async Task<AstrologerProfile?> GetByIdAsync(long astrologerId, CancellationToken ct)
    {
        const string sql = @"
SELECT AstrologerId, DisplayName, Bio, ExperienceYears, LanguagesCsv, SpecializationsCsv,
       PricePerMinute, Status, CreatedUtc, VerifiedUtc
FROM dbo.AstrologerProfiles
WHERE AstrologerId = @AstrologerId;";
        using var conn = _db.Create();
        return await conn.QuerySingleOrDefaultAsync<AstrologerProfile>(
            new CommandDefinition(sql, new { AstrologerId = astrologerId }, cancellationToken: ct));
    }

    public async Task CreateAsync(AstrologerProfile profile, CancellationToken ct)
    {
        const string sql = @"
INSERT INTO dbo.AstrologerProfiles
(AstrologerId, DisplayName, Bio, ExperienceYears, LanguagesCsv, SpecializationsCsv,
 PricePerMinute, Status, CreatedUtc, VerifiedUtc)
VALUES
(@AstrologerId, @DisplayName, @Bio, @ExperienceYears, @LanguagesCsv, @SpecializationsCsv,
 @PricePerMinute, @Status, @CreatedUtc, @VerifiedUtc);";
        using var conn = _db.Create();
        await conn.ExecuteAsync(new CommandDefinition(sql, profile, cancellationToken: ct));
    }

    public async Task UpdateStatusAsync(long astrologerId, string status, DateTime? verifiedUtc, CancellationToken ct)
    {
        const string sql = @"
UPDATE dbo.AstrologerProfiles
SET Status = @Status,
    VerifiedUtc = @VerifiedUtc
WHERE AstrologerId = @AstrologerId;";
        using var conn = _db.Create();
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            AstrologerId = astrologerId,
            Status = status,
            VerifiedUtc = verifiedUtc
        }, cancellationToken: ct));
    }
}
