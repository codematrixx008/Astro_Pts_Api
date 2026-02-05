namespace Astro.Domain.Chat;

public sealed record ChatMessage(
    long ChatMessageId,
    long ChatSessionId,
    long SenderUserId,
    string MessageText,
    DateTime CreatedUtc
);
