namespace MelloSilveiraTools.WebApi.Authentication;

/// <summary>
/// Canonical ASP.NET Core authorization policy names shared across Web API hosts, controllers, and minimal APIs.
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>
    /// Policy requiring an authenticated user whose live database record has <c>is_superuser = TRUE</c>.
    /// </summary>
    public const string SuperuserOnly = "SuperuserOnly";
}
