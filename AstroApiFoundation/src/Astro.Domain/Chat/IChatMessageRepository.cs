using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Astro.Domain.Chat;

public interface IChatMessageRepository
{
    Task<long> CreateAsync(ChatMessage message, CancellationToken ct);
    Task<IReadOnlyList<ChatMessage>> GetBySessionAsync(long chatSessionId, long? afterMessageId, int take, CancellationToken ct);
}
