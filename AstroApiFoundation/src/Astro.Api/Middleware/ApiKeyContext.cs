namespace Astro.Api.Middleware;

public sealed record ApiKeyContext(
    long ApiKeyId,
    long OrgId,
    IReadOnlyList<string> Scopes,
    string Prefix
);
