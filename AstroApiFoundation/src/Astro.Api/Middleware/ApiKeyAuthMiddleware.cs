using Astro.Application.Security;
using Astro.Domain.Auth;
using System.Security.Claims;

namespace Astro.Api.Middleware;

public sealed class ApiKeyAuthMiddleware
{
    private readonly RequestDelegate _next;

    public ApiKeyAuthMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(
        HttpContext context,
        IApiKeyRepository apiKeys,
        Pbkdf2Hasher hasher,
        CancellationToken ct)
    {
        // If already authenticated (JWT) we still allow, but public API is intended for API keys.
        // You can change this behavior later.
        if (!context.Request.Headers.TryGetValue("X-Api-Key", out var headerValue))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "missing_api_key" }, ct);
            return;
        }

        var raw = headerValue.ToString().Trim();
        var parts = raw.Split('.', 2);
        if (parts.Length != 2)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "invalid_api_key_format" }, ct);
            return;
        }

        var prefix = parts[0];
        var secret = parts[1];

        var apiKey = await apiKeys.GetActiveByPrefixAsync(prefix, ct);
        if (apiKey is null || !apiKey.IsActive || apiKey.RevokedUtc is not null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "invalid_api_key" }, ct);
            return;
        }

        if (!hasher.Verify(secret, apiKey.SecretHash))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "invalid_api_key" }, ct);
            return;
        }

        var scopes = (apiKey.ScopesCsv ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        context.SetApiKeyContext(new ApiKeyContext(apiKey.ApiKeyId, apiKey.OrgId, scopes, apiKey.Prefix, apiKey.DailyQuota, apiKey.PlanCode));

        // Set principal for authorization policies
        var claims = new List<Claim>
        {
            new Claim("api_key_id", apiKey.ApiKeyId.ToString()),
            new Claim("org_id", apiKey.OrgId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, $"apikey:{apiKey.ApiKeyId}")
        };
        foreach (var s in scopes) claims.Add(new Claim("scope", s));

        var identity = new ClaimsIdentity(claims, authenticationType: "ApiKey");
        context.User = new ClaimsPrincipal(identity);

        await apiKeys.TouchLastUsedAsync(apiKey.ApiKeyId, DateTime.UtcNow, ct);

        await _next(context);
    }
}
