namespace Astro.Domain.Billing;

public interface ILedgerRepository
{
    Task<long> CreateAsync(LedgerTransaction tx, CancellationToken ct);
    Task<IReadOnlyList<LedgerTransaction>> ListForUserAsync(long userId, int take, CancellationToken ct);
    Task<decimal> GetBalanceAsync(long userId, string currency, CancellationToken ct);
}
