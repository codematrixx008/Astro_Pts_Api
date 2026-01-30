using Astro.Domain.ApiUsage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Astro.Api.Controllers;

[ApiController]
[Route("usage")]
[Authorize]
public sealed class UsageController : ControllerBase
{
    private readonly IApiUsageCounterRepository _counters;

    public UsageController(IApiUsageCounterRepository counters)
        => _counters = counters;

    [HttpGet("daily-range")]
    public async Task<IActionResult> DailyRange(
        [FromQuery] long apiKeyId,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken ct)
    {
        if (to < from) return BadRequest("to must be >= from");

        var rows = await _counters.GetDailyRangeAsync(apiKeyId, from, to, ct);
        return Ok(new { apiKeyId, from, to, days = rows });
    }

    [HttpGet("me")]
    public IActionResult Me()
    {
        return Ok(new
        {
            sub = User.FindFirstValue(ClaimTypes.NameIdentifier),
            email = User.FindFirstValue("email"),
            orgId = User.FindFirstValue("org_id"),
            role = User.FindFirstValue(ClaimTypes.Role)
        });
    }
}
