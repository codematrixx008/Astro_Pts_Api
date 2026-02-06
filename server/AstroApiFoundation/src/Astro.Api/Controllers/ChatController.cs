using Astro.Api.Common;
using Astro.Api.Hubs;
using Astro.Application.Billing;
using Astro.Application.Chat;
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
    private readonly IAstrologerAvailabilityRepository _availability;
    private readonly BillingService _billing;
    private readonly IHubContext<ChatHub> _hub;

    // MVP pricing knobs (upgrade to per-plan/per-org later)
    private const decimal DefaultPlatformFeePct = 20m;

    public ChatController(
        IChatSessionRepository sessions,
        IChatMessageRepository messages,
        IAstrologerProfileRepository profiles,
        IAstrologerAvailabilityRepository availability,
        BillingService billing,
        IHubContext<ChatHub> hub)
    {
        _sessions = sessions;
        _messages = messages;
        _profiles = profiles;
        _availability = availability;
        _billing = billing;
        _hub = hub;
    }

    [HttpPost("sessions")]
    [Authorize(Roles = "consumer")]
    public async Task<IActionResult> CreateSession([FromBody] CreateChatSessionRequest req, CancellationToken ct)
    {
        var consumerId = User.RequireUserId();

        if (req.AstrologerId <= 0) return BadRequest(new { error = "invalid_astrologer" });
        if (req.DurationMinutes is < 5 or > 240) return BadRequest(new { error = "invalid_duration" });

        // Enforce UTC & basic range
        if (req.ScheduledStartUtc.Kind != DateTimeKind.Utc)
            return BadRequest(new { error = "scheduled_start_must_be_utc" });

        var startUtc = req.ScheduledStartUtc;
        var endUtc = startUtc.AddMinutes(req.DurationMinutes);

        // MVP: don't allow cross-midnight bookings (keeps availability model simple)
        if (startUtc.Date != endUtc.Date)
            return BadRequest(new { error = "cross_midnight_not_supported" });

        // Astrologer must be verified/active
        var profile = await _profiles.GetByIdAsync(req.AstrologerId, ct);
        if (profile is null) return NotFound(new { error = "astrologer_not_found" });
        if (profile.Status is not ("verified" or "active"))
            return BadRequest(new { error = "astrologer_not_bookable", status = profile.Status });

        // Booking enforcement #1: requested slot must fit availability
        var okAvail = await _availability.IsAvailableForRangeAsync(req.AstrologerId, startUtc, endUtc, ct);
        if (!okAvail) return Conflict(new { error = "outside_availability" });

        // Booking enforcement #2: prevent overlapping bookings
        var hasOverlap = await _sessions.HasAstrologerOverlapAsync(req.AstrologerId, startUtc, endUtc, ct);
        if (hasOverlap) return Conflict(new { error = "slot_already_booked" });

        var platformFee = DefaultPlatformFeePct;
        var astrologerShare = 100m - platformFee;

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
            PlatformFeePctSnapshot: platformFee,
            AstrologerSharePctSnapshot: astrologerShare,
            Notes: req.Notes
        );

        var id = await _sessions.CreateAsync(session, ct);

        return Ok(new { chatSessionId = id, status = "requested" });
    }

    [HttpGet("sessions/me")]
    [Authorize]
    public async Task<IActionResult> ListMySessions(CancellationToken ct)
    {
        var userId = User.RequireUserId();
        var rows = await _sessions.ListForUserAsync(userId, ct);
        return Ok(rows);
    }

    [HttpGet("sessions/{id:long}")]
    [Authorize]
    public async Task<IActionResult> GetSession(long id, CancellationToken ct)
    {
        var userId = User.RequireUserId();
        if (!await _sessions.IsParticipantAsync(id, userId, ct))
            return Forbid();

        var session = await _sessions.GetByIdAsync(id, ct);
        if (session is null) return NotFound();
        return Ok(session);
    }

    [HttpPost("sessions/{id:long}/accept")]
    [Authorize(Roles = "astrologer")]
    public async Task<IActionResult> Accept(long id, CancellationToken ct)
    {
        var astrologerId = User.RequireUserId();

        var session = await _sessions.GetByIdAsync(id, ct);
        if (session is null) return NotFound();

        // Extra safety: re-check overlap at accept time
        var hasOverlap = await _sessions.HasAstrologerOverlapAsync(astrologerId, session.ScheduledStartUtc, session.ScheduledEndUtc, ct);
        if (!hasOverlap) return Conflict(new { error = "slot_already_booked" });

        var ok = await _sessions.TryAcceptAsync(id, astrologerId, DateTime.UtcNow, ct);
        if (!ok) return Conflict(new { error = "cannot_accept" });

        await _hub.Clients.Group($"chat:{id}").SendAsync("session", new { chatSessionId = id, status = "accepted" }, ct);
        return Ok(new { ok = true, status = "accepted" });
    }

    [HttpPost("sessions/{id:long}/start")]
    [Authorize]
    public async Task<IActionResult> Start(long id, CancellationToken ct)
    {
        var userId = User.RequireUserId();

        if (!await _sessions.IsParticipantAsync(id, userId, ct))
            return Forbid();

        var ok = await _sessions.TryStartAsync(id, userId, DateTime.UtcNow, ct);
        if (!ok) return Conflict(new { error = "cannot_start" });

        await _hub.Clients.Group($"chat:{id}").SendAsync("session", new { chatSessionId = id, status = "active" }, ct);
        return Ok(new { ok = true, status = "active" });
    }

    [HttpPost("sessions/{id:long}/end")]
    [Authorize]
    public async Task<IActionResult> End(long id, CancellationToken ct)
    {
        var userId = User.RequireUserId();
        if (!await _sessions.IsParticipantAsync(id, userId, ct))
            return Forbid();

        var ok = await _sessions.TryEndAsync(id, userId, DateTime.UtcNow, ct);
        if (!ok) return Conflict(new { error = "cannot_end" });

        var session = await _sessions.GetByIdAsync(id, ct);
        if (session is null) return NotFound();

        // Settlement is idempotent at service/repo level via unique constraint (added in upgraded billing schema)
        await _billing.RecordSettlementAsync(session, ct);

        await _hub.Clients.Group($"chat:{id}").SendAsync("session", new { chatSessionId = id, status = "ended" }, ct);
        return Ok(new { ok = true, status = "ended" });
    }

    [HttpPost("sessions/{id:long}/cancel")]
    [Authorize]
    public async Task<IActionResult> Cancel(long id, CancellationToken ct)
    {
        var userId = User.RequireUserId();
        if (!await _sessions.IsParticipantAsync(id, userId, ct))
            return Forbid();

        var ok = await _sessions.TryCancelAsync(id, userId, DateTime.UtcNow, ct);
        if (!ok) return Conflict(new { error = "cannot_cancel" });

        await _hub.Clients.Group($"chat:{id}").SendAsync("session", new { chatSessionId = id, status = "canceled" }, ct);
        return Ok(new { ok = true, status = "canceled" });
    }

    [HttpGet("sessions/{id:long}/messages")]
    [Authorize]
    public async Task<IActionResult> GetMessages(long id, [FromQuery] int take = 100, CancellationToken ct = default)
    {
        var userId = User.RequireUserId();
        if (!await _sessions.IsParticipantAsync(id, userId, ct))
            return Forbid();

        take = Math.Clamp(take, 1, 500);
        var rows = await _messages.ListForSessionAsync(id, take, ct);
        return Ok(rows.OrderBy(x => x.CreatedUtc));
    }

    [HttpPost("sessions/{id:long}/messages")]
    [Authorize]
    public async Task<IActionResult> SendMessage(long id, [FromBody] SendMessageRequest req, CancellationToken ct)
    {
        var userId = User.RequireUserId();
        if (!await _sessions.IsParticipantAsync(id, userId, ct))
            return Forbid();

        var text = (req.MessageText ?? string.Empty).Trim();
        if (text.Length == 0 || text.Length > 2000)
            return BadRequest(new { error = "invalid_message" });

        var session = await _sessions.GetByIdAsync(id, ct);
        if (session is null) return NotFound();
        if (session.Status is not ("accepted" or "active"))
            return Conflict(new { error = "session_not_active" });

        var msg = new ChatMessage(0, id, userId, text, DateTime.UtcNow);
        var msgId = await _messages.CreateAsync(msg, ct);

        await _hub.Clients.Group($"chat:{id}").SendAsync("message", new
        {
            chatMessageId = msgId,
            chatSessionId = id,
            senderUserId = userId,
            messageText = text,
            createdUtc = msg.CreatedUtc
        }, ct);

        return Ok(new { ok = true, chatMessageId = msgId });
    }
}
