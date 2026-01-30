namespace Astro.Domain.Chat;

public sealed record ChatMessage(
    long MessageId,
    long ChatSessionId,
    long SenderUserId,
    string Message,
    System.DateTime CreatedUtc
);
