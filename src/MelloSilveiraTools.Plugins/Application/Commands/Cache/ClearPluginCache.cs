using MelloSilveiraTools.Core.Application.Commands;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Services;

namespace MelloSilveiraTools.Plugins.Application.Commands.Cache;

/// <summary>
/// Operation that clears the plugin cache managed by <see cref="IPluginService"/>.
/// </summary>
public class ClearPluginCache(IPluginService pluginService) : DefaultCommandBase
{
    /// <inheritdoc />
    protected override async Task<Result> ExecuteCommandAsync()
    {
        await pluginService.ClearAsync();
        return Result.CreateSuccessOk();
    }
}
