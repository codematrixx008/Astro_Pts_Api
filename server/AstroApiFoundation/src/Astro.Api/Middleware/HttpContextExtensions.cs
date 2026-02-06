namespace Astro.Api.Middleware;

public static class HttpContextExtensions
{
    private const string Key = "Astro.ApiKeyContext";

    public static void SetApiKeyContext(this HttpContext ctx, ApiKeyContext value)
        => ctx.Items[Key] = value;

    public static ApiKeyContext? GetApiKeyContext(this HttpContext ctx)
        => ctx.Items.TryGetValue(Key, out var v) ? v as ApiKeyContext : null;
}
