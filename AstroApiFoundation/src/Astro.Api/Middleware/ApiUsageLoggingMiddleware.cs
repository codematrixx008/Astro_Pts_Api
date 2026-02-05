using Astro.Domain.Auth;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Astro.Api.Middleware;

public sealed class ApiUsageLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public ApiUsageLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    // ✅ InvokeAsync MUST accept only HttpContext
    public async Task InvokeAsync(
        HttpContext context,
        IApiUsageLogRepository logs)
    {
        var sw = Stopwatch.StartNew();

        await _next(context);

        sw.Stop();

        // Log only API + auth routes
        var path = context.Request.Path.Value ?? string.Empty;
        if (!(path.StartsWith("/v1") ||
              path.StartsWith("/auth") ||
              path.StartsWith("/api-keys")))
        {
            return;
        }

        var apiCtx = context.GetApiKeyContext();
        long? apiKeyId = apiCtx?.ApiKeyId;

        long? userId = null;
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var sub =
                context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (long.TryParse(sub, out var uid))
                userId = uid;
        }

        var log = new ApiUsageLog(
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

        // ✅ Correct cancellation token source
        await logs.CreateAsync(log, context.RequestAborted);
    }
}