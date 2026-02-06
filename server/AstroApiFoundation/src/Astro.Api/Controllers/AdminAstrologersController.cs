using Astro.Domain.Auth;
using Astro.Domain.Marketplace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Astro.Api.Controllers;

[ApiController]
[Route("admin/astrologers")]
[Authorize(Roles = "admin")]
public sealed class AdminAstrologersController : ControllerBase
{
    private readonly IAstrologerProfileRepository _profiles;
    private readonly IUserRoleRepository _roles;

    public AdminAstrologersController(IAstrologerProfileRepository profiles, IUserRoleRepository roles)
    {
        _profiles = profiles;
        _roles = roles;
    }

    // Admin verifies an applied profile and grants astrologer role
    [HttpPost("{id:long}/verify")]
    public async Task<IActionResult> Verify(long id, CancellationToken ct)
    {
        var profile = await _profiles.GetByIdAsync(id, ct);
        if (profile is null) return NotFound(new { error = "not_found" });

        if (profile.Status != "applied")
            return BadRequest(new { error = "invalid_status", status = profile.Status });

        var now = DateTime.UtcNow;
        await _profiles.UpdateStatusAsync(id, "verified", now, ct);
        await _roles.EnsureUserHasRoleAsync(id, "astrologer", createdBy: null, ct);

        return Ok(new { ok = true, status = "verified" });
    }
}
