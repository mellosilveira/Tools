using SoftTissue.DataContracts.Operations;
using SoftTissue.DataContracts.Operations.Plugins.Cache;
using SoftTissue.Framework.Plugins.Models;
using SoftTissue.Framework.Plugins.Services;
using SoftTissue.Infrastructure.Plugins;

namespace MelloSilveiraTools.Application.Operations.Plugins.Cache;

public class ClearPluginCache(IPluginService<IMechanicalModelPlugin> pluginService)
    : OperationBaseWithDefaultResponse<ClearPluginCacheRequest>
{
    protected override Task<OperationResponse> ProcessOperationAsync(ClearPluginCacheRequest request)
    {
        OperationResponse response = new();

        if (string.IsNullOrWhiteSpace(request.Stage))
        {
            pluginService.ClearCache(CacheStage.Discovery);
        }
        else if (Enum.TryParse<CacheStage>(request.Stage, ignoreCase: true, out var stage))
        {
            pluginService.ClearCache(stage);
        }
        else
        {
            response.SetBadRequestError($"'{request.Stage}' is not a valid cache stage. Valid values: {string.Join(", ", Enum.GetNames<CacheStage>())}.");
            return Task.FromResult(response);
        }

        response.SetSuccessOk();
        return Task.FromResult(response);
    }
}
