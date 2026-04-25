namespace MelloSilveiraTools.Infrastructure.Plugins.Persistences;

/// <summary>
/// Built-in keys used to register <see cref="IPluginCachePersistence"/> implementations
/// as keyed services. The key must match the <c>{target}</c> route segment on the
/// plugin cache endpoints. Consumers of this package are free to register additional
/// implementations under their own keys.
/// </summary>
public static class PluginCacheTargets
{
    /// <summary>JSON file-based persistence (<see cref="JsonFilePluginCachePersistence"/>).</summary>
    public const string File = "file";

    /// <summary>Database-backed persistence (<see cref="DatabasePluginCachePersistence"/>).</summary>
    public const string Database = "database";
}
