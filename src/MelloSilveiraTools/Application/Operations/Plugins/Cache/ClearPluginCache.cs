using MelloSilveiraTools.ExtensionMethods;
using MelloSilveiraTools.Infrastructure.Logger;
using MelloSilveiraTools.Infrastructure.Services.Plugins;

namespace MelloSilveiraTools.Application.Operations.Plugins.Cache;

/// <summary>
/// Operation that clears the plugin cache managed by <see cref="IPluginService"/>.
/// </summary>
public class ClearPluginCache(ILogger logger, IPluginService pluginService) : DefaultOperationBase(logger)
{
    /// <inheritdoc />
    protected override Task<OperationResponse> ProcessOperationAsync()
    {
        pluginService.Clear();
        return OperationResponse.CreateSuccessOk().AsTask();
    }
}
