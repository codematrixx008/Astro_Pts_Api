using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Astro.Domain.Marketplace;

public interface IAstrologerAvailabilityRepository
{
    Task<IReadOnlyList<AstrologerAvailability>> GetByAstrologerAsync(long astrologerId, CancellationToken ct);
    Task AddAsync(AstrologerAvailability slot, CancellationToken ct);
    Task DisableAsync(long availabilityId, long astrologerId, CancellationToken ct);
}
