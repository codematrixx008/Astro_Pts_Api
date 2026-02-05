using Microsoft.Extensions.Options;

namespace Astro.Api.Security;

/// <summary>
/// Exchanges Google OAuth authorization codes (Auth Code + PKCE) for tokens.
/// The client performs the authorize redirect (PKCE) and sends:
/// - code
/// - code_verifier
/// - redirect_uri
/// The server exchanges code -> tokens via Google's token endpoint.
/// </summary>
public sealed class GoogleOAuthClient
{
    private readonly HttpClient _http;
    private readonly GoogleOAuthOptions _opts;

    public GoogleOAuthClient(HttpClient http, IOptions<GoogleOAuthOptions> opts)
    {
        _http = http;
        _opts = opts.Value;
    }

    public sealed class TokenResponse
    {
        public string access_token { get; set; } = "";
        public string id_token { get; set; } = "";
        public string refresh_token { get; set; } = "";
        public int expires_in { get; set; }
        public string token_type { get; set; } = "";
        public string scope { get; set; } = "";
    }

    public async Task<TokenResponse> ExchangeCodeAsync(
        string code,
        string codeVerifier,
        string redirectUri,
        CancellationToken ct)
    {
        var form = new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = _opts.WebClientId,
            ["client_secret"] = _opts.WebClientSecret,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code",
            ["code_verifier"] = codeVerifier
        };

        using var content = new FormUrlEncodedContent(form);
        using var resp = await _http.PostAsync(_opts.TokenEndpoint, content, ct);

        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"google_token_exchange_failed: {(int)resp.StatusCode} {body}");

        var dto = System.Text.Json.JsonSerializer.Deserialize<TokenResponse>(
            body,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (dto is null || string.IsNullOrWhiteSpace(dto.id_token))
            throw new InvalidOperationException("google_token_exchange_failed: missing id_token");

        return dto;
    }
}
