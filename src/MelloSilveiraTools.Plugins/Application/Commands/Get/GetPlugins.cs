using MelloSilveiraTools.Core.Application.Commands;
using MelloSilveiraTools.Core.ExtensionMethods;
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
    protected override Task<ListedResult<RegisteredPlugin>> ExecuteCommandAsync(GetPluginsRequest request)
        => Result
            .CreateListedSuccessOk(pluginService
                .GetPlugins(request.Name, PluginVersion.SafeParse(request.Version))
                .Where(registered => request.FullyLoaded is null || registered.IsFullyLoaded == request.FullyLoaded)
                .ToArray())
            .ToTask();
}
