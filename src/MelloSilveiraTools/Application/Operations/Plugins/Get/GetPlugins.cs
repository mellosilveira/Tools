using MelloSilveiraTools.ExtensionMethods;
using MelloSilveiraTools.Infrastructure.Logger;
using MelloSilveiraTools.Infrastructure.Plugins.Models;
using MelloSilveiraTools.Infrastructure.Services.Plugins;

namespace MelloSilveiraTools.Application.Operations.Plugins.Get;

/// <summary>
/// Operation that retrieves registered plugins matching the supplied name, version and load-state filters.
/// </summary>
public class GetPlugins(ILogger logger, IPluginService pluginService) : OperationBase<GetPluginsRequest, GetPluginsResponse>(logger)
{
    /// <inheritdoc />
    protected override Task<GetPluginsResponse> ProcessOperationAsync(GetPluginsRequest request)
        => OperationResponse
            .CreateListSuccessOk<GetPluginsResponse, RegisteredPlugin>(pluginService
                .GetPlugins(request.Name, PluginVersion.SafeParse(request.Version))
                .Where(registered => request.FullyLoaded is null || registered.IsFullyLoaded == request.FullyLoaded)
                .ToArray())
            .AsTask();

    /// <inheritdoc />
    protected override Task<GetPluginsResponse> ValidateOperationAsync(GetPluginsRequest request)
        => OperationResponse
            .CreateSuccessOk<GetPluginsResponse>()
            .AddErrorIf(!string.IsNullOrWhiteSpace(request.Version) && PluginVersion.TryParse(request.Version, out _), "Invalid version.")
            .AsTask();
}
