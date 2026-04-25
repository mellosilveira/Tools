using MelloSilveiraTools.ExtensionMethods;
using MelloSilveiraTools.Infrastructure.Logger;
using MelloSilveiraTools.Infrastructure.Plugins.Models;
using MelloSilveiraTools.Infrastructure.Services.Plugins;

namespace MelloSilveiraTools.Application.Operations.Plugins.Reload;

/// <summary>
/// Operation that reloads plugins into the runtime based on the supplied name/version filter and force flag.
/// </summary>
public class ReloadPlugins(ILogger logger, IPluginService pluginService) : OperationBaseWithDefaultResponse<ReloadPluginsRequest>(logger)
{
    /// <inheritdoc />
    protected override Task<OperationResponse> ProcessOperationAsync(ReloadPluginsRequest request)
    {
        try
        {
            pluginService.ReloadPluginsOnRuntime(request.Force, request.Name, PluginVersion.SafeParse(request.Version));
            return OperationResponse.CreateSuccessOk().AsTask();
        }
        catch (Exception ex)
        {
            const string message = "Failed to reload plugins.";
            Logger.Error(message, ex, new Dictionary<string, object> { { "Request", request } });
            return OperationResponse.CreateInternalServerError(message).AsTask();
        }
    }

    /// <inheritdoc />
    protected override Task<OperationResponse> ValidateOperationAsync(ReloadPluginsRequest request)
        => OperationResponse
            .CreateSuccessOk()
            .AddErrorIf(!string.IsNullOrWhiteSpace(request.Version) && PluginVersion.TryParse(request.Version, out _), "")
            .AsTask();
}
