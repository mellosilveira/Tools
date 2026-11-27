namespace MelloSilveiraTools.WebApi.Application.RateLimiting;

/// <summary>
/// Canonical ASP.NET Core rate-limiting policy names shared across Web API hosts, controllers, and minimal APIs.
/// </summary>
public static class RateLimitPolicies
{
    /// <summary>
    /// Per-IP fixed-window policy protecting authentication and user registration endpoints.
    /// </summary>
    public const string Auth = "auth";

    /// <summary>
    /// Per-IP fixed-window policy throttling anonymous compute and storage endpoints while allowing authenticated callers full throughput.
    /// </summary>
    public const string AnonymousCompute = "anonymous-compute";

    /// <summary>
    /// Per-user (or per-IP fallback) fixed-window policy throttling plugin assembly uploads and community submissions.
    /// </summary>
    public const string PluginUpload = "plugin-upload";
}
