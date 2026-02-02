using Astro.Application.Ephemeris;
using Astro.Application.Common;
using Astro.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Astro.Api.Controllers;

[ApiController]
[Route("v1/ephemeris")]
public sealed class EphemerisController : ControllerBase
{
    private readonly IEphemerisService _ephemeris;

    public EphemerisController(IEphemerisService ephemeris) => _ephemeris = ephemeris;

    [HttpGet("planet-positions")]
    [Authorize(Policy = ScopePolicies.EphemerisRead)]
    public async Task<ActionResult<PlanetPositionResponse>> GetPlanetPositions(
        [FromQuery] string datetimeUtc,
        CancellationToken ct)
    {
        if (!DateTimeOffset.TryParse(datetimeUtc, out var dto))
            return BadRequest(new { error = "invalid_datetimeUtc", message = "Use ISO8601 UTC like 2026-01-30T00:00:00Z" });

        if (dto.Offset != TimeSpan.Zero)
            return BadRequest(new { error = "datetimeUtc_must_be_utc", message = "datetimeUtc must have Z/UTC offset" });

        var input = dto.UtcDateTime;
        try { Validation.EnsureUtcAndRange(input); }
        catch (Exception ex) { return BadRequest(new { error = "invalid_datetimeUtc", message = ex.Message }); }

        var resp = await _ephemeris.GetPlanetPositionsAsync(new PlanetPositionRequest(input), ct);
        return Ok(resp);
    }
}
