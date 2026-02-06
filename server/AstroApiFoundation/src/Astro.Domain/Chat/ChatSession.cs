namespace Astro.Domain.Chat;

public sealed record ChatSession(
    long ChatSessionId,
    long ConsumerId,
    long AstrologerId,
    DateTime ScheduledStartUtc,
    DateTime ScheduledEndUtc,
    string Status, // requested | accepted | active | ended | canceled
    DateTime CreatedUtc,
    DateTime? AcceptedUtc,
    DateTime? StartedUtc,
    DateTime? EndedUtc,
    decimal PricePerMinuteSnapshot,
    decimal PlatformFeePctSnapshot,
    decimal AstrologerSharePctSnapshot,
    string? Notes
);
