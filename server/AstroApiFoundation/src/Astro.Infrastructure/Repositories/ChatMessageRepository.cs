using Astro.Domain.Chat;
using Astro.Infrastructure.Data;
using Dapper;

namespace Astro.Infrastructure.Repositories;

public sealed class ChatMessageRepository : IChatMessageRepository
{
    private readonly IDbConnectionFactory _db;
    public ChatMessageRepository(IDbConnectionFactory db) => _db = db;

    public async Task<long> CreateAsync(ChatMessage msg, CancellationToken ct)
    {
        const string sql = @"
INSERT INTO dbo.ChatMessages
(ChatSessionId, SenderUserId, MessageText, CreatedUtc)
OUTPUT INSERTED.ChatMessageId
VALUES
(@ChatSessionId, @SenderUserId, @MessageText, @CreatedUtc);";
        using var conn = _db.Create();
        return await conn.ExecuteScalarAsync<long>(new CommandDefinition(sql, new
        {
            msg.ChatSessionId,
            msg.SenderUserId,
            msg.MessageText,
            msg.CreatedUtc
        }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<ChatMessage>> ListForSessionAsync(long chatSessionId, int take, CancellationToken ct)
    {
        const string sql = @"
SELECT TOP (@Take)
    ChatMessageId, ChatSessionId, SenderUserId, MessageText, CreatedUtc
FROM dbo.ChatMessages
WHERE ChatSessionId = @ChatSessionId
ORDER BY CreatedUtc DESC;";
        using var conn = _db.Create();
        var rows = await conn.QueryAsync<ChatMessage>(new CommandDefinition(sql, new { ChatSessionId = chatSessionId, Take = take }, cancellationToken: ct));
        return rows.Reverse().ToList();
    }
}
