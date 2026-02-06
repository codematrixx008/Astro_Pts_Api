namespace Astro.Application.Chat;

public sealed record CreateChatSessionRequest(
    long AstrologerId,
    DateTime ScheduledStartUtc,
    int DurationMinutes,
    string? Notes
);

public sealed record SendMessageRequest(string MessageText);

public sealed record ChatSessionResponse(
    long ChatSessionId,
    long ConsumerId,
    long AstrologerId,
    DateTime ScheduledStartUtc,
    DateTime ScheduledEndUtc,
    string Status,
    DateTime CreatedUtc,
    DateTime? AcceptedUtc,
    DateTime? StartedUtc,
    DateTime? EndedUtc,
    decimal PricePerMinuteSnapshot,
    decimal PlatformFeePctSnapshot,
    decimal AstrologerSharePctSnapshot,
    string? Notes
);
