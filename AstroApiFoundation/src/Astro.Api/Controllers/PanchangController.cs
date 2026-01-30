using Astro.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Astro.Api.Controllers;

[ApiController]
[Route("v1/panchang")]
public sealed class PanchangController : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "panchang.read")]
    public IActionResult Get(
        [FromQuery] DateOnly date,
        [FromQuery] double lat,
        [FromQuery] double lon,
        [FromQuery] string tz = "Asia/Kolkata")
    {
        Validation.EnsureLatLon(lat, lon);

        return Ok(new
        {
            date,
            lat,
            lon,
            tz,
            tithi = "TBD",
            nakshatra = "TBD",
            yoga = "TBD",
            karana = "TBD"
        });
    }
}
