using Astro.Domain.ApiUsage;

namespace Astro.Api.Middleware;

public sealed class ApiQuotaMiddleware
{
    private readonly RequestDelegate _next;

    public ApiQuotaMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IApiUsageCounterRepository counters)
    {
        var apiCtx = context.GetApiKeyContext();
        if (apiCtx is null || apiCtx.DailyQuota is null)
        {
            await _next(context);
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var used = await counters.IncrementDailyAsync(apiCtx.ApiKeyId, today, context.RequestAborted);

        context.Response.Headers["X-Quota-Daily"] = apiCtx.DailyQuota.Value.ToString();
        context.Response.Headers["X-Usage-Daily"] = used.ToString();

        if (used > apiCtx.DailyQuota.Value)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "quota_exceeded",
                message = "Daily quota exceeded for this API key."
            }, context.RequestAborted);
            return;
        }

        await _next(context);
    }
}
