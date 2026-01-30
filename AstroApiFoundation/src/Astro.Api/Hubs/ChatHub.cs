using Astro.Api.Common;
using Astro.Domain.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Astro.Api.Hubs;

[Authorize]
public sealed class ChatHub : Hub
{
    private readonly IChatSessionRepository _sessions;

    public ChatHub(IChatSessionRepository sessions) => _sessions = sessions;

    public async Task JoinSession(long chatSessionId)
    {
        var userId = Context.User!.RequireUserId();
        var s = await _sessions.GetByIdAsync(chatSessionId, Context.ConnectionAborted);
        if (s is null) throw new HubException("session_not_found");

        if (s.ConsumerId != userId && s.AstrologerId != userId)
            throw new HubException("forbidden");

        await Groups.AddToGroupAsync(Context.ConnectionId, $"chat:{chatSessionId}");
    }

    public Task LeaveSession(long chatSessionId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat:{chatSessionId}");
}
