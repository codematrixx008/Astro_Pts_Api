namespace Astro.Domain.Billing;

public interface ILedgerRepository
{
    Task<long> CreateAsync(LedgerTransaction tx, CancellationToken ct);

    // User view
    Task<IReadOnlyList<LedgerTransaction>> ListForUserAsync(long userId, int take, CancellationToken ct);
    Task<decimal> GetBalanceAsync(long userId, string currency, CancellationToken ct);

    // Platform view (commission ledger)
    Task<IReadOnlyList<LedgerTransaction>> ListForPlatformAsync(int take, CancellationToken ct);
    Task<decimal> GetPlatformBalanceAsync(string currency, CancellationToken ct);

    // Settlement safety
    Task<bool> HasSettlementAsync(long chatSessionId, CancellationToken ct);
}
