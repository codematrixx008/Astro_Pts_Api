using Astro.Api.Common;
using Astro.Domain.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Astro.Api.Hubs;

[Authorize]
public sealed class ChatHub : Hub
{
    private readonly IChatSessionRepository _sessions;
    private readonly IChatMessageRepository _messages;

    public ChatHub(IChatSessionRepository sessions, IChatMessageRepository messages)
    {
        _sessions = sessions;
        _messages = messages;
    }

    private static string GroupName(long chatSessionId) => $"chat:{chatSessionId}";

    public async Task JoinSession(long chatSessionId)
    {
        var userId = Context.User!.RequireUserId();
        if (!await _sessions.IsParticipantAsync(chatSessionId, userId, Context.ConnectionAborted))
            throw new HubException("not_a_participant");

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(chatSessionId), Context.ConnectionAborted);
        await Clients.Caller.SendAsync("joined", new { chatSessionId }, Context.ConnectionAborted);
    }

    public async Task SendMessage(long chatSessionId, string messageText)
    {
        var userId = Context.User!.RequireUserId();
        var ct = Context.ConnectionAborted;

        if (string.IsNullOrWhiteSpace(messageText) || messageText.Length > 2000)
            throw new HubException("invalid_message");

        if (!await _sessions.IsParticipantAsync(chatSessionId, userId, ct))
            throw new HubException("not_a_participant");

        // Enforce messaging only for accepted/active sessions
        var session = await _sessions.GetByIdAsync(chatSessionId, ct);
        if (session is null) throw new HubException("session_not_found");
        if (session.Status is not ("accepted" or "active"))
            throw new HubException("session_not_active");

        var msg = new ChatMessage(
            ChatMessageId: 0,
            ChatSessionId: chatSessionId,
            SenderUserId: userId,
            MessageText: messageText.Trim(),
            CreatedUtc: DateTime.UtcNow
        );

        var id = await _messages.CreateAsync(msg, ct);

        await Clients.Group(GroupName(chatSessionId)).SendAsync("message", new
        {
            chatMessageId = id,
            chatSessionId,
            senderUserId = userId,
            messageText = msg.MessageText,
            createdUtc = msg.CreatedUtc
        }, ct);
    }
}
