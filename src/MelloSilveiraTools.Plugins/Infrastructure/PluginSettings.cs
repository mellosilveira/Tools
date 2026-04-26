namespace MelloSilveiraTools.Plugins.Infrastructure;

/// <summary>
/// Configuration settings for the plugin system.
/// </summary>
public record PluginSettings
{
    /// <summary>
    /// Path to the directory where plugin DLLs are stored.
    /// </summary>
    public required string Directory { get; init; }

    /// <summary>
    /// Interval between plugin-folder inspections performed by the orchestrator background service.
    /// Defaults to 30 seconds.
    /// </summary>
    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Grace period that the version immediately below the newly-loaded one is kept in cache after a
    /// higher version is promoted. Once this period elapses, the previous version is also evicted
    /// on the next orchestrator pass. Defaults to 24 hours.
    /// </summary>
    /// <remarks>
    /// Example: while versions <c>1.0</c>, <c>1.1</c> and <c>1.2</c> are loaded and <c>1.3</c> is
    /// detected, the orchestrator loads <c>1.3</c>, evicts <c>1.0</c> and <c>1.1</c> immediately,
    /// and keeps <c>1.2</c> until this retention period is exceeded.
    /// </remarks>
    public TimeSpan PreviousVersionRetention { get; init; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Key of the <c>IPluginCachePersistence</c> implementation used when no <c>{target}</c> route
    /// value is available (e.g. inside the plugin orchestrator background service, which runs
    /// outside an HTTP request).
    /// </summary>
    /// <remarks>
    /// Must match a key registered with <c>AddKeyedSingleton&lt;IPluginCachePersistence&gt;(key, ...)</c>.
    /// Defaults to <see cref="Persistences.PluginCacheTargets.File"/>.
    /// </remarks>
    public required string DefaultCacheTarget { get; init; }
}
