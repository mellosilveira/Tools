namespace MelloSilveiraTools.Plugins.Infrastructure;

/// <summary>
/// Defines canonical partition names for plugin storage lifecycle states.
/// </summary>
public static class PluginStoragePartitions
{
    /// <summary>
    /// Staging partition where incoming plugin assemblies arrive awaiting validation and discovery.
    /// </summary>
    public const string Incoming = "incoming";

    /// <summary>
    /// Partition containing validated, promoted, and actively loaded plugin assemblies.
    /// </summary>
    public const string Loaded = "loaded";

    /// <summary>
    /// Partition containing deprecated older versions of plugin assemblies retained for historical purposes.
    /// </summary>
    public const string Archive = "archive";

    /// <summary>
    /// Partition isolating invalid, corrupted, or non-managed binaries that failed validation.
    /// </summary>
    public const string Quarantine = "quarantine";

    /// <summary>
    /// Partition containing application-level cache persistence files (e.g. <c>plugin-cache.json</c>).
    /// </summary>
    public const string Cache = "cache";

    /// <summary>
    /// Partition containing shadow copies of loaded assemblies to prevent persistent OS file locks.
    /// </summary>
    public const string Shadow = "shadow";
}
