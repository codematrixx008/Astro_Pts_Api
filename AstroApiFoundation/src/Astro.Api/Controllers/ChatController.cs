using Astro.Api.Common;
using Astro.Api.Hubs;
using Astro.Application.Billing;
using Astro.Application.Chat;
using Astro.Domain.Billing;
using Astro.Domain.Chat;
using Astro.Domain.Marketplace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Astro.Api.Controllers;

[ApiController]
[Route("chat")]
public sealed class ChatController : ControllerBase
{
    private readonly IChatSessionRepository _sessions;
    private readonly IChatMessageRepository _messages;
    private readonly IAstrologerProfileRepository _profiles;
    private readonly BillingService _billing;
    private readonly IHubContext<ChatHub> _hub;

    public ChatController(
        IChatSessionRepository sessions,
        IChatMessageRepository messages,
        IAstrologerProfileRepository profiles,
        BillingService billing,
        IHubContext<ChatHub> hub)
    {
        _sessions = sessions;
        _messages = messages;
        _profiles = profiles;
        _billing = billing;
        _hub = hub;
    }

    // Consumer requests a chat with an astrologer at a given UTC start time.
    [HttpPost("sessions")]
    [Authorize(Roles = "consumer")]
    public async Task<IActionResult> CreateSession([FromBody] CreateChatSessionRequest req, CancellationToken ct)
    {
        var consumerId = User.RequireUserId();

        if (req.AstrologerId <= 0) return BadRequest(new { error = "invalid_astrologer" });
        if (req.DurationMinutes is < 5 or > 240) return BadRequest(new { error = "invalid_duration" });

        // UTC rules
        if (req.ScheduledStartUtc.Kind != DateTimeKind.Utc)
            return BadRequest(new { error = "start_must_be_utc" });

        var startUtc = req.ScheduledStartUtc;
        var endUtc = startUtc.AddMinutes(req.DurationMinutes);

        // Require astrologer profile verified/active
        var profile = await _profiles.GetByIdAsync(req.AstrologerId, ct);
        if (profile is null) return NotFound(new { error = "astrologer_not_found" });
        if (profile.Status is not ("verified" or "active"))
            return BadRequest(new { error = "astrologer_not_verified" });

        // Snapshots (later: plan-based)
        const decimal platformFeePct = 30m;
        const decimal astrologerSharePct = 70m;

        var session = new ChatSession(
            ChatSessionId: 0,
            ConsumerId: consumerId,
            AstrologerId: req.AstrologerId,
            ScheduledStartUtc: startUtc,
            ScheduledEndUtc: endUtc,
            Status: "requested",
            CreatedUtc: DateTime.UtcNow,
            AcceptedUtc: null,
            StartedUtc: null,
            EndedUtc: null,
            PricePerMinuteSnapshot: profile.PricePerMinute,
            PlatformFeePctSnapshot: platformFeePct,
            AstrologerSharePctSnapshot: astrologerSharePct,
            Notes: req.Notes
        );

        var id = await _sessions.CreateAsync(session, ct);
        return Ok(new { chatSessionId = id, status = "requested" });
    }

    [HttpGet("sessions/me")]
    [Authorize]
    public async Task<IActionResult> MySessions(CancellationToken ct)
    {
        var userId = User.RequireUserId();
        var rows = await _sessions.ListForUserAsync(userId, ct);
        return Ok(rows);
    }

    [HttpGet("sessions/{chatSessionId:long}")]
    [Authorize]
    public async Task<IActionResult> GetSession(long chatSessionId, CancellationToken ct)
    {
        var userId = User.RequireUserId();
        if (!await _sessions.IsParticipantAsync(chatSessionId, userId, ct))
            return Forbid();

        var session = await _sessions.GetByIdAsync(chatSessionId, ct);
        return session is null ? NotFound() : Ok(session);
    }

    [HttpPost("sessions/{chatSessionId:long}/accept")]
    [Authorize(Roles = "astrologer")]
    public async Task<IActionResult> Accept(long chatSessionId, CancellationToken ct)
    {
        var astrologerId = User.RequireUserId();
        var ok = await _sessions.TryAcceptAsync(chatSessionId, astrologerId, DateTime.UtcNow, ct);
        return ok ? Ok(new { ok = true, status = "accepted" }) : Conflict(new { error = "cannot_accept" });
    }

    [HttpPost("sessions/{chatSessionId:long}/start")]
    [Authorize]
    public async Task<IActionResult> Start(long chatSessionId, CancellationToken ct)
    {
        var userId = User.RequireUserId();
        var ok = await _sessions.TryStartAsync(chatSessionId, userId, DateTime.UtcNow, ct);
        return ok ? Ok(new { ok = true, status = "active" }) : Conflict(new { error = "cannot_start" });
    }

    [HttpPost("sessions/{chatSessionId:long}/end")]
    [Authorize]
    public async Task<IActionResult> End(long chatSessionId, CancellationToken ct)
    {
        var userId = User.RequireUserId();
        var ok = await _sessions.TryEndAsync(chatSessionId, userId, DateTime.UtcNow, ct);
        if (!ok) return Conflict(new { error = "cannot_end" });

        // Settlement
        var session = await _sessions.GetByIdAsync(chatSessionId, ct);
        if (session is not null)
            await _billing.RecordSettlementAsync(session, ct);

        return Ok(new { ok = true, status = "ended" });
    }

    [HttpPost("sessions/{chatSessionId:long}/cancel")]
    [Authorize]
    public async Task<IActionResult> Cancel(long chatSessionId, CancellationToken ct)
    {
        var userId = User.RequireUserId();
        var ok = await _sessions.TryCancelAsync(chatSessionId, userId, DateTime.UtcNow, ct);
        return ok ? Ok(new { ok = true, status = "canceled" }) : Conflict(new { error = "cannot_cancel" });
    }

    [HttpGet("sessions/{chatSessionId:long}/messages")]
    [Authorize]
    public async Task<IActionResult> ListMessages(long chatSessionId, [FromQuery] int take = 100, CancellationToken ct = default)
    {
        var userId = User.RequireUserId();
        if (!await _sessions.IsParticipantAsync(chatSessionId, userId, ct))
            return Forbid();

        take = Math.Clamp(take, 1, 500);
        var rows = await _messages.ListForSessionAsync(chatSessionId, take, ct);
        return Ok(rows);
    }

    // Optional REST message send (SignalR is preferred)
    [HttpPost("sessions/{chatSessionId:long}/messages")]
    [Authorize]
    public async Task<IActionResult> SendMessage(long chatSessionId, [FromBody] SendMessageRequest req, CancellationToken ct)
    {
        var userId = User.RequireUserId();
        if (!await _sessions.IsParticipantAsync(chatSessionId, userId, ct))
            return Forbid();

        if (string.IsNullOrWhiteSpace(req.MessageText) || req.MessageText.Length > 2000)
            return BadRequest(new { error = "invalid_message" });

        var session = await _sessions.GetByIdAsync(chatSessionId, ct);
        if (session is null) return NotFound();
        if (session.Status is not ("accepted" or "active"))
            return BadRequest(new { error = "session_not_active" });

        var msg = new ChatMessage(0, chatSessionId, userId, req.MessageText.Trim(), DateTime.UtcNow);
        var id = await _messages.CreateAsync(msg, ct);

        await _hub.Clients.Group($"chat:{chatSessionId}").SendAsync("message", new
        {
            chatMessageId = id,
            chatSessionId,
            senderUserId = userId,
            messageText = msg.MessageText,
            createdUtc = msg.CreatedUtc
        }, ct);

        return Ok(new { ok = true, chatMessageId = id });
    }
}