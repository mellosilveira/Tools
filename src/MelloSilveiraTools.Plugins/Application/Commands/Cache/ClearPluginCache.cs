using MelloSilveiraTools.Core.Commands;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Services;

namespace MelloSilveiraTools.Plugins.Application.Commands.Cache;

/// <summary>
/// Operation that clears the plugin cache managed by <see cref="IPluginService"/>.
/// Access is restricted to superusers at the HTTP authorization boundary (<c>SuperuserOnly</c>).
/// </summary>
/// <param name="pluginService">Plugin management service.</param>
public class ClearPluginCache(IPluginService pluginService) : DefaultCommandBase
{
    /// <inheritdoc />
    protected override async Task<Result> ExecuteCommandAsync(CancellationToken cancellationToken = default)
    {
        await pluginService.ClearAsync().ConfigureAwait(false);
        return Result.CreateSuccessOk();
    }
}
