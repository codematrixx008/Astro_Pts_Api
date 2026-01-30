using Astro.Api.Common;
using Astro.Application.Marketplace;
using Astro.Domain.Marketplace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Astro.Api.Controllers;

[ApiController]
[Route("astrologers")]
public sealed class AstrologersController : ControllerBase
{
    private readonly IAstrologerProfileRepository _profiles;
    private readonly IAstrologerAvailabilityRepository _availability;

    public AstrologersController(IAstrologerProfileRepository profiles, IAstrologerAvailabilityRepository availability)
    {
        _profiles = profiles;
        _availability = availability;
    }

    // Consumer applies to become astrologer
    [HttpPost("apply")]
    [Authorize(Roles = "consumer")]
    public async Task<IActionResult> Apply([FromBody] ApplyAstrologerRequest req, CancellationToken ct)
    {
        var userId = User.RequireUserId();

        var existing = await _profiles.GetByIdAsync(userId, ct);
        if (existing is not null)
            return Conflict(new { error = "already_applied" });

        var profile = new AstrologerProfile(
            AstrologerId: userId,
            DisplayName: req.DisplayName.Trim(),
            Bio: req.Bio,
            ExperienceYears: req.ExperienceYears,
            LanguagesCsv: string.Join(",", req.Languages.Select(x => x.Trim()).Where(x => x.Length > 0)),
            SpecializationsCsv: string.Join(",", req.Specializations.Select(x => x.Trim()).Where(x => x.Length > 0)),
            PricePerMinute: req.PricePerMinute,
            Status: "applied",
            CreatedUtc: DateTime.UtcNow,
            VerifiedUtc: null
        );

        await _profiles.CreateAsync(profile, ct);
        return Ok(new { ok = true, status = "applied" });
    }

    // Astrologer activates after verify (optional)
    [HttpPost("me/activate")]
    [Authorize(Roles = "astrologer")]
    public async Task<IActionResult> Activate(CancellationToken ct)
    {
        var userId = User.RequireUserId();
        var profile = await _profiles.GetByIdAsync(userId, ct);
        if (profile is null) return NotFound();

        if (profile.Status is not ("verified" or "active"))
            return BadRequest(new { error = "not_verified" });

        await _profiles.UpdateStatusAsync(userId, "active", profile.VerifiedUtc, ct);
        return Ok(new { ok = true, status = "active" });
    }

    // Public listing (basic filter)
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List([FromQuery] string? language, [FromQuery] string? specialization, CancellationToken ct)
    {
        // For MVP: keep simple. You can implement listing via SQL in repository later.
        // Right now, you can just return "not implemented" or implement a List repo method.
        return Ok(new { message = "Implement listing in repository (ListActiveAsync)." });
    }

    [HttpGet("{id:long}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProfile(long id, CancellationToken ct)
    {
        var profile = await _profiles.GetByIdAsync(id, ct);
        if (profile is null) return NotFound();
        return Ok(profile);
    }

    // Availability (astrologer self)
    [HttpGet("me/availability")]
    [Authorize(Roles = "astrologer")]
    public async Task<IActionResult> GetMyAvailability(CancellationToken ct)
    {
        var userId = User.RequireUserId();
        var rows = await _availability.GetByAstrologerAsync(userId, ct);
        return Ok(rows);
    }

    [HttpPost("me/availability")]
    [Authorize(Roles = "astrologer")]
    public async Task<IActionResult> AddAvailability([FromBody] AvailabilityCreateRequest req, CancellationToken ct)
    {
        var userId = User.RequireUserId();

        if (req.DayOfWeek < 0 || req.DayOfWeek > 6)
            return BadRequest(new { error = "invalid_dayofweek" });

        if (!TimeSpan.TryParse(req.StartTime, out var start) ||
            !TimeSpan.TryParse(req.EndTime, out var end) ||
            end <= start)
            return BadRequest(new { error = "invalid_time_range" });

        var slot = new AstrologerAvailability(
            AvailabilityId: 0,
            AstrologerId: userId,
            DayOfWeek: req.DayOfWeek,
            StartTime: start,
            EndTime: end,
            IsActive: true,
            CreatedUtc: DateTime.UtcNow
        );

        await _availability.AddAsync(slot, ct);
        return Ok(new { ok = true });
    }

    [HttpDelete("me/availability/{availabilityId:long}")]
    [Authorize(Roles = "astrologer")]
    public async Task<IActionResult> DisableAvailability(long availabilityId, CancellationToken ct)
    {
        var userId = User.RequireUserId();
        await _availability.DisableAsync(availabilityId, userId, ct);
        return Ok(new { ok = true });
    }

    // Consumer views astrologer availability
    [HttpGet("{id:long}/availability")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAvailability(long id, CancellationToken ct)
    {
        var rows = await _availability.GetByAstrologerAsync(id, ct);
        return Ok(rows.Where(x => x.IsActive));
    }
}
