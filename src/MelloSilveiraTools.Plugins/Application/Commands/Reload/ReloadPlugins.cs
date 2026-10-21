using MelloSilveiraTools.Core.Application.Commands;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Plugins.Application.Validators;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Services;

namespace MelloSilveiraTools.Plugins.Application.Commands.Reload;

/// <summary>
/// Operation that reloads plugins into the runtime based on the supplied name/version filter and force flag.
/// </summary>
public class ReloadPlugins(IPluginService pluginService, PluginValidator<ReloadPluginsRequest> validator) : CommandBaseWithDefaultResponse<ReloadPluginsRequest>(validator)
{
    /// <inheritdoc />
    protected override async Task<Result> ExecuteCommandAsync(ReloadPluginsRequest request, CancellationToken cancellationToken = default)
    {
        await pluginService.ReloadPluginsOnRuntimeAsync(request.Force, request.Name, PluginVersion.SafeParse(request.Version), cancellationToken).ConfigureAwait(false);
        return Result.CreateSuccessOk();
    }
}
