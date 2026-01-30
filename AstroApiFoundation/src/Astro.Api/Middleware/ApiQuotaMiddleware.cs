using Astro.Domain.ApiUsage;
using Astro.Api.Security;

namespace Astro.Api.Middleware
{
    public sealed class ApiQuotaMiddleware : IMiddleware
    {
        private readonly IApiUsageCounterRepository _counters;

        public ApiQuotaMiddleware(IApiUsageCounterRepository counters)
        {
            _counters = counters;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var apiCtx = context.GetApiKeyContext();
            if (apiCtx is null)
            {
                await next(context);
                return;
            }

            // If no quota -> just continue (still count if you want, but not required)
            var dailyQuota = apiCtx.DailyQuota; // add this into ApiKeyContext
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            if (dailyQuota is null)
            {
                await next(context);
                return;
            }

            // Increment then check (simple, consistent)
            var used = await _counters.IncrementDailyAsync(apiCtx.ApiKeyId, today, context.RequestAborted);

            context.Response.Headers["X-Quota-Daily"] = dailyQuota.Value.ToString();
            context.Response.Headers["X-Usage-Daily"] = used.ToString();

            if (used > dailyQuota.Value)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "quota_exceeded",
                    message = "Daily quota exceeded for this API key."
                });
                return;
            }

            await next(context);
        }
    }
}
