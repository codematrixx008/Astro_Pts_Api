namespace Astro.Domain.Marketplace;

public interface IAstrologerProfileRepository
{
    Task<AstrologerProfile?> GetByIdAsync(long astrologerId, CancellationToken ct);
    Task CreateAsync(AstrologerProfile profile, CancellationToken ct);
    Task UpdateStatusAsync(long astrologerId, string status, DateTime? verifiedUtc, CancellationToken ct);
    Task<IReadOnlyList<AstrologerProfile>> ListActiveAsync(string? language, string? specialization, CancellationToken ct);
}

public interface IAstrologerAvailabilityRepository
{
    Task<IReadOnlyList<AstrologerAvailability>> GetByAstrologerAsync(long astrologerId, CancellationToken ct);
    Task AddAsync(AstrologerAvailability slot, CancellationToken ct);
    Task DisableAsync(long availabilityId, long astrologerId, CancellationToken ct);
}
