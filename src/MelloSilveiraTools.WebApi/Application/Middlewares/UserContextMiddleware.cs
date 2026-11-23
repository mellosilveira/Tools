using MelloSilveiraTools.Database.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Globalization;
using System.Security.Claims;

namespace MelloSilveiraTools.WebApi.Application.Middlewares;

/// <summary>
/// Captures the authenticated HTTP <see cref="HttpContext.User"/> identifier into the injected
/// <see cref="ICurrentUserProvider"/> scope so PostgreSQL Row-Level Security (RLS) session scoping is active
/// throughout the request and flows automatically into background <see cref="Task"/> and TPL Dataflow
/// executions started during the request.
/// </summary>
/// <param name="next">Next middleware delegate in the ASP.NET Core pipeline.</param>
public sealed class UserContextMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Captures the authenticated user identifier into <paramref name="currentUserProvider"/> and invokes the next middleware.
    /// </summary>
    /// <param name="context">Current HTTP context.</param>
    /// <param name="currentUserProvider">Provider holding the active user scope for the execution flow.</param>
    public async Task InvokeAsync(HttpContext context, ICurrentUserProvider currentUserProvider)
    {
        long? userId = TryExtractUserId(context.User, out long parsedUserId) ? parsedUserId : null;
        using IDisposable _ = currentUserProvider.BeginScope(userId);
        await next(context).ConfigureAwait(false);
    }

    /// <summary>
    /// Attempts to extract a positive numeric user identifier from the <c>sub</c> or
    /// <see cref="ClaimTypes.NameIdentifier"/> claim of the supplied <paramref name="principal"/>.
    /// </summary>
    /// <param name="principal">Authenticated claims principal, if any.</param>
    /// <param name="userId">Extracted user identifier when parsing succeeds.</param>
    /// <returns><see langword="true"/> when a positive user identifier was extracted; otherwise <see langword="false"/>.</returns>
    public static bool TryExtractUserId(ClaimsPrincipal? principal, out long userId)
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
