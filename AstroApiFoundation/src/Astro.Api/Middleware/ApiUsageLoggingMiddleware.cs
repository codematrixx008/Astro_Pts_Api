using Astro.Domain.Auth;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Astro.Api.Middleware;

public sealed class ApiUsageLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public ApiUsageLoggingMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IApiUsageLogRepository logs)
    {
        var sw = Stopwatch.StartNew();
        await _next(context);
        sw.Stop();

        var path = context.Request.Path.Value ?? string.Empty;
        if (!(path.StartsWith("/v1") || path.StartsWith("/auth") || path.StartsWith("/api-keys")))
            return;

        var apiCtx = context.GetApiKeyContext();

        long? userId = null;
        var sub = context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (long.TryParse(sub, out var uid))
            userId = uid;

        var log = new ApiUsageLog(
            ApiUsageLogId: 0,
            ApiKeyId: apiCtx?.ApiKeyId,
            UserId: userId,
            Method: context.Request.Method,
            Path: path,
            StatusCode: context.Response.StatusCode,
            DurationMs: sw.ElapsedMilliseconds,
            Ip: context.Connection.RemoteIpAddress?.ToString(),
            CreatedUtc: DateTime.UtcNow
        );

        await logs.CreateAsync(log, context.RequestAborted);
    }
}
