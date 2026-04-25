using MelloSilveiraTools.Core.Infrastructure.Logger;
using MelloSilveiraTools.Database.ExtensionMethods;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Services;
using MelloSilveiraTools.WebApi.Application.Operations;
using MelloSilveiraTools.WebApi.ExtensionMethods;

namespace MelloSilveiraTools.Plugins.Application.Operations.Load;

/// <summary>
/// Operation that loads plugins into the runtime based on the supplied name/version filter.
/// </summary>
public class LoadPlugins(ILogger logger, IPluginService pluginService) : OperationBaseWithDefaultResponse<LoadPluginsRequest>(logger)
{
    /// <inheritdoc />
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
            Logger.Error(message, ex, new Dictionary<string, object?> { { "Request", request } });
            return OperationResponse.CreateInternalServerError(message).AsTask();
        }
    }

    /// <inheritdoc />
    protected override Task<OperationResponse> ValidateOperationAsync(LoadPluginsRequest request)
        => OperationResponse
            .CreateSuccessOk()
            .AddErrorIf(!string.IsNullOrWhiteSpace(request.Version) && PluginVersion.TryParse(request.Version, out _), "")
            .AsTask();
}
