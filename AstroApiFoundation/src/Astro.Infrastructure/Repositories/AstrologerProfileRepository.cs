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
SELECT AstrologerId, DisplayName, Bio, ExperienceYears, LanguagesCsv, SpecializationsCsv, PricePerMinute, Status, CreatedUtc, VerifiedUtc
FROM dbo.AstrologerProfiles
WHERE AstrologerId = @Id;";

        using var conn = _db.Create();
        return await conn.QuerySingleOrDefaultAsync<AstrologerProfile>(new CommandDefinition(sql, new { Id = astrologerId }, cancellationToken: ct));
    }

    public async Task CreateAsync(AstrologerProfile profile, CancellationToken ct)
    {
        const string sql = @"
INSERT INTO dbo.AstrologerProfiles
(AstrologerId, DisplayName, Bio, ExperienceYears, LanguagesCsv, SpecializationsCsv, PricePerMinute, Status, CreatedUtc, VerifiedUtc)
VALUES
(@AstrologerId, @DisplayName, @Bio, @ExperienceYears, @LanguagesCsv, @SpecializationsCsv, @PricePerMinute, @Status, @CreatedUtc, @VerifiedUtc);";

        using var conn = _db.Create();
        await conn.ExecuteAsync(new CommandDefinition(sql, profile, cancellationToken: ct));
    }

    public async Task UpdateStatusAsync(long astrologerId, string status, DateTime? verifiedUtc, CancellationToken ct)
    {
        const string sql = @"
UPDATE dbo.AstrologerProfiles
SET Status = @Status,
    VerifiedUtc = COALESCE(@VerifiedUtc, VerifiedUtc)
WHERE AstrologerId = @Id;";

        using var conn = _db.Create();
        await conn.ExecuteAsync(new CommandDefinition(sql, new { Id = astrologerId, Status = status, VerifiedUtc = verifiedUtc }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<AstrologerProfile>> ListActiveAsync(string? language, string? specialization, CancellationToken ct)
    {
        // Simple LIKE filter on CSVs for MVP.
        const string sql = @"
SELECT AstrologerId, DisplayName, Bio, ExperienceYears, LanguagesCsv, SpecializationsCsv, PricePerMinute, Status, CreatedUtc, VerifiedUtc
FROM dbo.AstrologerProfiles
WHERE Status = 'active'
  AND (@Lang IS NULL OR LanguagesCsv LIKE '%' + @Lang + '%')
  AND (@Spec IS NULL OR SpecializationsCsv LIKE '%' + @Spec + '%')
ORDER BY AstrologerId DESC;";

        using var conn = _db.Create();
        var rows = await conn.QueryAsync<AstrologerProfile>(new CommandDefinition(sql, new { Lang = language, Spec = specialization }, cancellationToken: ct));
        return rows.AsList();
    }
}
