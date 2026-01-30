using System.Threading;
using System.Threading.Tasks;

namespace Astro.Domain.Marketplace;

public interface IAstrologerProfileRepository
{
    Task<AstrologerProfile?> GetByIdAsync(long astrologerId, CancellationToken ct);
    Task CreateAsync(AstrologerProfile profile, CancellationToken ct);
    Task UpdateStatusAsync(long astrologerId, string status, System.DateTime? verifiedUtc, CancellationToken ct);
}
