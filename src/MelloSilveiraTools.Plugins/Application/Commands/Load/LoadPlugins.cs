using MelloSilveiraTools.Core.Commands;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Plugins.Application.Validators;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Services;

namespace MelloSilveiraTools.Plugins.Application.Commands.Load;

/// <summary>
/// Operation that loads plugins into the runtime based on the supplied name/version filter.
/// Access is restricted to superusers at the HTTP authorization boundary (<c>SuperuserOnly</c>).
/// </summary>
/// <param name="pluginService">Plugin management service.</param>
/// <param name="validator">Validator for plugin requests.</param>
public class LoadPlugins(
    IPluginService pluginService,
    PluginValidator validator) : CommandBaseWithDefaultResponse<PluginsRequest>(validator)
{
    /// <inheritdoc />
    protected override async Task<Result> ExecuteCommandAsync(PluginsRequest request, CancellationToken cancellationToken = default)
    {
        await pluginService.LoadPluginsOnRuntimeAsync(request.Name, PluginVersion.SafeParse(request.Version), cancellationToken).ConfigureAwait(false);
        return Result.CreateSuccessOk();
    }
}
