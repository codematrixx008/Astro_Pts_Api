namespace Astro.Domain.Chat;

public interface IChatMessageRepository
{
    Task<long> CreateAsync(ChatMessage msg, CancellationToken ct);
    Task<IReadOnlyList<ChatMessage>> ListForSessionAsync(long chatSessionId, int take, CancellationToken ct);
}
