using Astro.Application.Ephemeris;
using Astro.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Astro.Application.Common;

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
            return BadRequest("Invalid datetimeUtc. Use ISO8601 UTC like 2026-01-30T00:00:00Z.");

        var dt = dto.UtcDateTime;
        Validation.EnsureUtcAndRange(dt);

        var resp = await _ephemeris.GetPlanetPositionsAsync(new PlanetPositionRequest(dt), ct);
        return Ok(resp);
    }
}
