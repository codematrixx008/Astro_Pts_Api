using Astro.Api.Common;
using Astro.Application.Billing;
using Astro.Domain.Billing;
using Astro.Domain.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Astro.Api.Controllers;

[ApiController]
[Route("billing")]
public sealed class BillingController : ControllerBase
{
    private readonly ILedgerRepository _ledger;
    private readonly IPayoutRepository _payouts;
    private readonly IChatSessionRepository _sessions;
    private readonly BillingService _billing;

    public BillingController(ILedgerRepository ledger, IPayoutRepository payouts, IChatSessionRepository sessions, BillingService billing)
    {
        _ledger = ledger;
        _payouts = payouts;
        _sessions = sessions;
        _billing = billing;
    }

    // ==========================
    // Session estimate (participant)
    // ==========================
    [HttpGet("sessions/{id:long}/estimate")]
    [Authorize]
    public async Task<IActionResult> Estimate(long id, [FromQuery] string currency = "INR", CancellationToken ct = default)
    {
        var userId = User.RequireUserId();
        if (!await _sessions.IsParticipantAsync(id, userId, ct)) return Forbid();

        var session = await _sessions.GetByIdAsync(id, ct);
        if (session is null) return NotFound();

        return Ok(_billing.Estimate(session, currency));
    }

    // ==========================
    // My ledger & balance
    // ==========================
    [HttpGet("me/ledger")]
    [Authorize]
    public async Task<IActionResult> MyLedger([FromQuery] int take = 50, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 500);
        var userId = User.RequireUserId();
        var rows = await _ledger.ListForUserAsync(userId, take, ct);
        return Ok(rows);
    }

    [HttpGet("me/balance")]
    [Authorize]
    public async Task<IActionResult> MyBalance([FromQuery] string currency = "INR", CancellationToken ct = default)
    {
        var userId = User.RequireUserId();
        var bal = await _ledger.GetBalanceAsync(userId, currency, ct);
        return Ok(new { currency, balance = bal });
    }

    // ==========================
    // Platform ledger (admin)
    // ==========================
    [HttpGet("platform/balance")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> PlatformBalance([FromQuery] string currency = "INR", CancellationToken ct = default)
    {
        var bal = await _ledger.GetPlatformBalanceAsync(currency, ct);
        return Ok(new { currency, balance = bal });
    }

    [HttpGet("platform/ledger")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> PlatformLedger([FromQuery] int take = 50, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 500);
        var rows = await _ledger.ListForPlatformAsync(take, ct);
        return Ok(rows);
    }

    // ==========================
    // Payouts (astrologer)
    // ==========================
    [HttpPost("payouts/request")]
    [Authorize(Roles = "astrologer")]
    public async Task<IActionResult> RequestPayout([FromBody] PayoutRequest req, CancellationToken ct)
    {
        var astrologerId = User.RequireUserId();

        if (req.Amount <= 0) return BadRequest(new { error = "invalid_amount" });
        if (string.IsNullOrWhiteSpace(req.Currency)) return BadRequest(new { error = "invalid_currency" });

        var available = await _ledger.GetBalanceAsync(astrologerId, req.Currency.Trim().ToUpperInvariant(), ct);
        if (available < req.Amount)
            return Conflict(new { error = "insufficient_balance", available });

        var payout = new Payout(
            PayoutId: 0,
            AstrologerUserId: astrologerId,
            Amount: Math.Round(req.Amount, 2),
            Currency: req.Currency.Trim().ToUpperInvariant(),
            Status: "requested",
            RequestedUtc: DateTime.UtcNow,
            PaidUtc: null,
            MetaJson: JsonSerializer.Serialize(new { notes = req.Notes })
        );

        var id = await _payouts.CreateAsync(payout, ct);
        return Ok(new { ok = true, payoutId = id, status = "requested" });
    }

    [HttpGet("payouts/me")]
    [Authorize(Roles = "astrologer")]
    public async Task<IActionResult> MyPayouts([FromQuery] int take = 50, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 200);
        var astrologerId = User.RequireUserId();
        var rows = await _payouts.ListForUserAsync(astrologerId, take, ct);
        return Ok(rows);
    }

    // ==========================
    // Payout admin
    // ==========================
    [HttpGet("payouts/admin")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> ListPayouts([FromQuery] string status = "requested", [FromQuery] int take = 50, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 500);
        var rows = await _payouts.ListAllAsync(status?.Trim().ToLowerInvariant() ?? "", take, ct);
        return Ok(rows);
    }

    [HttpPost("payouts/{id:long}/mark-paid")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> MarkPaid(long id, CancellationToken ct)
    {
        var payout = await _payouts.GetByIdAsync(id, ct);
        if (payout is null) return NotFound();

        if (payout.Status != "requested")
            return Conflict(new { error = "invalid_status", status = payout.Status });

        var now = DateTime.UtcNow;
        var ok = await _payouts.MarkPaidAsync(id, now, ct);
        if (!ok) return Conflict(new { error = "cannot_mark_paid" });

        // Record payout in ledger (reduces astrologer balance)
        await _ledger.CreateAsync(new LedgerTransaction(
            LedgerTransactionId: 0,
            ChatSessionId: null,
            UserId: payout.AstrologerUserId,
            EntryType: "payout",
            Amount: payout.Amount,
            Currency: payout.Currency,
            CreatedUtc: now,
            MetaJson: payout.MetaJson
        ), ct);

        return Ok(new { ok = true, status = "paid" });
    }
}
