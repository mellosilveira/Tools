using MelloSilveiraTools.ExtensionMethods;
using MelloSilveiraTools.Infrastructure.Logger;
using MelloSilveiraTools.Infrastructure.Plugins.Models;
using MelloSilveiraTools.Infrastructure.Services.Plugins;

namespace MelloSilveiraTools.Application.Operations.Plugins.Get;

public class GetPlugins(ILogger logger, IPluginService pluginService) : OperationBase<GetPluginsRequest, GetPluginsResponse>(logger)
{
    protected override Task<GetPluginsResponse> ProcessOperationAsync(GetPluginsRequest request)
        => OperationResponse
            .CreateListSuccessOk<GetPluginsResponse, PluginInfo>(pluginService
                .GetPlugins(request.Name, PluginVersion.SafeParse(request.Version))
                .Where(ti => request.FullyLoaded is null || ti.IsFullyLoaded == request.FullyLoaded)
                .ToArray())
            .AsTask();

    protected override Task<GetPluginsResponse> ValidateOperationAsync(GetPluginsRequest request)
        => OperationResponse
            .CreateSuccessOk<GetPluginsResponse>()
            .AddErrorIf(!string.IsNullOrWhiteSpace(request.Version) && PluginVersion.TryParse(request.Version, out _), "")
            .AsTask();
}
