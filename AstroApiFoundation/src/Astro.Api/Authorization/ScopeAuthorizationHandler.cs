using Microsoft.AspNetCore.Authorization;

namespace Astro.Api.Authorization;

public sealed class ScopeAuthorizationHandler : AuthorizationHandler<ScopeRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ScopeRequirement requirement)
    {
        var scopes = context.User.FindAll("scope").Select(x => x.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (scopes.Contains(requirement.Scope))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
