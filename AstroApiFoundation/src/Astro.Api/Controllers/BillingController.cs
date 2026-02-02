using Astro.Api.Common;
using Astro.Application.Billing;
using Astro.Domain.Billing;
using Astro.Domain.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Astro.Api.Controllers;

[ApiController]
[Route("billing")]
[Authorize]
public sealed class BillingController : ControllerBase
{
    private readonly IChatSessionRepository _sessions;
    private readonly ILedgerRepository _ledger;
    private readonly BillingService _billing;

    public BillingController(IChatSessionRepository sessions, ILedgerRepository ledger, BillingService billing)
    {
        _sessions = sessions;
        _ledger = ledger;
        _billing = billing;
    }

    [HttpGet("sessions/{chatSessionId:long}/estimate")]
    public async Task<IActionResult> Estimate(long chatSessionId, CancellationToken ct)
    {
        var userId = User.RequireUserId();
        if (!await _sessions.IsParticipantAsync(chatSessionId, userId, ct))
            return Forbid();

        var session = await _sessions.GetByIdAsync(chatSessionId, ct);
        if (session is null) return NotFound();

        var est = _billing.Estimate(session, currency: "INR");
        return Ok(est);
    }

    [HttpGet("me/ledger")]
    public async Task<IActionResult> MyLedger([FromQuery] int take = 50, CancellationToken ct = default)
    {
        var userId = User.RequireUserId();
        take = Math.Clamp(take, 1, 200);
        var rows = await _ledger.ListForUserAsync(userId, take, ct);
        return Ok(rows);
    }

    [HttpGet("me/balance")]
    public async Task<IActionResult> MyBalance([FromQuery] string currency = "INR", CancellationToken ct = default)
    {
        var userId = User.RequireUserId();
        var bal = await _ledger.GetBalanceAsync(userId, currency, ct);
        return Ok(new { currency, balance = bal });
    }
}
