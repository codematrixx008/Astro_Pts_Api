using Astro.Domain.Billing;
using Astro.Infrastructure.Data;
using Dapper;

namespace Astro.Infrastructure.Repositories;

public sealed class LedgerRepository : ILedgerRepository
{
    private readonly IDbConnectionFactory _db;
    public LedgerRepository(IDbConnectionFactory db) => _db = db;

    public async Task<long> CreateAsync(LedgerTransaction tx, CancellationToken ct)
    {
        const string sql = @"
INSERT INTO dbo.LedgerTransactions
(ChatSessionId, UserId, EntryType, Amount, Currency, CreatedUtc, MetaJson)
OUTPUT INSERTED.LedgerTransactionId
VALUES
(@ChatSessionId, @UserId, @EntryType, @Amount, @Currency, @CreatedUtc, @MetaJson);";
        using var conn = _db.Create();
        return await conn.ExecuteScalarAsync<long>(new CommandDefinition(sql, new
        {
            tx.ChatSessionId,
            tx.UserId,
            tx.EntryType,
            tx.Amount,
            tx.Currency,
            tx.CreatedUtc,
            tx.MetaJson
        }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<LedgerTransaction>> ListForUserAsync(long userId, int take, CancellationToken ct)
    {
        const string sql = @"
SELECT TOP (@Take)
    LedgerTransactionId, ChatSessionId, UserId, EntryType, Amount, Currency, CreatedUtc, MetaJson
FROM dbo.LedgerTransactions
WHERE UserId = @UserId
ORDER BY CreatedUtc DESC;";
        using var conn = _db.Create();
        var rows = await conn.QueryAsync<LedgerTransaction>(
            new CommandDefinition(sql, new { UserId = userId, Take = take }, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<decimal> GetBalanceAsync(long userId, string currency, CancellationToken ct)
    {
        // Balance rules:
        // - astrologer_credit adds
        // - consumer_debit subtracts
        // - payout subtracts (when payout is marked/recorded in ledger)
        const string sql = @"
SELECT ISNULL(SUM(
    CASE
        WHEN EntryType IN ('astrologer_credit') THEN Amount
        WHEN EntryType IN ('consumer_debit') THEN -Amount
        WHEN EntryType IN ('payout') THEN -Amount
        ELSE 0
    END
), 0)
FROM dbo.LedgerTransactions
WHERE UserId = @UserId AND Currency = @Currency;";
        using var conn = _db.Create();
        return await conn.ExecuteScalarAsync<decimal>(
            new CommandDefinition(sql, new { UserId = userId, Currency = currency }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<LedgerTransaction>> ListForPlatformAsync(int take, CancellationToken ct)
    {
        const string sql = @"
SELECT TOP (@Take)
    LedgerTransactionId, ChatSessionId, UserId, EntryType, Amount, Currency, CreatedUtc, MetaJson
FROM dbo.LedgerTransactions
WHERE UserId IS NULL
ORDER BY CreatedUtc DESC;";
        using var conn = _db.Create();
        var rows = await conn.QueryAsync<LedgerTransaction>(
            new CommandDefinition(sql, new { Take = take }, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<decimal> GetPlatformBalanceAsync(string currency, CancellationToken ct)
    {
        // Platform balance:
        // - platform_commission adds
        // - payout subtracts (if you choose to record payouts as platform expense later)
        const string sql = @"
SELECT ISNULL(SUM(
    CASE
        WHEN EntryType IN ('platform_commission') THEN Amount
        WHEN EntryType IN ('payout_platform_expense') THEN -Amount
        ELSE 0
    END
), 0)
FROM dbo.LedgerTransactions
WHERE UserId IS NULL AND Currency = @Currency;";
        using var conn = _db.Create();
        return await conn.ExecuteScalarAsync<decimal>(
            new CommandDefinition(sql, new { Currency = currency }, cancellationToken: ct));
    }

    public async Task<bool> HasSettlementAsync(long chatSessionId, CancellationToken ct)
    {
        const string sql = @"
SELECT CASE WHEN EXISTS(
    SELECT 1 FROM dbo.LedgerTransactions
    WHERE ChatSessionId = @ChatSessionId AND EntryType = 'consumer_debit'
) THEN 1 ELSE 0 END;";
        using var conn = _db.Create();
        var ok = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { ChatSessionId = chatSessionId }, cancellationToken: ct));
        return ok == 1;
    }
}
