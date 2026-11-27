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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MelloSilveiraTools.Plugins.Application.Controllers;

/// <summary>
/// Exposes HTTP endpoints for inspecting and managing the plugin runtime (discovery, community submission,
/// moderation, loading, reloading, and cache management) with explicit endpoint authorization attributes.
/// </summary>
[Route("api/V1/plugins")]
public class PluginController : ControllerBase
{
    /// <summary>
    /// Returns sanitized summaries of registered runtime plugins from memory cache.
    /// </summary>
    /// <param name="command">Command that resolves the matching plugins.</param>
    /// <param name="request">Filter criteria applied to the plugin lookup.</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpGet]
    public async Task<ActionResult<ListedResult<PluginInfo>>> Get(
        [FromServices] GetPlugins command,
        [FromQuery] GetPluginsRequest request,
        CancellationToken cancellationToken = default)
        => await command.ExecuteAsync(request, cancellationToken).BuildHttpResponseAsync().ConfigureAwait(false);

    /// <summary>
    /// Inspects plugin binaries across the main storage partitions (<c>loaded</c>, <c>incoming</c>, <c>pending</c>,
    /// <c>archive</c>, <c>quarantine</c>, <c>rejected</c>), returning physical <see cref="Core.Files.Models.StorageEntryMetadata"/>,
    /// static PE/architecture validation status, and runtime type registration details (superuser only).
    /// </summary>
    /// <param name="command">Command performing storage partition inspection and assembly validation.</param>
    /// <param name="request">Optional name, version, load-state, and partition filters.</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    [Authorize(Policy = AuthorizationPolicies.SuperuserOnly)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpGet("inspect")]
    public async Task<ActionResult<ListedResult<PluginInfo>>> Inspect(
        [FromServices] InspectPlugins command,
        [FromQuery] InspectPluginsRequest request,
        CancellationToken cancellationToken = default)
        => await command.ExecuteAsync(request, cancellationToken).BuildHttpResponseAsync().ConfigureAwait(false);

    /// <summary>
    /// Lists plugin submissions visible to the current authenticated user (or all submissions for superusers).
    /// </summary>
    /// <param name="command">Command querying visible submissions.</param>
    /// <param name="request">Optional plugin name and status filter.</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpGet("submissions")]
    public async Task<ActionResult<ListedResult<PluginInfo>>> GetSubmissions(
        [FromServices] GetPluginSubmissions command,
        [FromQuery] GetPluginSubmissionsRequest request,
        CancellationToken cancellationToken = default)
        => await command.ExecuteAsync(request, cancellationToken).BuildHttpResponseAsync().ConfigureAwait(false);

    /// <summary>
    /// Cancels and deletes a pending plugin submission (author or superuser only).
    /// </summary>
    /// <param name="command">Command canceling the submission.</param>
    /// <param name="id">Submission identifier.</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpDelete("submissions/{id:long}")]
    public async Task<ActionResult<Result>> CancelSubmission(
        [FromServices] CancelPluginSubmission command,
        [FromRoute] long id,
        CancellationToken cancellationToken = default)
        => await command.ExecuteAsync(new CancelPluginSubmissionRequest { Id = id }, cancellationToken).BuildHttpResponseAsync().ConfigureAwait(false);

    /// <summary>
    /// Approves a pending plugin submission after SHA-256 verification, promotes it to <c>incoming/</c>, and loads it immediately.
    /// </summary>
    /// <param name="command">Command performing approval and runtime loading.</param>
    /// <param name="id">Submission identifier.</param>
    /// <param name="body">Optional reviewer notes.</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    [Authorize(Policy = AuthorizationPolicies.SuperuserOnly)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPost("submissions/{id:long}/approve")]
    public async Task<ActionResult<Result<PluginInfo>>> ApproveSubmission(
        [FromServices] ApprovePluginSubmission command,
        [FromRoute] long id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ReviewPluginSubmissionBody? body = null,
        CancellationToken cancellationToken = default)
        => await command.ExecuteAsync(new ReviewPluginSubmissionRequest { Id = id, ReviewNotes = body?.ReviewNotes }, cancellationToken).BuildHttpResponseAsync().ConfigureAwait(false);

    /// <summary>
    /// Rejects a pending plugin submission and relocates its binary to the <c>rejected/</c> partition.
    /// </summary>
    /// <param name="command">Command performing rejection.</param>
    /// <param name="id">Submission identifier.</param>
    /// <param name="body">Optional reviewer notes.</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    [Authorize(Policy = AuthorizationPolicies.SuperuserOnly)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPost("submissions/{id:long}/reject")]
    public async Task<ActionResult<Result<PluginInfo>>> RejectSubmission(
        [FromServices] RejectPluginSubmission command,
        [FromRoute] long id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ReviewPluginSubmissionBody? body = null,
        CancellationToken cancellationToken = default)
        => await command.ExecuteAsync(new ReviewPluginSubmissionRequest { Id = id, ReviewNotes = body?.ReviewNotes }, cancellationToken).BuildHttpResponseAsync().ConfigureAwait(false);

