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
        var rows = await conn.QueryAsync<LedgerTransaction>(new CommandDefinition(sql, new { UserId = userId, Take = take }, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<decimal> GetBalanceAsync(long userId, string currency, CancellationToken ct)
    {
        // Consumer debits subtract, credits add.
        const string sql = @"
SELECT ISNULL(SUM(
    CASE
        WHEN EntryType IN ('astrologer_credit') THEN Amount
        WHEN EntryType IN ('consumer_debit') THEN -Amount
        ELSE 0
    END
), 0)
FROM dbo.LedgerTransactions
WHERE UserId = @UserId AND Currency = @Currency;";
        using var conn = _db.Create();
        return await conn.ExecuteScalarAsync<decimal>(new CommandDefinition(sql, new { UserId = userId, Currency = currency }, cancellationToken: ct));
    }
}
