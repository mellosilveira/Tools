using MelloSilveiraTools.Core.Commands;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Plugins.Application.Validators;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Services;

namespace MelloSilveiraTools.Plugins.Application.Commands.Get;

/// <summary>
/// Operation that retrieves sanitized public summaries of registered runtime plugins from memory cache.
/// Safe for anonymous and authenticated callers; never exposes internal storage paths or loads assemblies from disk.
/// </summary>
public class GetPlugins(IPluginService pluginService, PluginValidator<GetPluginsRequest> validator) : ListedCommandBase<GetPluginsRequest, PluginInfo>(validator)
{
    /// <inheritdoc />
    protected override async Task<ListedResult<PluginInfo>> ExecuteCommandAsync(GetPluginsRequest request, CancellationToken cancellationToken = default)
    {
        List<PluginInfo> plugins = await pluginService
            .GetPluginsAsync(request.Name, PluginVersion.SafeParse(request.Version), cancellationToken)
            .Where(registered => request.FullyLoaded is null || registered.IsFullyLoaded == request.FullyLoaded)
            .Select(PluginInfo.FromRegistered)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result.CreateListedSuccessOk(plugins);
    }
}
