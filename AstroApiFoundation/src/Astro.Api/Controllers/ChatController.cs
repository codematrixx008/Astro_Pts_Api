using Astro.Api.Common;
using Astro.Application.Chat;
using Astro.Domain.Chat;
using Astro.Domain.Marketplace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Astro.Api.Controllers;

[ApiController]
[Route("chat")]
public sealed class ChatController : ControllerBase
{
    private readonly IChatSessionRepository _sessions;
    private readonly IChatMessageRepository _messages;
    private readonly IAstrologerProfileRepository _profiles;

    public ChatController(IChatSessionRepository sessions, IChatMessageRepository messages, IAstrologerProfileRepository profiles)
    {
        _sessions = sessions;
        _messages = messages;
        _profiles = profiles;
    }

    [HttpPost("request")]
    [Authorize(Roles = "consumer")]
    public async Task<IActionResult> RequestChat([FromBody] ChatRequestCreateRequest req, CancellationToken ct)
    {
        var consumerId = User.RequireUserId();

        var astro = await _profiles.GetByIdAsync(req.AstrologerId, ct);
        if (astro is null || astro.Status != "active")
            return BadRequest(new { error = "astrologer_not_available" });

        var session = new ChatSession(
            ChatSessionId: 0,
            ConsumerId: consumerId,
            AstrologerId: req.AstrologerId,
            Status: "requested",
            RequestedUtc: DateTime.UtcNow,
            AcceptedUtc: null,
            StartedUtc: null,
            EndedUtc: null,
            RatePerMinute: astro.PricePerMinute,
            Topic: req.Topic
        );

        var id = await _sessions.CreateRequestAsync(session, ct);
        return Ok(new { chatSessionId = id, status = "requested" });
    }

    [HttpPost("{chatSessionId:long}/accept")]
    [Authorize(Roles = "astrologer")]
    public async Task<IActionResult> Accept(long chatSessionId, CancellationToken ct)
    {
        var astrologerId = User.RequireUserId();

        var s = await _sessions.GetByIdAsync(chatSessionId, ct);
        if (s is null) return NotFound();
        if (s.AstrologerId != astrologerId) return Forbid();

        await _sessions.AcceptAsync(chatSessionId, astrologerId, DateTime.UtcNow, ct);
        await _sessions.StartAsync(chatSessionId, DateTime.UtcNow, ct);

        return Ok(new { ok = true, status = "active" });
    }

    [HttpPost("{chatSessionId:long}/end")]
    [Authorize] // participant check below
    public async Task<IActionResult> End(long chatSessionId, CancellationToken ct)
    {
        var userId = User.RequireUserId();
        var s = await _sessions.GetByIdAsync(chatSessionId, ct);
        if (s is null) return NotFound();

        if (s.ConsumerId != userId && s.AstrologerId != userId)
            return Forbid();

        await _sessions.EndAsync(chatSessionId, DateTime.UtcNow, ct);
        return Ok(new { ok = true, status = "ended" });
    }

    [HttpGet("my")]
    [Authorize(Roles = "consumer")]
    public async Task<IActionResult> MySessions(CancellationToken ct)
    {
        var id = User.RequireUserId();
        return Ok(await _sessions.GetForConsumerAsync(id, ct));
    }

    [HttpGet("inbox")]
    [Authorize(Roles = "astrologer")]
    public async Task<IActionResult> Inbox(CancellationToken ct)
    {
        var id = User.RequireUserId();
        return Ok(await _sessions.GetForAstrologerAsync(id, ct));
    }

    [HttpGet("{chatSessionId:long}/messages")]
    [Authorize]
    public async Task<IActionResult> GetMessages(long chatSessionId, [FromQuery] long? afterMessageId, [FromQuery] int take = 50, CancellationToken ct = default)
    {
        var userId = User.RequireUserId();
        var s = await _sessions.GetByIdAsync(chatSessionId, ct);
        if (s is null) return NotFound();

        if (s.ConsumerId != userId && s.AstrologerId != userId)
            return Forbid();

        var rows = await _messages.GetBySessionAsync(chatSessionId, afterMessageId, take, ct);
        return Ok(rows);
    }

    [HttpPost("{chatSessionId:long}/messages")]
    [Authorize]
    public async Task<IActionResult> SendMessage(long chatSessionId, [FromBody] SendMessageRequest req, CancellationToken ct)
    {
        var userId = User.RequireUserId();
        var s = await _sessions.GetByIdAsync(chatSessionId, ct);
        if (s is null) return NotFound();

        if (s.ConsumerId != userId && s.AstrologerId != userId)
            return Forbid();

        if (string.IsNullOrWhiteSpace(req.Message))
            return BadRequest(new { error = "empty_message" });

        if (s.Status != "active")
            return BadRequest(new { error = "session_not_active", status = s.Status });

        var msg = new ChatMessage(
            MessageId: 0,
            ChatSessionId: chatSessionId,
            SenderUserId: userId,
            Message: req.Message.Trim(),
            CreatedUtc: DateTime.UtcNow
        );

        var id = await _messages.CreateAsync(msg, ct);
        return Ok(new { messageId = id });
    }
}