    /// <summary>
    /// Loads the plugins that match the supplied name and/or version.
    /// </summary>
    /// <param name="command">Command that performs the plugin load.</param>
    /// <param name="request">Filter identifying the plugins to load.</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    [Authorize(Policy = AuthorizationPolicies.SuperuserOnly)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPost("load")]
    public async Task<ActionResult<Result>> Load(
        [FromServices] LoadPlugins command,
        [FromQuery] PluginsRequest request,
        CancellationToken cancellationToken = default)
        => await command.ExecuteAsync(request, cancellationToken).BuildHttpResponseAsync().ConfigureAwait(false);

    /// <summary>
    /// Loads every plugin known to the host.
    /// </summary>
    /// <param name="command">Command that performs the plugin load.</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    [Authorize(Policy = AuthorizationPolicies.SuperuserOnly)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPost("load/all")]
    public async Task<ActionResult<Result>> LoadAll(
        [FromServices] LoadPlugins command,
        CancellationToken cancellationToken = default)
        => await command.ExecuteAsync(new PluginsRequest(), cancellationToken).BuildHttpResponseAsync().ConfigureAwait(false);

    /// <summary>
    /// Reloads plugins that match the supplied name and/or version, optionally forcing a reload.
    /// </summary>
    /// <param name="command">Command that performs the plugin reload.</param>
    /// <param name="request">Filter identifying the plugins to reload.</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    [Authorize(Policy = AuthorizationPolicies.SuperuserOnly)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPost("reload")]
    public async Task<ActionResult<Result>> Reload(
        [FromServices] ReloadPlugins command,
        [FromQuery] ReloadPluginsRequest request,
        CancellationToken cancellationToken = default)
        => await command.ExecuteAsync(request, cancellationToken).BuildHttpResponseAsync().ConfigureAwait(false);

    /// <summary>
    /// Reloads every plugin known to the host.
    /// </summary>
    /// <param name="command">Command that performs the plugin reload.</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    [Authorize(Policy = AuthorizationPolicies.SuperuserOnly)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPost("reload/all")]
    public async Task<ActionResult<Result>> ReloadAll(
        [FromServices] ReloadPlugins command,
        CancellationToken cancellationToken = default)
        => await command.ExecuteAsync(new ReloadPluginsRequest(), cancellationToken).BuildHttpResponseAsync().ConfigureAwait(false);

    /// <summary>
    /// Clears the plugin cache.
    /// </summary>
    /// <param name="command">Command that clears the plugin cache.</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    [Authorize(Policy = AuthorizationPolicies.SuperuserOnly)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpDelete("cache")]
    public async Task<ActionResult<Result>> ClearCache(
        [FromServices] ClearPluginCache command,
        CancellationToken cancellationToken = default)
        => await command.ExecuteAsync(cancellationToken).BuildHttpResponseAsync().ConfigureAwait(false);

    /// <summary>
    /// Persists the plugin cache to the target identified by the {target} route segment (e.g. file or database).
    /// </summary>
    /// <param name="command">Command that persists the plugin cache.</param>
    /// <param name="request">Filter identifying the plugins to persist.</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    [Authorize(Policy = AuthorizationPolicies.SuperuserOnly)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPost("cache/{target}/persist")]
    public async Task<ActionResult<Result>> PersistCache(
        [FromServices] PersistPluginCache command,
        [FromQuery] PluginsRequest request,
        CancellationToken cancellationToken = default)
        => await command.ExecuteAsync(request, cancellationToken).BuildHttpResponseAsync().ConfigureAwait(false);

    /// <summary>
    /// Restores the plugin cache from the target identified by the {target} route segment (e.g. file or database).
    /// </summary>
    /// <param name="command">Command that restores the plugin cache.</param>
    /// <param name="request">Filter identifying the plugins to restore.</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    [Authorize(Policy = AuthorizationPolicies.SuperuserOnly)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPost("cache/{target}/restore")]
    public async Task<ActionResult<Result>> RestoreCache(
        [FromServices] RestorePluginCache command,
        [FromQuery] PluginsRequest request,
        CancellationToken cancellationToken = default)
        => await command.ExecuteAsync(request, cancellationToken).BuildHttpResponseAsync().ConfigureAwait(false);

    /// <summary>
    /// Submits a community plugin assembly into the isolated <c>pending/</c> partition awaiting superuser moderation.
    /// </summary>
    /// <param name="command">Command that validates ownership/quotas, hashes, and stages the plugin submission.</param>
    /// <param name="file">The plugin assembly file (.dll).</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPost("submissions")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<Result<PluginInfo>>> Submit(
        [FromServices] SubmitPlugin command,
        IFormFile file,
        CancellationToken cancellationToken = default)
        => await command.ExecuteAsync(new UploadPluginRequest { File = file }, cancellationToken).BuildHttpResponseAsync().ConfigureAwait(false);

    /// <summary>
    /// Publishes a plugin assembly directly into <c>incoming/</c> with auto-approval (superuser only).
    /// </summary>
    /// <param name="command">Command that validates, hashes, and publishes the plugin directly.</param>
    /// <param name="file">The plugin assembly file (.dll).</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    [Authorize(Policy = AuthorizationPolicies.SuperuserOnly)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<Result<PluginInfo>>> Upload(
        [FromServices] UploadPlugin command,
        IFormFile file,
        CancellationToken cancellationToken = default)
        => await command.ExecuteAsync(new UploadPluginRequest { File = file }, cancellationToken).BuildHttpResponseAsync().ConfigureAwait(false);
}
