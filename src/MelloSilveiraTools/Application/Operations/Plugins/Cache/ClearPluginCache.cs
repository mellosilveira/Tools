using MelloSilveiraTools.ExtensionMethods;
using MelloSilveiraTools.Infrastructure.Logger;
using MelloSilveiraTools.Infrastructure.Services.Plugins;

namespace MelloSilveiraTools.Application.Operations.Plugins.Cache;

public class ClearPluginCache(ILogger logger, IPluginService pluginService) : OperationBaseWithoutRequest<OperationResponse>(logger)
{
    protected override Task<OperationResponse> ProcessOperationAsync()
    {
        pluginService.Clear();
        return OperationResponse.CreateSuccessOk().AsTask();
    }
}
