using Astro.Application.Auth;
using Astro.Application.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Astro.Api.Security;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _opts;

    public JwtTokenService(IOptions<JwtOptions> opts) => _opts = opts.Value;

    public AuthTokens CreateTokens(long userId, long orgId, string email, IReadOnlyList<string> roles, IReadOnlyList<string> scopes)
    {
        var now = DateTime.UtcNow;
        var accessExp = now.AddMinutes(_opts.AccessTokenMinutes);
        var refreshExp = now.AddDays(_opts.RefreshTokenDays);

        
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()), // NEW (helps controllers)
            new("org_id", orgId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };


        foreach (var r in roles) claims.Add(new Claim(ClaimTypes.Role, r));
        foreach (var s in scopes) claims.Add(new Claim("scope", s));


        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opts.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _opts.Issuer,
            audience: _opts.Audience,
            claims: claims,
            notBefore: now,
            expires: accessExp,
            signingCredentials: creds);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        // refresh token is opaque secret
        var refreshToken = TokenGenerator.CreateSecret(48);

        return new AuthTokens(accessToken, refreshToken, accessExp, refreshExp);
    }

    public ClaimsPrincipal? ValidateAccessToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opts.SigningKey));

        try
        {
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidIssuer = _opts.Issuer,
                ValidAudience = _opts.Audience,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            }, out _);

            return principal;
        }
        catch
        {
            return null;
        }
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opts.SigningKey));

        try
        {
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidIssuer = _opts.Issuer,
                ValidAudience = _opts.Audience,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateLifetime = false,
                ClockSkew = TimeSpan.Zero,
                RoleClaimType = ClaimTypes.Role,

            }, out _);

            return principal;
        }
        catch
        {
            return null;
        }
    }
}
