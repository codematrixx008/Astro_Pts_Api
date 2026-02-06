using Google.Apis.Auth;
using Microsoft.Extensions.Options;

namespace Astro.Api.Security;

/// <summary>
/// Validates Google ID tokens (signature, issuer, lifetime, and audience).
/// </summary>
public sealed class GoogleIdTokenValidator
{
    private readonly GoogleOAuthOptions _opts;

    public GoogleIdTokenValidator(IOptions<GoogleOAuthOptions> opts)
    {
        _opts = opts.Value;
    }

    public async Task<GoogleJsonWebSignature.Payload> ValidateAsync(string idToken, CancellationToken ct)
    {
        var audiences = new HashSet<string>(StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(_opts.WebClientId)) audiences.Add(_opts.WebClientId);
        if (_opts.AllowedAudiences is { Length: > 0 })
        {
            foreach (var a in _opts.AllowedAudiences)
                if (!string.IsNullOrWhiteSpace(a)) audiences.Add(a);
        }

        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = audiences.ToArray()
        };

        // Validates signature, exp/iat, iss, aud
        var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

        // Strongly recommended: only accept verified emails for linking by email.
        if (payload.EmailVerified != true)
            throw new UnauthorizedAccessException("google_email_not_verified");

        return payload;
    }
}
