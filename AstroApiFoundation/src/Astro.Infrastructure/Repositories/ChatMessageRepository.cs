using Astro.Domain.Chat;
using Astro.Infrastructure.Data;
using Dapper;

namespace Astro.Infrastructure.Repositories;

public sealed class ChatMessageRepository : IChatMessageRepository
{
    private readonly IDbConnectionFactory _db;
    public ChatMessageRepository(IDbConnectionFactory db) => _db = db;

    public async Task<long> CreateAsync(ChatMessage message, CancellationToken ct)
    {
        const string sql = @"
INSERT INTO dbo.ChatMessages
(ChatSessionId, SenderUserId, Message, CreatedUtc)
VALUES
(@ChatSessionId, @SenderUserId, @Message, @CreatedUtc);

SELECT CAST(SCOPE_IDENTITY() AS bigint);";
        using var conn = _db.Create();
        return await conn.ExecuteScalarAsync<long>(
            new CommandDefinition(sql, new
            {
                message.ChatSessionId,
                message.SenderUserId,
                message.Message,
                message.CreatedUtc
            }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<ChatMessage>> GetBySessionAsync(long chatSessionId, long? afterMessageId, int take, CancellationToken ct)
    {
        take = Math.Clamp(take, 1, 200);

        const string sql = @"
SELECT TOP (@Take)
    MessageId, ChatSessionId, SenderUserId, Message, CreatedUtc
FROM dbo.ChatMessages
WHERE ChatSessionId = @ChatSessionId
  AND (@AfterMessageId IS NULL OR MessageId > @AfterMessageId)
ORDER BY MessageId ASC;";
        using var conn = _db.Create();
        var rows = await conn.QueryAsync<ChatMessage>(new CommandDefinition(sql, new
        {
            Take = take,
            ChatSessionId = chatSessionId,
            AfterMessageId = afterMessageId
        }, cancellationToken: ct));
        return rows.ToList();
    }
}
