using MelloSilveiraTools.Core.Logger;
using MelloSilveiraTools.Database.ExtensionMethods;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Services;
using MelloSilveiraTools.WebApi.Application.Operations;
using MelloSilveiraTools.WebApi.ExtensionMethods;

namespace MelloSilveiraTools.Plugins.Application.Operations.Get;

/// <summary>
/// Operation that retrieves registered plugins matching the supplied name, version and load-state filters.
/// </summary>
public class GetPlugins(ILogger logger, IPluginService pluginService) : ListedOperationBase<GetPluginsRequest, RegisteredPlugin>(logger)
{
    /// <inheritdoc />
    protected override Task<ListedOperationResponse<RegisteredPlugin>> ProcessOperationAsync(GetPluginsRequest request)
        => OperationResponse
            .CreateListedSuccessOk(pluginService
                .GetPlugins(request.Name, PluginVersion.SafeParse(request.Version))
                .Where(registered => request.FullyLoaded is null || registered.IsFullyLoaded == request.FullyLoaded)
                .ToArray())
            .AsTask();

    /// <inheritdoc />
    protected override Task<ListedOperationResponse<RegisteredPlugin>> ValidateOperationAsync(GetPluginsRequest request)
        => OperationResponse
            .CreateListedSuccessOk<RegisteredPlugin>()
            .AddErrorIf(!string.IsNullOrWhiteSpace(request.Version) && PluginVersion.TryParse(request.Version, out _), "Invalid version.")
            .AsTask();
}
