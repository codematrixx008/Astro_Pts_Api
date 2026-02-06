namespace Astro.Api.Security;

public sealed class GoogleOAuthOptions
{
    // Web OAuth client used to exchange authorization codes from the React SPA.
    public string WebClientId { get; set; } = "";
    public string WebClientSecret { get; set; } = "";

    // Accept ID tokens whose `aud` matches any of these client IDs (web/android/ios).
    public string[] AllowedAudiences { get; set; } = Array.Empty<string>();

    // OAuth token endpoint (normally fixed)
    public string TokenEndpoint { get; set; } = "https://oauth2.googleapis.com/token";
}
