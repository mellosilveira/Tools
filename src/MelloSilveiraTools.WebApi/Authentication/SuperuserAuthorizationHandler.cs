using MelloSilveiraTools.Core.Security;
using Microsoft.AspNetCore.Authorization;

namespace MelloSilveiraTools.WebApi.Authentication;

/// <summary>
/// Authorization requirement satisfied only when the current execution scope carries
/// database-verified superuser privileges in <see cref="ICurrentUserProvider"/>.
/// </summary>
public sealed class SuperuserRequirement : IAuthorizationRequirement;

/// <summary>
/// Evaluates <see cref="SuperuserRequirement"/> against <see cref="ICurrentUserProvider.IsSuperuser"/>,
/// which is populated directly from the PostgreSQL <c>users</c> table by <c>UserContextMiddleware</c>
/// rather than trusting JWT claims.
/// </summary>
/// <param name="currentUserProvider">Provider holding the live database-verified user scope.</param>
public sealed class SuperuserAuthorizationHandler(ICurrentUserProvider currentUserProvider) : AuthorizationHandler<SuperuserRequirement>
{
    /// <inheritdoc/>
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, SuperuserRequirement requirement)
    {
        if (currentUserProvider.IsSuperuser())
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
