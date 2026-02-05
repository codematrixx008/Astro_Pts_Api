namespace Astro.Api.Security;

public sealed class AuthCookieOptions
{
    public string RefreshCookieName { get; init; } = "astro_refresh";
    public int RefreshDays { get; init; } = 30;
    public string SameSite { get; init; } = "None"; // None for cross-site React
}
