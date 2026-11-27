using MelloSilveiraTools.Core.Security;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Globalization;
using System.Security.Claims;

namespace MelloSilveiraTools.WebApi.Application.Middlewares;

/// <summary>
/// Resolves the authenticated HTTP <see cref="HttpContext.User"/> identifier against the PostgreSQL
/// <c>users</c> table and establishes the live <see cref="ICurrentUserProvider"/> scope so both
/// Row-Level Security (RLS) and ASP.NET Core authorization policies reflect instant account
/// deactivation and superuser status changes without trusting privileges stored in JWT claims.
/// Unauthenticated requests proceed cleanly under an anonymous (<see langword="null"/> user) scope.
/// </summary>
/// <param name="next">Next middleware delegate in the ASP.NET Core pipeline.</param>
public sealed class UserContextMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Verifies the authenticated user (when present) against <paramref name="repository"/>,
    /// rejects deactivated or deleted accounts with <c>401 Unauthorized</c>, and establishes
    /// the active <paramref name="currentUserProvider"/> scope before invoking the next middleware.
    /// </summary>
    /// <param name="context">Current HTTP context.</param>
    /// <param name="currentUserProvider">Provider holding the active user scope for the execution flow.</param>
    /// <param name="repository">Repository used to verify active account and superuser status against <c>users</c>.</param>
    public async Task InvokeAsync(HttpContext context, ICurrentUserProvider currentUserProvider, IRepository repository)
    {
        if (!TryExtractUserId(context.User, out long parsedUserId))
        {
            using IDisposable _ = currentUserProvider.BeginScope(null, isSuperuser: false);
            await next(context).ConfigureAwait(false);
            return;
        }

        UserEntity? user = await repository.GetAsync<UserEntity>(parsedUserId, context.RequestAborted).ConfigureAwait(false);
        if (user is null || !user.IsActive)
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity());
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        using IDisposable userScope = currentUserProvider.BeginScope(user.Id, user.IsSuperuser);
        await next(context).ConfigureAwait(false);
    }

    /// <summary>
    /// Attempts to extract a positive numeric user identifier from the <c>sub</c> or
    /// <see cref="ClaimTypes.NameIdentifier"/> claim of the supplied <paramref name="principal"/>.
    /// </summary>
    /// <param name="principal">Authenticated claims principal, if any.</param>
    /// <param name="userId">Extracted user identifier when parsing succeeds.</param>
    /// <returns><see langword="true"/> when a positive user identifier was extracted; otherwise <see langword="false"/>.</returns>
    public bool TryExtractUserId(ClaimsPrincipal? principal, out long userId)
    {
        userId = 0L;
        if (principal?.Identity?.IsAuthenticated != true)
            return false;

        string? rawValue = principal.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrWhiteSpace(rawValue)
            && long.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out userId)
            && userId > 0L;
    }
}
