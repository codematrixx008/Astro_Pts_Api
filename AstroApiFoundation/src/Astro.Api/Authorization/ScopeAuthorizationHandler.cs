using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Astro.Api.Authorization;

public sealed class ScopeAuthorizationHandler : AuthorizationHandler<ScopeRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ScopeRequirement requirement)
    {
        // Read all "scope" claims
        var scopes = context.User.FindAll("scope").Select(c => c.Value).SelectMany(v => v.Split(new[] { ' ', ',' },StringSplitOptions.RemoveEmptyEntries)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (scopes.Contains(requirement.Scope))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
