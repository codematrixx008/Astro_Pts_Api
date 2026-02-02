using Astro.Domain.ApiUsage;

namespace Astro.Api.Middleware;

/// <summary>
/// Enforces per-API-key daily quota (if configured on the ApiKey).
/// Must run after ApiKeyAuthMiddleware so ApiKeyContext is available.
/// </summary>
public sealed class ApiQuotaMiddleware
{
    private readonly RequestDelegate _next;

    public ApiQuotaMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IApiUsageCounterRepository counters)
    {
        var ct = context.RequestAborted;
        var apiCtx = context.GetApiKeyContext();
        if (apiCtx is null)
        {
            await _next(context);
            return;
        }

        if (apiCtx.DailyQuota is null or <= 0)
        {
            await _next(context);
            return;
        }

        var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow);
        var count = await counters.IncrementDailyAsync(apiCtx.ApiKeyId, todayUtc, ct);

        if (count > apiCtx.DailyQuota.Value)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "quota_exceeded",
                quota = apiCtx.DailyQuota.Value,
                used = count
            }, ct);
            return;
        }

        await _next(context);
    }
}
