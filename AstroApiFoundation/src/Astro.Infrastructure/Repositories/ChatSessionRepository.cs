using Astro.Domain.Chat;
using Astro.Infrastructure.Data;
using Dapper;

namespace Astro.Infrastructure.Repositories;

public sealed class ChatSessionRepository : IChatSessionRepository
{
    private readonly IDbConnectionFactory _db;
    public ChatSessionRepository(IDbConnectionFactory db) => _db = db;

    public async Task<long> CreateRequestAsync(ChatSession session, CancellationToken ct)
    {
        const string sql = @"
INSERT INTO dbo.ChatSessions
(ConsumerId, AstrologerId, Status, RequestedUtc, AcceptedUtc, StartedUtc, EndedUtc, RatePerMinute, Topic)
VALUES
(@ConsumerId, @AstrologerId, @Status, @RequestedUtc, @AcceptedUtc, @StartedUtc, @EndedUtc, @RatePerMinute, @Topic);

SELECT CAST(SCOPE_IDENTITY() AS bigint);";
        using var conn = _db.Create();
        return await conn.ExecuteScalarAsync<long>(
            new CommandDefinition(sql, new
            {
                session.ConsumerId,
                session.AstrologerId,
                session.Status,
                session.RequestedUtc,
                session.AcceptedUtc,
                session.StartedUtc,
                session.EndedUtc,
                session.RatePerMinute,
                session.Topic
            }, cancellationToken: ct));
    }

    public async Task<ChatSession?> GetByIdAsync(long chatSessionId, CancellationToken ct)
    {
        const string sql = @"
SELECT ChatSessionId, ConsumerId, AstrologerId, Status,
       RequestedUtc, AcceptedUtc, StartedUtc, EndedUtc,
       RatePerMinute, Topic
FROM dbo.ChatSessions
WHERE ChatSessionId = @ChatSessionId;";
        using var conn = _db.Create();
        return await conn.QuerySingleOrDefaultAsync<ChatSession>(
            new CommandDefinition(sql, new { ChatSessionId = chatSessionId }, cancellationToken: ct));
    }

    public async Task AcceptAsync(long chatSessionId, long astrologerId, DateTime acceptedUtc, CancellationToken ct)
    {
        // accept only if correct astrologer and status is requested
        const string sql = @"
UPDATE dbo.ChatSessions
SET Status = 'accepted', AcceptedUtc = @AcceptedUtc
WHERE ChatSessionId = @ChatSessionId
  AND AstrologerId = @AstrologerId
  AND Status = 'requested';";
        using var conn = _db.Create();
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            ChatSessionId = chatSessionId,
            AstrologerId = astrologerId,
            AcceptedUtc = acceptedUtc
        }, cancellationToken: ct));
    }

    public async Task StartAsync(long chatSessionId, DateTime startedUtc, CancellationToken ct)
    {
        const string sql = @"
UPDATE dbo.ChatSessions
SET Status = 'active', StartedUtc = @StartedUtc
WHERE ChatSessionId = @ChatSessionId
  AND Status IN ('accepted','requested');";
        using var conn = _db.Create();
        await conn.ExecuteAsync(new CommandDefinition(sql, new { ChatSessionId = chatSessionId, StartedUtc = startedUtc }, cancellationToken: ct));
    }

    public async Task EndAsync(long chatSessionId, DateTime endedUtc, CancellationToken ct)
    {
        const string sql = @"
UPDATE dbo.ChatSessions
SET Status = 'ended', EndedUtc = @EndedUtc
WHERE ChatSessionId = @ChatSessionId
  AND Status IN ('active','accepted');";
        using var conn = _db.Create();
        await conn.ExecuteAsync(new CommandDefinition(sql, new { ChatSessionId = chatSessionId, EndedUtc = endedUtc }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<ChatSession>> GetForConsumerAsync(long consumerId, CancellationToken ct)
    {
        const string sql = @"
SELECT ChatSessionId, ConsumerId, AstrologerId, Status, RequestedUtc, AcceptedUtc, StartedUtc, EndedUtc, RatePerMinute, Topic
FROM dbo.ChatSessions
WHERE ConsumerId = @ConsumerId
ORDER BY RequestedUtc DESC;";
        using var conn = _db.Create();
        var rows = await conn.QueryAsync<ChatSession>(new CommandDefinition(sql, new { ConsumerId = consumerId }, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<ChatSession>> GetForAstrologerAsync(long astrologerId, CancellationToken ct)
    {
        const string sql = @"
SELECT ChatSessionId, ConsumerId, AstrologerId, Status, RequestedUtc, AcceptedUtc, StartedUtc, EndedUtc, RatePerMinute, Topic
FROM dbo.ChatSessions
WHERE AstrologerId = @AstrologerId
ORDER BY RequestedUtc DESC;";
        using var conn = _db.Create();
        var rows = await conn.QueryAsync<ChatSession>(new CommandDefinition(sql, new { AstrologerId = astrologerId }, cancellationToken: ct));
        return rows.ToList();
    }
}
