namespace Astro.Domain.Billing;

public sealed record LedgerTransaction(
    long LedgerTransactionId,
    long? ChatSessionId,
    long UserId,
    string EntryType, // consumer_debit | astrologer_credit | platform_commission
    decimal Amount,
    string Currency,
    DateTime CreatedUtc,
    string? MetaJson
);
