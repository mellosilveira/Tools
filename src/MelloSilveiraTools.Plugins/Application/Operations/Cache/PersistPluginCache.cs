using MelloSilveiraTools.Core.Infrastructure.Logger;
using MelloSilveiraTools.Database.ExtensionMethods;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Services;
using MelloSilveiraTools.WebApi.Application.Operations;
using MelloSilveiraTools.WebApi.ExtensionMethods;

namespace MelloSilveiraTools.Plugins.Application.Operations.Cache;

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
