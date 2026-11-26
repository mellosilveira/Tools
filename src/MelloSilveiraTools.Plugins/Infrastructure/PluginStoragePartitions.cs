namespace MelloSilveiraTools.Plugins.Infrastructure;

/// <summary>
/// Defines canonical partition names for plugin storage lifecycle states.
/// </summary>
public static class PluginStoragePartitions
{
    /// <summary>
    /// Isolated review partition where authenticated community plugin submissions await superuser approval.
    /// Never scanned or loaded automatically by the background orchestrator.
    /// </summary>
    public const string Pending = "pending";

    /// <summary>
    /// Partition retaining community plugin submissions rejected by a superuser during moderation.
    /// </summary>
    public const string Rejected = "rejected";

    /// <summary>
    /// Staging partition where approved plugin assemblies arrive awaiting runtime discovery and loading.
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

    /// <summary>
    /// Ordered set of main plugin lifecycle partitions scanned during superuser storage inspection:
    /// <c>loaded</c>, <c>incoming</c>, <c>pending</c>, <c>archive</c>, <c>quarantine</c>, and <c>rejected</c>.
    /// </summary>
    public static readonly IReadOnlyList<string> MainPartitions =
    [
        Loaded,
        Incoming,
        Pending,
        Archive,
        Quarantine,
        Rejected
    ];
}
