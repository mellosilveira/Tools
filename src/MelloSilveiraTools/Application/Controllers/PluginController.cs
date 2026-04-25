using MelloSilveiraTools.Application.Operations;
using MelloSilveiraTools.Application.Operations.Plugins.Cache;
using MelloSilveiraTools.Application.Operations.Plugins.Get;
using MelloSilveiraTools.Application.Operations.Plugins.Load;
using MelloSilveiraTools.Application.Operations.Plugins.Reload;
using MelloSilveiraTools.ExtensionMethods;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MelloSilveiraTools.Application.Controllers;

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
    public async Task<ActionResult<GetPluginsResponse>> Get(
        [FromServices] GetPlugins operation,
        [FromQuery] GetPluginsRequest request)
    {
        GetPluginsResponse response = await operation.ProcessAsync(request).ConfigureAwait(false);
        return response.BuildHttpResponse();
    }

    /// <summary>
    /// Loads the plugins that match the supplied name and/or version.
    /// </summary>
    /// <param name="operation">Operation that performs the plugin load.</param>
    /// <param name="request">Filter identifying the plugins to load.</param>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPost("load")]
    public async Task<ActionResult<OperationResponse>> Load(
        [FromServices] LoadPlugins operation,
        [FromQuery] LoadPluginsRequest request)
    {
        OperationResponse response = await operation.ProcessAsync(request).ConfigureAwait(false);
        return response.BuildHttpResponse();
    }

    /// <summary>
    /// Loads every plugin known to the host.
    /// </summary>
    /// <param name="operation">Operation that performs the plugin load.</param>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPost("load/all")]
    public async Task<ActionResult<OperationResponse>> LoadAll(
        [FromServices] LoadPlugins operation)
    {
        OperationResponse response = await operation.ProcessAsync(new LoadPluginsRequest()).ConfigureAwait(false);
        return response.BuildHttpResponse();
    }

    /// <summary>
    /// Reloads plugins that match the supplied name and/or version, optionally forcing a reload.
    /// </summary>
    /// <param name="operation">Operation that performs the plugin reload.</param>
    /// <param name="request">Filter identifying the plugins to reload.</param>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPost("reload")]
    public async Task<ActionResult<OperationResponse>> Reload(
        [FromServices] ReloadPlugins operation,
        [FromQuery] ReloadPluginsRequest request)
    {
        OperationResponse response = await operation.ProcessAsync(request).ConfigureAwait(false);
        return response.BuildHttpResponse();
    }

    /// <summary>
    /// Reloads every plugin known to the host.
    /// </summary>
    /// <param name="operation">Operation that performs the plugin reload.</param>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPost("reload/all")]
    public async Task<ActionResult<OperationResponse>> ReloadAll(
        [FromServices] ReloadPlugins operation)
    {
        OperationResponse response = await operation.ProcessAsync(new ReloadPluginsRequest()).ConfigureAwait(false);
        return response.BuildHttpResponse();
    }

    /// <summary>
    /// Clears the plugin cache.
    /// </summary>
    /// <param name="operation">Operation that clears the plugin cache.</param>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpDelete("cache")]
    public async Task<ActionResult<OperationResponse>> ClearCache([FromServices] ClearPluginCache operation)
    {
        OperationResponse response = await operation.ProcessAsync().ConfigureAwait(false);
        return response.BuildHttpResponse();
    }

    /// <summary>
    /// Persists the plugin cache to the target identified by the {target} route segment (e.g. file or database).
    /// </summary>
    /// <param name="operation">Operation that persists the plugin cache.</param>
    /// <param name="request">Filter identifying the plugins to persist.</param>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPost("cache/{target}/persist")]
    public async Task<ActionResult<OperationResponse>> PersistCache(
        [FromServices] PersistPluginCache operation,
        [FromQuery] PersistPluginCacheRequest request)
    {
        OperationResponse response = await operation.ProcessAsync(request).ConfigureAwait(false);
        return response.BuildHttpResponse();
    }

    /// <summary>
    /// Restores the plugin cache from the target identified by the {target} route segment (e.g. file or database).
    /// </summary>
    /// <param name="operation">Operation that restores the plugin cache.</param>
    /// <param name="request">Filter identifying the plugins to restore.</param>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPost("cache/{target}/restore")]
    public async Task<ActionResult<OperationResponse>> RestoreCache(
        [FromServices] RestorePluginCache operation,
        [FromQuery] RestorePluginCacheRequest request)
    {
        OperationResponse response = await operation.ProcessAsync(request).ConfigureAwait(false);
        return response.BuildHttpResponse();
    }
}
