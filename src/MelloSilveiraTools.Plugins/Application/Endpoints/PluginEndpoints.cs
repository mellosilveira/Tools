using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Plugins.Application.Commands;
using MelloSilveiraTools.Plugins.Application.Commands.Cache;
using MelloSilveiraTools.Plugins.Application.Commands.Get;
using MelloSilveiraTools.Plugins.Application.Commands.Load;
using MelloSilveiraTools.Plugins.Application.Commands.Reload;
using MelloSilveiraTools.Plugins.Application.Commands.Submissions;
using MelloSilveiraTools.Plugins.Application.Commands.Submissions.Cancel;
using MelloSilveiraTools.Plugins.Application.Commands.Submissions.Get;
using MelloSilveiraTools.Plugins.Application.Commands.Submissions.Review;
using MelloSilveiraTools.Plugins.Application.Commands.Upload;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.WebApi.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace MelloSilveiraTools.Plugins.Application.Endpoints;

/// <summary>
/// Maps the same surface as <see cref="Controllers.PluginController"/> using minimal APIs.
/// Hosts that don't wire <c>app.MapControllers()</c> can reach the plugin runtime through this extension instead.
/// </summary>
public static class PluginEndpoints
{
    /// <summary>
    /// Maps every plugin-management and community-submission endpoint under <paramref name="pattern"/>.
    /// <list type="bullet">
    /// <item><description><c>GET /</c> stays open to all users and reads sanitized <see cref="PluginInfo"/> summaries from in-memory cache only.</description></item>
    /// <item><description><c>POST /submissions</c>, <c>GET /submissions</c>, and <c>DELETE /submissions/{id}</c> serve authenticated community uploads and self-service cancellation (protected by <paramref name="requireAuthForSubmissions"/>).</description></item>
    /// <item><description><c>GET /inspect</c>, <c>POST /upload</c>, <c>POST /submissions/{id}/approve</c>, <c>/reject</c>, <c>/load*</c>, <c>/reload*</c>, and <c>/cache*</c> are restricted via <paramref name="managementAuthorizationPolicy"/>.</description></item>
    /// </list>
    /// </summary>
    /// <param name="builder">Endpoint route builder used to register the endpoints.</param>
    /// <param name="pattern">Route prefix; defaults to <c>"/api/v1/plugins"</c>.</param>
    /// <param name="managementAuthorizationPolicy">
    /// ASP.NET Core authorization policy name applied to superuser direct upload, moderation, and runtime-mutating endpoints.
    /// Defaults to <see cref="AuthorizationPolicies.SuperuserOnly"/> (fail-closed); pass <see langword="null"/> only in isolated non-auth tests.
    /// </param>
    /// <param name="requireAuthForSubmissions">
    /// When <see langword="true"/> (default, fail-closed), requires an authenticated user on <c>POST /submissions</c>,
    /// <c>GET /submissions</c>, and <c>DELETE /submissions/{id}</c> before entering the endpoint handler.
    /// </param>
    /// <param name="uploadRateLimitPolicy">
    /// Optional ASP.NET Core rate-limiter policy name (e.g. <see cref="MelloSilveiraTools.WebApi.Application.RateLimiting.RateLimitPolicies.PluginUpload"/>)
    /// applied to <c>POST /submissions</c> and <c>POST /upload</c>.
    /// </param>
    /// <returns>The created <see cref="RouteGroupBuilder"/> so callers can chain additional metadata.</returns>
    public static RouteGroupBuilder MapPluginEndpoints(
        this IEndpointRouteBuilder builder,
        string pattern = "/api/v1/plugins",
        string? managementAuthorizationPolicy = AuthorizationPolicies.SuperuserOnly,
        bool requireAuthForSubmissions = true,
        string? uploadRateLimitPolicy = null)
    {
        RouteGroupBuilder group = builder.MapGroup(pattern).WithTags("Plugins");

        group
            .MapGet("/", async (HttpContext httpContext, [FromServices] GetPlugins operation, [AsParameters] GetPluginsRequest request) => await operation.ExecuteAsync(request, httpContext.RequestAborted).ToHttpResultAsync().ConfigureAwait(false))
            .AllowAnonymous()
            .Produces<ListedResult<PluginInfo>>(StatusCodes.Status200OK)
            .Produces<Result>(StatusCodes.Status400BadRequest)
            .Produces<Result>(StatusCodes.Status500InternalServerError)
            .WithName("GetPlugins")
            .WithSummary("Returns sanitized summaries of loaded runtime plugins from memory cache.");

        RouteHandlerBuilder submitEndpoint = group
            .MapPost("/submissions", async (HttpContext httpContext, [FromServices] SubmitPlugin operation, IFormFile file) => await operation.ExecuteAsync(new UploadPluginRequest { File = file }, httpContext.RequestAborted).ToHttpResultAsync().ConfigureAwait(false))
            .DisableAntiforgery()
            .Produces<Result<PluginInfo>>(StatusCodes.Status200OK)
            .Produces<Result<PluginInfo>>(StatusCodes.Status400BadRequest)
            .Produces<Result<PluginInfo>>(StatusCodes.Status401Unauthorized)
            .Produces<Result<PluginInfo>>(StatusCodes.Status403Forbidden)
            .Produces<Result<PluginInfo>>(StatusCodes.Status409Conflict)
            .Produces<Result>(StatusCodes.Status500InternalServerError)
            .WithName("SubmitPlugin")
            .WithSummary("Submits a community plugin assembly into pending/ awaiting superuser review.");

        RouteHandlerBuilder uploadEndpoint = group
            .MapPost("/upload", async (HttpContext httpContext, [FromServices] UploadPlugin operation, IFormFile file) => await operation.ExecuteAsync(new UploadPluginRequest { File = file }, httpContext.RequestAborted).ToHttpResultAsync().ConfigureAwait(false))
            .DisableAntiforgery()
            .Produces<Result<PluginInfo>>(StatusCodes.Status200OK)
            .Produces<Result<PluginInfo>>(StatusCodes.Status400BadRequest)
            .Produces<Result<PluginInfo>>(StatusCodes.Status401Unauthorized)
            .Produces<Result<PluginInfo>>(StatusCodes.Status403Forbidden)
            .Produces<Result<PluginInfo>>(StatusCodes.Status409Conflict)
            .Produces<Result>(StatusCodes.Status500InternalServerError)
            .WithName("UploadPlugin")
            .WithSummary("Publishes a plugin assembly directly into incoming/ with auto-approval (superuser only).");

        if (!string.IsNullOrWhiteSpace(uploadRateLimitPolicy))
        {
            submitEndpoint.RequireRateLimiting(uploadRateLimitPolicy);
            uploadEndpoint.RequireRateLimiting(uploadRateLimitPolicy);
        }

        RouteHandlerBuilder[] submissionEndpoints =
        [
            submitEndpoint,

            group
                .MapGet("/submissions", async (HttpContext httpContext, [FromServices] GetPluginSubmissions operation, [AsParameters] GetPluginSubmissionsRequest request) => await operation.ExecuteAsync(request, httpContext.RequestAborted).ToHttpResultAsync().ConfigureAwait(false))
                .Produces<ListedResult<PluginInfo>>(StatusCodes.Status200OK)
                .Produces<Result>(StatusCodes.Status401Unauthorized)
                .Produces<Result>(StatusCodes.Status500InternalServerError)
                .WithName("GetPluginSubmissions")
                .WithSummary("Lists plugin submissions visible to the caller (approved + own submissions for regular users; full queue for superusers)."),

            group
                .MapDelete("/submissions/{id:long}", async (HttpContext httpContext, [FromServices] CancelPluginSubmission operation, long id) => await operation.ExecuteAsync(new CancelPluginSubmissionRequest { Id = id }, httpContext.RequestAborted).ToHttpResultAsync().ConfigureAwait(false))
                .Produces<Result>(StatusCodes.Status200OK)
                .Produces<Result>(StatusCodes.Status401Unauthorized)
                .Produces<Result>(StatusCodes.Status403Forbidden)
                .Produces<Result>(StatusCodes.Status404NotFound)
                .Produces<Result>(StatusCodes.Status409Conflict)
                .Produces<Result>(StatusCodes.Status500InternalServerError)
                .WithName("CancelPluginSubmission")
                .WithSummary("Cancels and deletes a pending plugin submission (author or superuser only; never allowed once approved).")
        ];

        if (requireAuthForSubmissions)
        {
            foreach (RouteHandlerBuilder endpoint in submissionEndpoints)
                endpoint.RequireAuthorization();
        }

        RouteHandlerBuilder[] managementEndpoints =
        [
            group
                .MapGet("/inspect", async (HttpContext httpContext, [FromServices] InspectPlugins operation, [AsParameters] InspectPluginsRequest request) => await operation.ExecuteAsync(request, httpContext.RequestAborted).ToHttpResultAsync().ConfigureAwait(false))
                .Produces<ListedResult<PluginInfo>>(StatusCodes.Status200OK)
                .Produces<Result>(StatusCodes.Status400BadRequest)
                .Produces<Result>(StatusCodes.Status401Unauthorized)
                .Produces<Result>(StatusCodes.Status403Forbidden)
                .Produces<Result>(StatusCodes.Status500InternalServerError)
                .WithName("InspectPlugins")
                .WithSummary("Inspects plugin assemblies across main storage partitions with StorageEntryMetadata, validation status, and type loading state (superuser only)."),

            uploadEndpoint,
            group
                .MapPost("/submissions/{id:long}/approve", async (HttpContext httpContext, [FromServices] ApprovePluginSubmission operation, long id, [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] ReviewPluginSubmissionBody? body) => await operation.ExecuteAsync(new ReviewPluginSubmissionRequest { Id = id, ReviewNotes = body?.ReviewNotes }, httpContext.RequestAborted).ToHttpResultAsync().ConfigureAwait(false))
                .Produces<Result<PluginInfo>>(StatusCodes.Status200OK)
                .Produces<Result<PluginInfo>>(StatusCodes.Status403Forbidden)
                .Produces<Result<PluginInfo>>(StatusCodes.Status404NotFound)
                .Produces<Result<PluginInfo>>(StatusCodes.Status409Conflict)
                .Produces<Result>(StatusCodes.Status500InternalServerError)
                .WithName("ApprovePluginSubmission")
                .WithSummary("Approves a pending plugin submission after SHA-256 verification, promotes it to incoming/, and loads it immediately."),

            group
                .MapPost("/submissions/{id:long}/reject", async (HttpContext httpContext, [FromServices] RejectPluginSubmission operation, long id, [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] ReviewPluginSubmissionBody? body) => await operation.ExecuteAsync(new ReviewPluginSubmissionRequest { Id = id, ReviewNotes = body?.ReviewNotes }, httpContext.RequestAborted).ToHttpResultAsync().ConfigureAwait(false))
                .Produces<Result<PluginInfo>>(StatusCodes.Status200OK)
                .Produces<Result<PluginInfo>>(StatusCodes.Status403Forbidden)
                .Produces<Result<PluginInfo>>(StatusCodes.Status404NotFound)
                .Produces<Result<PluginInfo>>(StatusCodes.Status409Conflict)
                .Produces<Result>(StatusCodes.Status500InternalServerError)
                .WithName("RejectPluginSubmission")
                .WithSummary("Rejects a pending plugin submission and moves its binary to the rejected partition."),

            group
                .MapPost("/load", async (HttpContext httpContext, [FromServices] LoadPlugins operation, [AsParameters] PluginsRequest request) => await operation.ExecuteAsync(request, httpContext.RequestAborted).ToHttpResultAsync().ConfigureAwait(false))
                .Produces<Result>(StatusCodes.Status200OK)
                .Produces<Result>(StatusCodes.Status400BadRequest)
                .Produces<Result>(StatusCodes.Status500InternalServerError)
                .WithName("LoadPlugins")
                .WithSummary("Loads plugins matching the supplied name and/or version."),

            group
                .MapPost("/load/all", async (HttpContext httpContext, [FromServices] LoadPlugins operation) => await operation.ExecuteAsync(new PluginsRequest(), httpContext.RequestAborted).ToHttpResultAsync().ConfigureAwait(false))
                .Produces<Result>(StatusCodes.Status200OK)
                .Produces<Result>(StatusCodes.Status500InternalServerError)
                .WithName("LoadAllPlugins")
                .WithSummary("Loads every plugin known to the host."),

            group
                .MapPost("/reload", async (HttpContext httpContext, [FromServices] ReloadPlugins operation, [AsParameters] ReloadPluginsRequest request) => await operation.ExecuteAsync(request, httpContext.RequestAborted).ToHttpResultAsync().ConfigureAwait(false))
                .Produces<Result>(StatusCodes.Status200OK)
                .Produces<Result>(StatusCodes.Status400BadRequest)
                .Produces<Result>(StatusCodes.Status500InternalServerError)
                .WithName("ReloadPlugins")
                .WithSummary("Reloads plugins matching the supplied name and/or version."),

            group
                .MapPost("/reload/all", async (HttpContext httpContext, [FromServices] ReloadPlugins operation) => await operation.ExecuteAsync(new ReloadPluginsRequest(), httpContext.RequestAborted).ToHttpResultAsync().ConfigureAwait(false))
                .Produces<Result>(StatusCodes.Status200OK)
                .Produces<Result>(StatusCodes.Status500InternalServerError)
                .WithName("ReloadAllPlugins")
                .WithSummary("Reloads every plugin known to the host."),

            group
                .MapDelete("/cache", async (HttpContext httpContext, [FromServices] ClearPluginCache operation) => await operation.ExecuteAsync(httpContext.RequestAborted).ToHttpResultAsync().ConfigureAwait(false))
                .Produces<Result>(StatusCodes.Status200OK)
                .Produces<Result>(StatusCodes.Status400BadRequest)
                .Produces<Result>(StatusCodes.Status500InternalServerError)
                .WithName("ClearPluginCache")
                .WithSummary("Clears the plugin cache."),

            group
                .MapPost("/cache/{target}/persist", async (HttpContext httpContext, [FromServices] PersistPluginCache operation, [AsParameters] PluginsRequest request) => await operation.ExecuteAsync(request, httpContext.RequestAborted).ToHttpResultAsync().ConfigureAwait(false))
                .Produces<Result>(StatusCodes.Status200OK)
                .Produces<Result>(StatusCodes.Status400BadRequest)
                .Produces<Result>(StatusCodes.Status500InternalServerError)
                .WithName("PersistPluginCache")
                .WithSummary("Persists the plugin cache to the target identified by the {target} route segment."),

            group
                .MapPost("/cache/{target}/restore", async (HttpContext httpContext, [FromServices] RestorePluginCache operation, [AsParameters] PluginsRequest request) => await operation.ExecuteAsync(request, httpContext.RequestAborted).ToHttpResultAsync().ConfigureAwait(false))
                .Produces<Result>(StatusCodes.Status200OK)
                .Produces<Result>(StatusCodes.Status400BadRequest)
                .Produces<Result>(StatusCodes.Status500InternalServerError)
                .WithName("RestorePluginCache")
                .WithSummary("Restores the plugin cache from the target identified by the {target} route segment.")
        ];

        if (!string.IsNullOrWhiteSpace(managementAuthorizationPolicy))
        {
            foreach (RouteHandlerBuilder endpoint in managementEndpoints)
                endpoint.RequireAuthorization(managementAuthorizationPolicy);
        }

        return group;
    }
}
