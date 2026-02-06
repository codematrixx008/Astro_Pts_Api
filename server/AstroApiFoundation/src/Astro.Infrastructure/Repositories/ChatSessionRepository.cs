using Astro.Domain.Chat;
using Astro.Infrastructure.Data;
using Dapper;

namespace Astro.Infrastructure.Repositories;

public sealed class ChatSessionRepository : IChatSessionRepository
{
    private readonly IDbConnectionFactory _db;
    public ChatSessionRepository(IDbConnectionFactory db) => _db = db;

    public async Task<long> CreateAsync(ChatSession session, CancellationToken ct)
    {
        const string sql = @"
INSERT INTO dbo.ChatSessions
(ConsumerId, AstrologerId, ScheduledStartUtc, ScheduledEndUtc, Status, CreatedUtc,
 PricePerMinuteSnapshot, PlatformFeePctSnapshot, AstrologerSharePctSnapshot, Notes)
OUTPUT INSERTED.ChatSessionId
VALUES
(@ConsumerId, @AstrologerId, @ScheduledStartUtc, @ScheduledEndUtc, @Status, @CreatedUtc,
 @PricePerMinuteSnapshot, @PlatformFeePctSnapshot, @AstrologerSharePctSnapshot, @Notes);";

        using var conn = _db.Create();
        return await conn.ExecuteScalarAsync<long>(new CommandDefinition(sql, new
        {
            session.ConsumerId,
            session.AstrologerId,
            session.ScheduledStartUtc,
            session.ScheduledEndUtc,
            session.Status,
            session.CreatedUtc,
            session.PricePerMinuteSnapshot,
            session.PlatformFeePctSnapshot,
            session.AstrologerSharePctSnapshot,
            session.Notes
        }, cancellationToken: ct));
    }

    public async Task<ChatSession?> GetByIdAsync(long chatSessionId, CancellationToken ct)
    {
        const string sql = @"
SELECT ChatSessionId, ConsumerId, AstrologerId, ScheduledStartUtc, ScheduledEndUtc,
       Status, CreatedUtc, AcceptedUtc, StartedUtc, EndedUtc,
       PricePerMinuteSnapshot, PlatformFeePctSnapshot, AstrologerSharePctSnapshot, Notes
FROM dbo.ChatSessions
WHERE ChatSessionId = @ChatSessionId;";
        using var conn = _db.Create();
        return await conn.QuerySingleOrDefaultAsync<ChatSession>(
            new CommandDefinition(sql, new { ChatSessionId = chatSessionId }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<ChatSession>> ListForUserAsync(long userId, CancellationToken ct)
    {
        const string sql = @"
SELECT ChatSessionId, ConsumerId, AstrologerId, ScheduledStartUtc, ScheduledEndUtc,
       Status, CreatedUtc, AcceptedUtc, StartedUtc, EndedUtc,
       PricePerMinuteSnapshot, PlatformFeePctSnapshot, AstrologerSharePctSnapshot, Notes
FROM dbo.ChatSessions
WHERE ConsumerId = @UserId OR AstrologerId = @UserId
ORDER BY CreatedUtc DESC;";
        using var conn = _db.Create();
        var rows = await conn.QueryAsync<ChatSession>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<bool> IsParticipantAsync(long chatSessionId, long userId, CancellationToken ct)
    {
        const string sql = @"
SELECT CASE WHEN EXISTS(
    SELECT 1 FROM dbo.ChatSessions
    WHERE ChatSessionId = @ChatSessionId
      AND (ConsumerId = @UserId OR AstrologerId = @UserId)
) THEN 1 ELSE 0 END;";
        using var conn = _db.Create();
        var ok = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { ChatSessionId = chatSessionId, UserId = userId }, cancellationToken: ct));
        return ok == 1;
    }

    public async Task<bool> TryAcceptAsync(long chatSessionId, long astrologerId, DateTime acceptedUtc, CancellationToken ct)
    {
        try
        {
            const string sql = @"
            UPDATE dbo.ChatSessions
            SET Status = 'accepted', 
                AcceptedUtc = @AcceptedUtc
            WHERE ChatSessionId = @ChatSessionId
              AND AstrologerId = @AstrologerId
              AND Status = 'requested';";

            using var conn = _db.Create();

            // Ensure we are passing the date as UTC explicitly
            var parameters = new
            {
                ChatSessionId = chatSessionId,
                AstrologerId = astrologerId,
                AcceptedUtc = acceptedUtc.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(acceptedUtc, DateTimeKind.Utc)
                    : acceptedUtc.ToUniversalTime()
            };

            var rows = await conn.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: ct));

            return rows == 1;
        }
        catch (Exception ex)
        {
            
            Console.WriteLine(ex.ToString());
            return false; 
        }
    }

    public async Task<bool> TryStartAsync(long chatSessionId, long userId, DateTime startedUtc, CancellationToken ct)
    {
        try
        {
            const string sql = @"
UPDATE dbo.ChatSessions
SET Status = 'active', StartedUtc = @StartedUtc
WHERE ChatSessionId = @ChatSessionId
  AND Status = 'accepted'
  AND (ConsumerId = @UserId OR AstrologerId = @UserId);";
            using var conn = _db.Create();
            var rows = await conn.ExecuteAsync(new CommandDefinition(sql, new
            {
                ChatSessionId = chatSessionId,
                UserId = userId,
                StartedUtc = startedUtc
            }, cancellationToken: ct));
            return rows == 1;
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return false;
        }
    }

    public async Task<bool> TryEndAsync(long chatSessionId, long userId, DateTime endedUtc, CancellationToken ct)
    {
        const string sql = @"
UPDATE dbo.ChatSessions
SET Status = 'ended', EndedUtc = @EndedUtc
WHERE ChatSessionId = @ChatSessionId
  AND Status IN ('active','accepted')
  AND (ConsumerId = @UserId OR AstrologerId = @UserId);";
        using var conn = _db.Create();
        var rows = await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            ChatSessionId = chatSessionId,
            UserId = userId,
            EndedUtc = endedUtc
        }, cancellationToken: ct));
        return rows == 1;
    }

    public async Task<bool> TryCancelAsync(long chatSessionId, long userId, DateTime canceledUtc, CancellationToken ct)
    {
        const string sql = @"
UPDATE dbo.ChatSessions
SET Status = 'canceled', EndedUtc = @CanceledUtc
WHERE ChatSessionId = @ChatSessionId
  AND Status IN ('requested','accepted')
  AND (ConsumerId = @UserId OR AstrologerId = @UserId);";
        using var conn = _db.Create();
        var rows = await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            ChatSessionId = chatSessionId,
            UserId = userId,
            CanceledUtc = canceledUtc
        }, cancellationToken: ct));
        return rows == 1;
    }

    public async Task<bool> HasAstrologerOverlapAsync(long astrologerId, DateTime startUtc, DateTime endUtc, CancellationToken ct)
    {
        // overlap rule: start < existingEnd AND end > existingStart
        const string sql = @"
        SELECT CASE WHEN EXISTS(
            SELECT 1
            FROM dbo.ChatSessions
            WHERE AstrologerId = @AstrologerId
              AND Status IN ('requested','accepted','active')
              AND @StartUtc < ScheduledEndUtc
              AND @EndUtc > ScheduledStartUtc
        ) THEN 1 ELSE 0 END;";
        using var conn = _db.Create();
        var ok = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new
            {
                AstrologerId = astrologerId,
                StartUtc = startUtc,
                EndUtc = endUtc
            }, cancellationToken: ct));
        return ok == 1;
    }

}
