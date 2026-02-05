using Astro.Api.Common;
using Astro.Domain.Consumers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Astro.Api.Controllers;

[ApiController]
[Route("profile")]
[Authorize(Roles = "consumer")]
public sealed class ConsumerProfileController : ControllerBase
{
    private readonly IConsumerProfileRepository _profiles;

    public ConsumerProfileController(IConsumerProfileRepository profiles) => _profiles = profiles;

    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var userId = User.RequireUserId();
        var p = await _profiles.GetByUserIdAsync(userId, ct);

        // If not created yet, return empty defaults (UI will open popup)
        if (p is null)
            return Ok(new { userId, fullName = "", gender = (string?)null, phone = (string?)null, maritalStatus = (string?)null, occupation = (string?)null, city = (string?)null, preferredLanguage = (string?)null });

        return Ok(p);
    }

    public sealed record UpdateConsumerProfileRequest(
        string FullName,
        string? Gender,
        string? Phone,
        string? MaritalStatus,
        string? Occupation,
        string? City,
        string? PreferredLanguage);

    [HttpPost("me")]
    public async Task<IActionResult> UpsertMe([FromBody] UpdateConsumerProfileRequest req, CancellationToken ct)
    {
        var userId = User.RequireUserId();

        if (string.IsNullOrWhiteSpace(req.FullName))
            return BadRequest(new { error = "fullName_required" });

        var model = new ConsumerProfile(
            UserId: userId,
            FullName: req.FullName.Trim(),
            Gender: string.IsNullOrWhiteSpace(req.Gender) ? null : req.Gender.Trim(),
            Phone: string.IsNullOrWhiteSpace(req.Phone) ? null : req.Phone.Trim(),
            MaritalStatus: string.IsNullOrWhiteSpace(req.MaritalStatus) ? null : req.MaritalStatus.Trim(),
            Occupation: string.IsNullOrWhiteSpace(req.Occupation) ? null : req.Occupation.Trim(),
            City: string.IsNullOrWhiteSpace(req.City) ? null : req.City.Trim(),
            PreferredLanguage: string.IsNullOrWhiteSpace(req.PreferredLanguage) ? null : req.PreferredLanguage.Trim(),
            UpdatedUtc: DateTime.UtcNow
        );

        await _profiles.UpsertAsync(model, ct);
        return Ok(new { ok = true });
    }
}
