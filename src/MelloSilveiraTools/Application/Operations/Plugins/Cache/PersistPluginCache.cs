namespace MelloSilveiraTools.Application.Operations.Plugins.Cache;

public class PersistPluginCache(
    IPluginService pluginService,
    JsonFilePluginCachePersistence jsonPersistence,
    DatabasePluginCachePersistence databasePersistence)
    : OperationBaseWithDefaultResponse<PersistPluginCacheRequest>
{
    protected override async Task<OperationResponse> ProcessOperationAsync(PersistPluginCacheRequest request)
    {
        OperationResponse response = new();

        IPluginCachePersistence persistence = request.Target?.ToLowerInvariant() switch
        {
            "json" or null => jsonPersistence,
            "database" => databasePersistence,
            _ => null
        };

        if (persistence is null)
        {
            response.SetBadRequestError($"'{request.Target}' is not a valid persistence target. Valid values: json, database.");
            return response;
        }

        await pluginService.PersistCacheAsync(persistence).ConfigureAwait(false);
        response.SetSuccessOk();
        return response;
    }
}
