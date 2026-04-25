using MelloSilveiraTools.Core.Infrastructure.Logger;
using MelloSilveiraTools.Database.ExtensionMethods;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Services;
using MelloSilveiraTools.WebApi.Application.Operations;
using MelloSilveiraTools.WebApi.ExtensionMethods;

namespace MelloSilveiraTools.Plugins.Application.Operations.Cache;

/// <summary>
/// Operation that restores the plugin cache from the configured target (file, database, etc.) for the supplied plugin filter.
/// </summary>
public class RestorePluginCache(ILogger logger, IPluginService pluginService) : OperationBaseWithDefaultResponse<RestorePluginCacheRequest>(logger)
{
    /// <inheritdoc />
    protected override async Task<OperationResponse> ProcessOperationAsync(RestorePluginCacheRequest request)
    {
        await pluginService.RestoreCacheAsync(request.Name, PluginVersion.SafeParse(request.Version)).ConfigureAwait(false);
        return OperationResponse.CreateSuccessOk();
    }

    /// <inheritdoc />
    protected override Task<OperationResponse> ValidateOperationAsync(RestorePluginCacheRequest request) => OperationResponse
        .CreateSuccessOk()
        .AddErrorIf(!string.IsNullOrWhiteSpace(request.Version) && PluginVersion.TryParse(request.Version, out _), "Invalid version.")
        .AsTask();
}
