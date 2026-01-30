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

    public AdminAstrologersController(IAstrologerProfileRepository profiles) => _profiles = profiles;

    [HttpPost("{astrologerId:long}/verify")]
    public async Task<IActionResult> Verify(long astrologerId, CancellationToken ct)
    {
        var profile = await _profiles.GetByIdAsync(astrologerId, ct);
        if (profile is null) return NotFound();

        if (profile.Status != "applied")
            return BadRequest(new { error = "invalid_status", status = profile.Status });

        await _profiles.UpdateStatusAsync(astrologerId, "verified", DateTime.UtcNow, ct);

        // IMPORTANT: You must also update user role to astrologer in your user/org role table.
        // Implement in your existing UserOrganizationRepository: SetRole(userId, "astrologer")

        return Ok(new { ok = true, status = "verified" });
    }
}
