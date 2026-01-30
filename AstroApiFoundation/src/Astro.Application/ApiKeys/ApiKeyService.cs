using Astro.Application.Common;
using Astro.Application.Security;
using Astro.Domain.Auth;

namespace Astro.Application.ApiKeys;

public sealed class ApiKeyService
{
    private readonly IApiKeyRepository _apiKeys;
    private readonly Pbkdf2Hasher _hasher;
    private readonly IClock _clock;

    public ApiKeyService(IApiKeyRepository apiKeys, Pbkdf2Hasher hasher, IClock clock)
    {
        _apiKeys = apiKeys;
        _hasher = hasher;
        _clock = clock;
    }

    public async Task<CreatedApiKey> CreateAsync(long orgId, CreateApiKeyRequest req, CancellationToken ct)
    {
        var prefix = TokenGenerator.CreatePrefix();
        var secret = TokenGenerator.CreateSecret(32);

        // store salted hash of secret
        var secretHash = _hasher.Hash(secret);

        var scopesCsv = string.Join(',', req.Scopes.Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase));

        var apiKeyId = await _apiKeys.CreateAsync(orgId, req.Name.Trim(), prefix, secretHash, scopesCsv, ct);

        return new CreatedApiKey(
            ApiKeyId: apiKeyId,
            Name: req.Name.Trim(),
            Prefix: prefix,
            Secret: $"{prefix}.{secret}",
            Scopes: scopesCsv.Length == 0 ? Array.Empty<string>() : scopesCsv.Split(','),
            CreatedUtc: _clock.UtcNow
        );
    }

    public async Task<IReadOnlyList<ApiKeyListItem>> ListAsync(long orgId, CancellationToken ct)
    {
        var rows = await _apiKeys.ListAsync(orgId, ct);

        return rows.Select(x => new ApiKeyListItem(
            x.ApiKeyId,
            x.Name,
            x.Prefix,
            x.ScopesCsv.Length == 0 ? Array.Empty<string>() : x.ScopesCsv.Split(','),
            x.IsActive,
            x.CreatedUtc,
            x.LastUsedUtc,
            x.RevokedUtc
        )).ToList();
    }

    public Task RevokeAsync(long apiKeyId, CancellationToken ct)
        => _apiKeys.RevokeAsync(apiKeyId, _clock.UtcNow, ct);
}
