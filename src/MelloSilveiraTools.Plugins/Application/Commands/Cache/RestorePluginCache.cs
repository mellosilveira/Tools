using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Plugins.Application.Validators;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Services;
using MelloSilveiraTools.WebApi.Application.Commands;

namespace MelloSilveiraTools.Plugins.Application.Commands.Cache;

/// <summary>
/// Operation that restores the plugin cache from the configured target (file, database, etc.) for the supplied plugin filter.
/// </summary>
public class RestorePluginCache(IPluginService pluginService, PluginValidator validator) : CommandBaseWithDefaultResponse<PluginsRequest>(validator)
{
    /// <inheritdoc />
    protected override async Task<Result> ExecuteCommandAsync(PluginsRequest request)
    {
        await pluginService.RestoreCacheAsync(request.Name, PluginVersion.SafeParse(request.Version)).ConfigureAwait(false);
        return Result.CreateSuccessOk();
    }
}
