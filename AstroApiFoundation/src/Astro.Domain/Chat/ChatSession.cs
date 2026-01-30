namespace Astro.Domain.Chat;

public sealed record ChatSession(
    long ChatSessionId,
    long ConsumerId,
    long AstrologerId,
    string Status,                 // requested, accepted, active, ended, cancelled
    System.DateTime RequestedUtc,
    System.DateTime? AcceptedUtc,
    System.DateTime? StartedUtc,
    System.DateTime? EndedUtc,
    decimal RatePerMinute,
    string? Topic
);
