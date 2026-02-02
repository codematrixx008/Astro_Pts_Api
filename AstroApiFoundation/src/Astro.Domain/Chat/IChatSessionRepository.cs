namespace Astro.Domain.Chat;

public interface IChatSessionRepository
{
    Task<long> CreateAsync(ChatSession session, CancellationToken ct);
    Task<ChatSession?> GetByIdAsync(long chatSessionId, CancellationToken ct);
    Task<IReadOnlyList<ChatSession>> ListForUserAsync(long userId, CancellationToken ct);
    Task<bool> IsParticipantAsync(long chatSessionId, long userId, CancellationToken ct);

    Task<bool> TryAcceptAsync(long chatSessionId, long astrologerId, DateTime acceptedUtc, CancellationToken ct);
    Task<bool> TryStartAsync(long chatSessionId, long userId, DateTime startedUtc, CancellationToken ct);
    Task<bool> TryEndAsync(long chatSessionId, long userId, DateTime endedUtc, CancellationToken ct);
    Task<bool> TryCancelAsync(long chatSessionId, long userId, DateTime canceledUtc, CancellationToken ct);

    // Booking enforcement: check overlapping sessions for astrologer for active/requested/accepted sessions.
    Task<bool> HasAstrologerOverlapAsync(long astrologerId, DateTime startUtc, DateTime endUtc, CancellationToken ct);
}
