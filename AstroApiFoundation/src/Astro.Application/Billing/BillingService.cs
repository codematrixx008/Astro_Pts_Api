using Astro.Domain.Billing;
using Astro.Domain.Chat;
using System.Text.Json;

namespace Astro.Application.Billing;

/// <summary>
/// MVP billing/ledger. No external payment gateway.
/// - When a chat ends, compute amount from session snapshots
/// - Write ledger entries: consumer_debit, astrologer_credit, platform_commission
/// Later: integrate provider (Stripe/Razorpay), plans, wallets, refunds, chargebacks.
/// </summary>
public sealed class BillingService
{
    private readonly ILedgerRepository _ledger;

    public BillingService(ILedgerRepository ledger) => _ledger = ledger;

    public BillingEstimateResponse Estimate(ChatSession session, string currency = "INR")
    {
        var minutes = Math.Max(1, (int)Math.Ceiling((session.ScheduledEndUtc - session.ScheduledStartUtc).TotalMinutes));
        var total = Math.Round(session.PricePerMinuteSnapshot * minutes, 2);

        var platformCommission = Math.Round(total * (session.PlatformFeePctSnapshot / 100m), 2);
        var astrologerEarnings = Math.Round(total - platformCommission, 2);

        return new BillingEstimateResponse(
            ChatSessionId: session.ChatSessionId,
            Currency: currency,
            TotalAmount: total,
            PlatformCommission: platformCommission,
            AstrologerEarnings: astrologerEarnings);
    }

    public async Task RecordSettlementAsync(ChatSession session, CancellationToken ct, string currency = "INR")
    {
        var est = Estimate(session, currency);
        var now = DateTime.UtcNow;

        // Consumer debit (negative balance if you don't implement pre-paid wallet yet)
        await _ledger.CreateAsync(new LedgerTransaction(
            LedgerTransactionId: 0,
            ChatSessionId: session.ChatSessionId,
            UserId: session.ConsumerId,
            EntryType: "consumer_debit",
            Amount: est.TotalAmount,
            Currency: est.Currency,
            CreatedUtc: now,
            MetaJson: JsonSerializer.Serialize(new { minutes = (session.ScheduledEndUtc - session.ScheduledStartUtc).TotalMinutes, ppm = session.PricePerMinuteSnapshot })
        ), ct);

        // Astrologer credit
        await _ledger.CreateAsync(new LedgerTransaction(
            LedgerTransactionId: 0,
            ChatSessionId: session.ChatSessionId,
            UserId: session.AstrologerId,
            EntryType: "astrologer_credit",
            Amount: est.AstrologerEarnings,
            Currency: est.Currency,
            CreatedUtc: now,
            MetaJson: JsonSerializer.Serialize(new { sharePct = session.AstrologerSharePctSnapshot })
        ), ct);

        // Platform commission (store against a synthetic platform user 0? We'll store UserId=session.AstrologerId? No.
        // MVP approach: store platform commission under UserId=0 is not possible due to FK. So we store under ConsumerId with entryType=platform_commission.
        // Later: introduce PlatformAccounts table.
        await _ledger.CreateAsync(new LedgerTransaction(
            LedgerTransactionId: 0,
            ChatSessionId: session.ChatSessionId,
            UserId: session.ConsumerId,
            EntryType: "platform_commission",
            Amount: est.PlatformCommission,
            Currency: est.Currency,
            CreatedUtc: now,
            MetaJson: JsonSerializer.Serialize(new { platformFeePct = session.PlatformFeePctSnapshot })
        ), ct);
    }
}
