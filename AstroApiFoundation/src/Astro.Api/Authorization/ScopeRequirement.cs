using Microsoft.AspNetCore.Authorization;

namespace Astro.Api.Authorization;

public sealed class ScopeRequirement : IAuthorizationRequirement
{
    public string Scope { get; }

    public ScopeRequirement(string scope) => Scope = scope;
}
