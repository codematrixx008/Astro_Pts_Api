using Astro.Api.Authorization;
using Astro.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("v1/panchang")]
public sealed class PanchangController : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = ScopePolicies.PanchangRead)]
    public IActionResult Get(
        [FromQuery] DateOnly date,
        [FromQuery] double lat,
        [FromQuery] double lon,
        [FromQuery] string tz = "Asia/Kolkata")
    {
        Validation.EnsureDateRange(date);
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
