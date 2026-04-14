using MelloSilveiraTools.Application.Operations;
using MelloSilveiraTools.Application.Operations.Plugins.Cache;
using MelloSilveiraTools.Application.Operations.Plugins.Get;
using MelloSilveiraTools.Application.Operations.Plugins.Load;
using MelloSilveiraTools.Application.Operations.Plugins.Reload;
using MelloSilveiraTools.ExtensionMethods;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MelloSilveiraTools.Application.Controllers;

[Route("api/V1/plugins")]
public class PluginController : ControllerBase
{
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

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPost("load/all")]
    public async Task<ActionResult<OperationResponse>> LoadAll(
        [FromServices] LoadPlugins operation)
    {
        OperationResponse response = await operation.ProcessAsync(new LoadPluginsRequest()).ConfigureAwait(false);
        return response.BuildHttpResponse();
    }

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

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPost("reload/all")]
    public async Task<ActionResult<OperationResponse>> ReloadAll(
        [FromServices] ReloadPlugins operation)
    {
        OperationResponse response = await operation.ProcessAsync(new ReloadPluginsRequest()).ConfigureAwait(false);
        return response.BuildHttpResponse();
    }

    // ------------------------ FALTAM ESSAS

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpDelete("cache")]
    public async Task<ActionResult<OperationResponse>> ClearCache(
        [FromServices] ClearPluginCache operation,
        [FromQuery] string stage)
    {
        ClearPluginCacheRequest request = new() { Stage = stage };
        OperationResponse response = await operation.ProcessAsync(request).ConfigureAwait(false);
        return response.BuildHttpResponse();
    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPost("cache/persist")]
    public async Task<ActionResult<OperationResponse>> PersistCache(
        [FromServices] PersistPluginCache operation,
        [FromQuery] string target)
    {
        PersistPluginCacheRequest request = new() { Target = target };
        OperationResponse response = await operation.ProcessAsync(request).ConfigureAwait(false);
        return response.BuildHttpResponse();
    }
}
