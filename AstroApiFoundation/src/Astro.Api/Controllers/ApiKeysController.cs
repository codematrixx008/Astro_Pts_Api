using Astro.Application.ApiKeys;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Astro.Api.Controllers;

[ApiController]
[Route("api-keys")]
[Authorize] // JWT protected
public sealed class ApiKeysController : ControllerBase
{
    private readonly ApiKeyService _keys;

    public ApiKeysController(ApiKeyService keys)
    {
        _keys = keys;
    }

    [HttpPost]
    public async Task<ActionResult<CreatedApiKey>> Create(
        [FromBody] CreateApiKeyRequest req,
        CancellationToken ct)
    {
        var orgIdStr = User.FindFirstValue("org_id");
        if (!long.TryParse(orgIdStr, out var orgId))
        {
            return Unauthorized(new { error = "missing_org" });
        }

        var created = await _keys.CreateAsync(orgId, req, ct);

        // Secret is returned ONLY ONCE
        return Ok(created);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ApiKeyListItem>>> List(
        CancellationToken ct)
    {
        var orgIdStr = User.FindFirstValue("org_id");
        if (!long.TryParse(orgIdStr, out var orgId))
        {
            return Unauthorized(new { error = "missing_org" });
        }

        var list = await _keys.ListAsync(orgId, ct);
        return Ok(list);
    }

    [HttpDelete("{apiKeyId:long}")]
    public async Task<IActionResult> Revoke(
        long apiKeyId,
        CancellationToken ct)
    {
        await _keys.RevokeAsync(apiKeyId, ct);
        return Ok(new { ok = true });
    }
}
