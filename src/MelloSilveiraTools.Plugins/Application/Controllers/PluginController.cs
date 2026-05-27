using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Plugins.Application.Commands;
using MelloSilveiraTools.Plugins.Application.Commands.Cache;
using MelloSilveiraTools.Plugins.Application.Commands.Get;
using MelloSilveiraTools.Plugins.Application.Commands.Load;
using MelloSilveiraTools.Plugins.Application.Commands.Reload;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.WebApi.ExtensionMethods;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MelloSilveiraTools.Plugins.Application.Controllers;

/// <summary>
/// Exposes HTTP endpoints for inspecting and managing the plugin runtime (discovery, loading, reloading and cache management).
/// </summary>
[Route("api/V1/plugins")]
public class PluginController : ControllerBase
{
    /// <summary>
    /// Returns the plugins currently known to the host, optionally filtered by name, version and load state.
    /// </summary>
    /// <param name="operation">Operation that resolves the matching plugins.</param>
    /// <param name="request">Filter criteria applied to the plugin lookup.</param>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpGet]
    public async Task<ActionResult<ListedResult<RegisteredPlugin>>> Get([FromServices] GetPlugins operation, [FromQuery] GetPluginsRequest request)
        => await operation.ExecuteAsync(request).BuildHttpResponseAsync().ConfigureAwait(false);

    /// <summary>
    /// Loads the plugins that match the supplied name and/or version.
    /// </summary>
    /// <param name="operation">Operation that performs the plugin load.</param>
    /// <param name="request">Filter identifying the plugins to load.</param>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPost("load")]
    public async Task<ActionResult<Result>> Load([FromServices] LoadPlugins operation, [FromQuery] PluginsRequest request)
        => await operation.ExecuteAsync(request).BuildHttpResponseAsync().ConfigureAwait(false);

    /// <summary>
    /// Loads every plugin known to the host.
    /// </summary>
    /// <param name="operation">Operation that performs the plugin load.</param>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPost("load/all")]
    public async Task<ActionResult<Result>> LoadAll([FromServices] LoadPlugins operation)
        => await operation.ExecuteAsync(new PluginsRequest()).BuildHttpResponseAsync().ConfigureAwait(false);

    /// <summary>
    /// Reloads plugins that match the supplied name and/or version, optionally forcing a reload.
    /// </summary>
    /// <param name="operation">Operation that performs the plugin reload.</param>
    /// <param name="request">Filter identifying the plugins to reload.</param>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPost("reload")]
    public async Task<ActionResult<Result>> Reload([FromServices] ReloadPlugins operation, [FromQuery] ReloadPluginsRequest request)
        => await operation.ExecuteAsync(request).BuildHttpResponseAsync().ConfigureAwait(false);

    /// <summary>
    /// Reloads every plugin known to the host.
    /// </summary>
    /// <param name="operation">Operation that performs the plugin reload.</param>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPost("reload/all")]
    public async Task<ActionResult<Result>> ReloadAll([FromServices] ReloadPlugins operation)
        => await operation.ExecuteAsync(new ReloadPluginsRequest()).BuildHttpResponseAsync().ConfigureAwait(false);

    /// <summary>
    /// Clears the plugin cache.
    /// </summary>
    /// <param name="operation">Operation that clears the plugin cache.</param>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpDelete("cache")]
    public async Task<ActionResult<Result>> ClearCache([FromServices] ClearPluginCache operation)
        => await operation.ExecuteAsync().BuildHttpResponseAsync().ConfigureAwait(false);

    /// <summary>
    /// Persists the plugin cache to the target identified by the {target} route segment (e.g. file or database).
    /// </summary>
    /// <param name="operation">Operation that persists the plugin cache.</param>
    /// <param name="request">Filter identifying the plugins to persist.</param>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPost("cache/{target}/persist")]
    public async Task<ActionResult<Result>> PersistCache([FromServices] PersistPluginCache operation, [FromQuery] PluginsRequest request)
        => await operation.ExecuteAsync(request).BuildHttpResponseAsync().ConfigureAwait(false);

    /// <summary>
    /// Restores the plugin cache from the target identified by the {target} route segment (e.g. file or database).
    /// </summary>
    /// <param name="operation">Operation that restores the plugin cache.</param>
    /// <param name="request">Filter identifying the plugins to restore.</param>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPost("cache/{target}/restore")]
    public async Task<ActionResult<Result>> RestoreCache([FromServices] RestorePluginCache operation, [FromQuery] PluginsRequest request)
        => await operation.ExecuteAsync(request).BuildHttpResponseAsync().ConfigureAwait(false);
}
