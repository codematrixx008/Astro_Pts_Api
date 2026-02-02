namespace Astro.Domain.Billing;

public sealed record LedgerTransaction(
    long LedgerTransactionId,
    long? ChatSessionId,
    long? UserId,          // NULL => platform account (commission, fees, etc.)
    string EntryType,      // consumer_debit | astrologer_credit | platform_commission | payout
    decimal Amount,        // positive number; balance rules are defined in repository
    string Currency,
    DateTime CreatedUtc,
    string? MetaJson
);
