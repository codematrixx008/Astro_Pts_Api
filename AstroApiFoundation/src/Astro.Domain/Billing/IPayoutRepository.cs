namespace Astro.Domain.Billing;

public interface IPayoutRepository
{
    Task<long> CreateAsync(Payout payout, CancellationToken ct);
    Task<IReadOnlyList<Payout>> ListForUserAsync(long astrologerUserId, int take, CancellationToken ct);
    Task<IReadOnlyList<Payout>> ListAllAsync(string status, int take, CancellationToken ct);
    Task<bool> MarkPaidAsync(long payoutId, DateTime paidUtc, CancellationToken ct);
    Task<Payout?> GetByIdAsync(long payoutId, CancellationToken ct);
}
