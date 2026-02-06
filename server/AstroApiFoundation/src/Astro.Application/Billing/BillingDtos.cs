namespace Astro.Application.Billing;

public sealed record BillingEstimateResponse(
    long ChatSessionId,
    string Currency,
    decimal TotalAmount,
    decimal PlatformCommission,
    decimal AstrologerEarnings
);

public sealed record PayoutRequest(
    decimal Amount,
    string Currency,
    string? Notes
);
