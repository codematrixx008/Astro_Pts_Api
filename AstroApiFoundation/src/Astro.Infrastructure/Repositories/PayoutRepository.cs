using Astro.Domain.Billing;
using Astro.Infrastructure.Data;
using Dapper;

namespace Astro.Infrastructure.Repositories;

public sealed class PayoutRepository : IPayoutRepository
{
    private readonly IDbConnectionFactory _db;
    public PayoutRepository(IDbConnectionFactory db) => _db = db;

    public async Task<long> CreateAsync(Payout payout, CancellationToken ct)
    {
        const string sql = @"
INSERT INTO dbo.Payouts
(AstrologerUserId, Amount, Currency, Status, RequestedUtc, PaidUtc, MetaJson)
OUTPUT INSERTED.PayoutId
VALUES
(@AstrologerUserId, @Amount, @Currency, @Status, @RequestedUtc, NULL, @MetaJson);";
        using var conn = _db.Create();
        return await conn.ExecuteScalarAsync<long>(
            new CommandDefinition(sql, new
            {
                payout.AstrologerUserId,
                payout.Amount,
                payout.Currency,
                payout.Status,
                payout.RequestedUtc,
                payout.MetaJson
            }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<Payout>> ListForUserAsync(long astrologerUserId, int take, CancellationToken ct)
    {
        const string sql = @"
SELECT TOP (@Take)
    PayoutId, AstrologerUserId, Amount, Currency, Status, RequestedUtc, PaidUtc, MetaJson
FROM dbo.Payouts
WHERE AstrologerUserId = @AstrologerUserId
ORDER BY RequestedUtc DESC;";
        using var conn = _db.Create();
        var rows = await conn.QueryAsync<Payout>(
            new CommandDefinition(sql, new { AstrologerUserId = astrologerUserId, Take = take }, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<Payout>> ListAllAsync(string status, int take, CancellationToken ct)
    {
        const string sql = @"
SELECT TOP (@Take)
    PayoutId, AstrologerUserId, Amount, Currency, Status, RequestedUtc, PaidUtc, MetaJson
FROM dbo.Payouts
WHERE (@Status = '' OR Status = @Status)
ORDER BY RequestedUtc DESC;";
        using var conn = _db.Create();
        var rows = await conn.QueryAsync<Payout>(
            new CommandDefinition(sql, new { Status = status ?? string.Empty, Take = take }, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<bool> MarkPaidAsync(long payoutId, DateTime paidUtc, CancellationToken ct)
    {
        const string sql = @"
UPDATE dbo.Payouts
SET Status = 'paid', PaidUtc = @PaidUtc
WHERE PayoutId = @PayoutId AND Status = 'requested';";
        using var conn = _db.Create();
        var rows = await conn.ExecuteAsync(
            new CommandDefinition(sql, new { PayoutId = payoutId, PaidUtc = paidUtc }, cancellationToken: ct));
        return rows == 1;
    }

    public async Task<Payout?> GetByIdAsync(long payoutId, CancellationToken ct)
    {
        const string sql = @"
SELECT PayoutId, AstrologerUserId, Amount, Currency, Status, RequestedUtc, PaidUtc, MetaJson
FROM dbo.Payouts
WHERE PayoutId = @PayoutId;";
        using var conn = _db.Create();
        return await conn.QuerySingleOrDefaultAsync<Payout>(
            new CommandDefinition(sql, new { PayoutId = payoutId }, cancellationToken: ct));
    }
}
