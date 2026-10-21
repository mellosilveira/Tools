using MelloSilveiraTools.Core.Application.Commands;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Plugins.Application.Validators;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Services;

namespace MelloSilveiraTools.Plugins.Application.Commands.Get;

/// <summary>
/// Operation that retrieves registered plugins matching the supplied name, version and load-state filters.
/// </summary>
public class GetPlugins(IPluginService pluginService, PluginValidator<GetPluginsRequest> validator) : ListedCommandBase<GetPluginsRequest, RegisteredPlugin>(validator)
{
    /// <inheritdoc />
    protected override async Task<ListedResult<RegisteredPlugin>> ExecuteCommandAsync(GetPluginsRequest request, CancellationToken cancellationToken = default)
    {
        List<RegisteredPlugin> plugins = await pluginService
            .GetPluginsAsync(request.Name, PluginVersion.SafeParse(request.Version), cancellationToken)
            .Where(registered => request.FullyLoaded is null || registered.IsFullyLoaded == request.FullyLoaded)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result.CreateListedSuccessOk(plugins);
    }
}
