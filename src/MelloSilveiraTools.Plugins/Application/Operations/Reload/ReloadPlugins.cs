using MelloSilveiraTools.Core.Infrastructure.Logger;
using MelloSilveiraTools.Database.ExtensionMethods;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Services;
using MelloSilveiraTools.WebApi.Application.Operations;
using MelloSilveiraTools.WebApi.ExtensionMethods;

namespace MelloSilveiraTools.Plugins.Application.Operations.Reload;

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
            Logger.Error(message, ex, new Dictionary<string, object?> { { "Request", request } });
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
