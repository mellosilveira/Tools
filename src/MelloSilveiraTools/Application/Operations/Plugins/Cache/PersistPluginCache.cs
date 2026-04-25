using MelloSilveiraTools.ExtensionMethods;
using MelloSilveiraTools.Infrastructure.Logger;
using MelloSilveiraTools.Infrastructure.Plugins.Models;
using MelloSilveiraTools.Infrastructure.Services.Plugins;

namespace MelloSilveiraTools.Application.Operations.Plugins.Cache;

/// <summary>
/// Operation that persists the plugin cache to the configured target (file, database, etc.) for the supplied plugin filter.
/// </summary>
public class PersistPluginCache(ILogger logger, IPluginService pluginService) : OperationBaseWithDefaultResponse<PersistPluginCacheRequest>(logger)
{
    /// <inheritdoc />
    protected override async Task<OperationResponse> ProcessOperationAsync(PersistPluginCacheRequest request)
    {
        await pluginService.PersistCacheAsync(request.Name, PluginVersion.SafeParse(request.Version)).ConfigureAwait(false);
        return OperationResponse.CreateSuccessOk();
    }

    /// <inheritdoc />
    protected override Task<OperationResponse> ValidateOperationAsync(PersistPluginCacheRequest request) => OperationResponse
        .CreateSuccessOk()
        .AddErrorIf(!string.IsNullOrWhiteSpace(request.Version) && PluginVersion.TryParse(request.Version, out _), "Invalid version.")
        .AsTask();
}
