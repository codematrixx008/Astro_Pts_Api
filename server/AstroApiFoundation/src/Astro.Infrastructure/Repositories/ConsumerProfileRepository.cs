using Astro.Domain.Consumers;
using Astro.Infrastructure.Data;
using Dapper;

namespace Astro.Infrastructure.Repositories;

public sealed class ConsumerProfileRepository : IConsumerProfileRepository
{
    private readonly IDbConnectionFactory _db;
    public ConsumerProfileRepository(IDbConnectionFactory db) => _db = db;

    public async Task<ConsumerProfile?> GetByUserIdAsync(long userId, CancellationToken ct)
    {
        const string sql = @"
SELECT UserId, FullName, Gender, Phone, MaritalStatus, Occupation, City, PreferredLanguage, UpdatedUtc
FROM dbo.ConsumerProfiles
WHERE UserId = @UserId;
";
        using var conn = _db.Create();
        return await conn.QueryFirstOrDefaultAsync<ConsumerProfile>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));
    }

    public async Task UpsertAsync(ConsumerProfile profile, CancellationToken ct)
    {
        const string sql = @"
MERGE dbo.ConsumerProfiles AS t
USING (SELECT @UserId AS UserId) AS s
ON t.UserId = s.UserId
WHEN MATCHED THEN UPDATE SET
    FullName = @FullName,
    Gender = @Gender,
    Phone = @Phone,
    MaritalStatus = @MaritalStatus,
    Occupation = @Occupation,
    City = @City,
    PreferredLanguage = @PreferredLanguage,
    UpdatedUtc = @UpdatedUtc
WHEN NOT MATCHED THEN INSERT
    (UserId, FullName, Gender, Phone, MaritalStatus, Occupation, City, PreferredLanguage, UpdatedUtc)
VALUES
    (@UserId, @FullName, @Gender, @Phone, @MaritalStatus, @Occupation, @City, @PreferredLanguage, @UpdatedUtc);
";
        using var conn = _db.Create();
        await conn.ExecuteAsync(new CommandDefinition(sql, profile, cancellationToken: ct));
    }
}
