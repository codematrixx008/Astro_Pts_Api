using Astro.Domain.Auth;

namespace Astro.Api.Middleware;

public sealed class ApiUsageLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiUsageLoggingMiddleware> _log;

    public ApiUsageLoggingMiddleware(
        RequestDelegate next,
        ILogger<ApiUsageLoggingMiddleware> log)
    {
        _next = next;
        _log = log;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IApiUsageLogRepository logs // ✅ scoped, resolved per-request
    )
    {
        var ct = context.RequestAborted;

        var sw = System.Diagnostics.Stopwatch.StartNew();
        await _next(context);
        sw.Stop();

        var path = context.Request.Path.Value ?? "";
        if (!(path.StartsWith("/v1") ||
              path.StartsWith("/auth") ||
              path.StartsWith("/api-keys")))
            return;

        try
        {
            var apiCtx = context.GetApiKeyContext();
            long? apiKeyId = apiCtx?.ApiKeyId;

            long? userId = null;
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var sub =
                    context.User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                    ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (long.TryParse(sub, out var uid))
                    userId = uid;
            }

            var logEntry = new ApiUsageLog(
                ApiUsageLogId: 0,
                ApiKeyId: apiKeyId,
                UserId: userId,
                Method: context.Request.Method,
                Path: path,
                StatusCode: context.Response.StatusCode,
                DurationMs: sw.ElapsedMilliseconds,
                Ip: context.Connection.RemoteIpAddress?.ToString(),
                CreatedUtc: DateTime.UtcNow
            );

            await logs.CreateAsync(logEntry, ct);
        }
        catch (Exception ex)
        {
            // Never break the request because of logging
            _log.LogError(ex, "Failed to log API usage");
        }
    }
}
