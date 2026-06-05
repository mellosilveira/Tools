using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Database.ExtensionMethods;
using MelloSilveiraTools.Plugins.Application.Validators;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Services;
using MelloSilveiraTools.WebApi.Application.Commands;

namespace MelloSilveiraTools.Plugins.Application.Commands.Reload;

/// <summary>
/// Operation that reloads plugins into the runtime based on the supplied name/version filter and force flag.
/// </summary>
public class ReloadPlugins(IPluginService pluginService, PluginValidator<ReloadPluginsRequest> validator) : CommandBaseWithDefaultResponse<ReloadPluginsRequest>(validator)
{
    /// <inheritdoc />
    protected override Task<Result> ExecuteCommandAsync(ReloadPluginsRequest request)
    {
        pluginService.ReloadPluginsOnRuntime(request.Force, request.Name, PluginVersion.SafeParse(request.Version));
        return Result.CreateSuccessOk().AsTask();
    }
}
