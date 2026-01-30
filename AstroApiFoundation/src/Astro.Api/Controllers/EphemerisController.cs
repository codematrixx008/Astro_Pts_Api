using Astro.Application.Common;
using Astro.Application.Ephemeris;
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
            return BadRequest("Invalid datetimeUtc. Use ISO8601 UTC.");

        var dt = dto.UtcDateTime;
        Validation.EnsureUtcAndRange(dt);

        var resp = await _ephemeris.GetPlanetPositionsAsync(
            new PlanetPositionRequest(dt), ct);

        return Ok(resp);
    }

    [HttpGet("ascendant")]
    [Authorize(Policy = ScopePolicies.EphemerisRead)]
    public IActionResult GetAscendant(
        [FromQuery] string datetimeUtc,
        [FromQuery] double lat,
        [FromQuery] double lon)
    {
        if (!DateTimeOffset.TryParse(datetimeUtc, out var dto))
            return BadRequest("Invalid datetimeUtc.");

        var dt = dto.UtcDateTime;
        Validation.EnsureUtcAndRange(dt);
        Validation.EnsureLatLon(lat, lon);

        return Ok(new
        {
            datetimeUtc = dt,
            lat,
            lon,
            ayanamsa = "lahiri",
            ascendantLongitudeDeg = 0.0
        });
    }
}
