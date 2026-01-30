using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Astro.Domain.Chat;

public interface IChatSessionRepository
{
    Task<long> CreateRequestAsync(ChatSession session, CancellationToken ct);
    Task<ChatSession?> GetByIdAsync(long chatSessionId, CancellationToken ct);

    Task AcceptAsync(long chatSessionId, long astrologerId, System.DateTime acceptedUtc, CancellationToken ct);
    Task StartAsync(long chatSessionId, System.DateTime startedUtc, CancellationToken ct);
    Task EndAsync(long chatSessionId, System.DateTime endedUtc, CancellationToken ct);

    Task<IReadOnlyList<ChatSession>> GetForConsumerAsync(long consumerId, CancellationToken ct);
    Task<IReadOnlyList<ChatSession>> GetForAstrologerAsync(long astrologerId, CancellationToken ct);
}
