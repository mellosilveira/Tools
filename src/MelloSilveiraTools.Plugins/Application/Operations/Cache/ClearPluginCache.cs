using MelloSilveiraTools.Core.Logger;
using MelloSilveiraTools.Database.ExtensionMethods;
using MelloSilveiraTools.Plugins.Infrastructure.Services;
using MelloSilveiraTools.WebApi.Application.Operations;

namespace MelloSilveiraTools.Plugins.Application.Operations.Cache;

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
