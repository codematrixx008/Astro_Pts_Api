using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Astro.Domain.ApiUsage
{
    public interface IApiUsageCounterRepository
    {
        Task<int> IncrementDailyAsync(long apiKeyId, DateOnly dateUtc, CancellationToken ct);
        Task<int> GetDailyCountAsync(long apiKeyId, DateOnly dateUtc, CancellationToken ct);
        Task<IReadOnlyList<(DateOnly DateUtc, int RequestCount)>> GetDailyRangeAsync(long apiKeyId, DateOnly from, DateOnly to, CancellationToken ct);

    }

}
