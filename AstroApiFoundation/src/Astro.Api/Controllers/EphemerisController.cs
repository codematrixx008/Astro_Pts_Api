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
        [FromQuery] DateTime? datetimeUtc,
        CancellationToken ct)
    {
        var input = datetimeUtc?.ToUniversalTime() ?? DateTime.UtcNow;
        var resp = await _ephemeris.GetPlanetPositionsAsync(new PlanetPositionRequest(input), ct);
        return Ok(resp);
    }
}
