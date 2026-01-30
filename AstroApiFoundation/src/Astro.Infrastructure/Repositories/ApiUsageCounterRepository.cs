using Astro.Domain.ApiUsage;
using Astro.Infrastructure.Data;
using Dapper;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Astro.Infrastructure.Repositories
{
    public sealed class ApiUsageCounterRepository : IApiUsageCounterRepository
    {
        private readonly IDbConnectionFactory _db;

        public ApiUsageCounterRepository(IDbConnectionFactory db)
            => _db = db;

        public async Task<int> IncrementDailyAsync(long apiKeyId, DateOnly dateUtc, CancellationToken ct)
        {
            const string sql = @"
MERGE dbo.ApiUsageCounters AS target
USING (SELECT @ApiKeyId AS ApiKeyId, @DateUtc AS DateUtc) AS src
ON target.ApiKeyId = src.ApiKeyId AND target.DateUtc = src.DateUtc
WHEN MATCHED THEN
    UPDATE SET RequestCount = target.RequestCount + 1
WHEN NOT MATCHED THEN
    INSERT (ApiKeyId, DateUtc, RequestCount)
    VALUES (src.ApiKeyId, src.DateUtc, 1)
OUTPUT inserted.RequestCount;
";
            using var conn = _db.Create();

            return await conn.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    sql,
                    new
                    {
                        ApiKeyId = apiKeyId,
                        DateUtc = dateUtc.ToDateTime(TimeOnly.MinValue)
                    },
                    cancellationToken: ct
                ));
        }

        public async Task<int> GetDailyCountAsync(long apiKeyId, DateOnly dateUtc, CancellationToken ct)
        {
            const string sql = @"
SELECT ISNULL(RequestCount, 0)
FROM dbo.ApiUsageCounters
WHERE ApiKeyId = @ApiKeyId AND DateUtc = @DateUtc;
";
            using var conn = _db.Create();

            return await conn.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    sql,
                    new
                    {
                        ApiKeyId = apiKeyId,
                        DateUtc = dateUtc.ToDateTime(TimeOnly.MinValue)
                    },
                    cancellationToken: ct
                ));
        }

        public async Task<IReadOnlyList<(DateOnly DateUtc, int RequestCount)>>
            GetDailyRangeAsync(long apiKeyId, DateOnly from, DateOnly to, CancellationToken ct)
        {
            const string sql = @"
SELECT DateUtc, RequestCount
FROM dbo.ApiUsageCounters
WHERE ApiKeyId = @ApiKeyId
  AND DateUtc >= @From
  AND DateUtc <= @To
ORDER BY DateUtc ASC;
";
            using var conn = _db.Create();

            var rows = await conn.QueryAsync<(DateTime DateUtc, int RequestCount)>(
                new CommandDefinition(
                    sql,
                    new
                    {
                        ApiKeyId = apiKeyId,
                        From = from.ToDateTime(TimeOnly.MinValue),
                        To = to.ToDateTime(TimeOnly.MinValue)
                    },
                    cancellationToken: ct
                ));

            return rows
                .Select(r => (DateOnly.FromDateTime(r.DateUtc), r.RequestCount))
                .ToList();
        }
    }
}
