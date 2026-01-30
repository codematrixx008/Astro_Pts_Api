namespace Astro.Application.ApiKeys;

public sealed record CreateApiKeyRequest(
    string Name,
    IReadOnlyList<string> Scopes
);

public sealed record CreatedApiKey(
    long ApiKeyId,
    string Name,
    string Prefix,
    string Secret, // shown once
    IReadOnlyList<string> Scopes,
    DateTime CreatedUtc
);

public sealed record ApiKeyListItem(
    long ApiKeyId,
    string Name,
    string Prefix,
    IReadOnlyList<string> Scopes,
    bool IsActive,
    DateTime CreatedUtc,
    DateTime? LastUsedUtc,
    DateTime? RevokedUtc
);
