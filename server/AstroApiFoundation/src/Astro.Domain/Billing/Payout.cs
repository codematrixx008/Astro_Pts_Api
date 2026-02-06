namespace Astro.Domain.Billing;

public sealed record Payout(
    long PayoutId,
    long AstrologerUserId,
    decimal Amount,
    string Currency,
    string Status,      // requested | paid | rejected
    DateTime RequestedUtc,
    DateTime? PaidUtc,
    string? MetaJson
);
