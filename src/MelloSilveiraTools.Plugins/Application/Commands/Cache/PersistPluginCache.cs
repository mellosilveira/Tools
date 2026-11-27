using MelloSilveiraTools.Core.Commands;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Plugins.Application.Validators;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Services;

namespace MelloSilveiraTools.Plugins.Application.Commands.Cache;

/// <summary>
/// Operation that persists the plugin cache to the configured target (file, database, etc.) for the supplied plugin filter.
/// Access is restricted to superusers at the HTTP authorization boundary (<c>SuperuserOnly</c>).
/// </summary>
/// <param name="pluginService">Plugin management service.</param>
/// <param name="validator">Validator for plugin requests.</param>
public class PersistPluginCache(
    IPluginService pluginService,
    PluginValidator validator) : CommandBaseWithDefaultResponse<PluginsRequest>(validator)
{
    /// <inheritdoc />
    protected override async Task<Result> ExecuteCommandAsync(PluginsRequest request, CancellationToken cancellationToken = default)
    {
        await pluginService.PersistCacheAsync(request.Name, PluginVersion.SafeParse(request.Version), cancellationToken).ConfigureAwait(false);
        return Result.CreateSuccessOk();
    }
}
