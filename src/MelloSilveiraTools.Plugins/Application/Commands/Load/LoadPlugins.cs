using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Database.ExtensionMethods;
using MelloSilveiraTools.Plugins.Application.Validators;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Services;
using MelloSilveiraTools.WebApi.Application.Commands;

namespace MelloSilveiraTools.Plugins.Application.Commands.Load;

/// <summary>
/// Operation that loads plugins into the runtime based on the supplied name/version filter.
/// </summary>
public class LoadPlugins(IPluginService pluginService, PluginValidator validator) : CommandBaseWithDefaultResponse<PluginsRequest>(validator)
{
    /// <inheritdoc />
    protected override Task<Result> ExecuteCommandAsync(PluginsRequest request)
    {
        pluginService.LoadPluginsOnRuntime(request.Name, PluginVersion.SafeParse(request.Version));
        return Result.CreateSuccessOk().AsTask();
    }
}
