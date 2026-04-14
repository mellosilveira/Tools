using MelloSilveiraTools.ExtensionMethods;
using MelloSilveiraTools.Infrastructure.Logger;
using MelloSilveiraTools.Infrastructure.Plugins.Models;
using MelloSilveiraTools.Infrastructure.Services.Plugins;

namespace MelloSilveiraTools.Application.Operations.Plugins.Load;

public class LoadPlugins(ILogger logger, IPluginService pluginService) : OperationBaseWithDefaultResponse<LoadPluginsRequest>(logger)
{
    protected override Task<OperationResponse> ProcessOperationAsync(LoadPluginsRequest request)
    {
        try
        {
            pluginService.LoadPluginsOnRuntime(request.Name, PluginVersion.SafeParse(request.Version));
            return OperationResponse.CreateSuccessOk().AsTask();
        }
        catch (Exception ex)
        {
            const string message = "Failed to load plugins.";
            Logger.Error(message, ex, new Dictionary<string, object> { { "Request", request } });
            return OperationResponse.CreateInternalServerError(message).AsTask();
        }
    }

    protected override Task<OperationResponse> ValidateOperationAsync(LoadPluginsRequest request)
        => OperationResponse
            .CreateSuccessOk()
            .AddErrorIf(!string.IsNullOrWhiteSpace(request.Version) && PluginVersion.TryParse(request.Version, out _), "")
            .AsTask();
}
