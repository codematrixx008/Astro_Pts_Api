namespace Astro.Api.Security;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "Astro.Api";
    public string Audience { get; set; } = "Astro.Api";
    public string SigningKey { get; set; } = "";
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 14;
}
