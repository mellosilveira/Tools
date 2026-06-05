using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Database.ExtensionMethods;
using MelloSilveiraTools.Plugins.Infrastructure.Services;
using MelloSilveiraTools.WebApi.Application.Commands;

namespace MelloSilveiraTools.Plugins.Application.Commands.Cache;

/// <summary>
/// Operation that clears the plugin cache managed by <see cref="IPluginService"/>.
/// </summary>
public class ClearPluginCache(IPluginService pluginService) : DefaultCommandBase
{
    /// <inheritdoc />
    protected override Task<Result> ExecuteCommandAsync()
    {
        pluginService.Clear();
        return Result.CreateSuccessOk().AsTask();
    }
}
