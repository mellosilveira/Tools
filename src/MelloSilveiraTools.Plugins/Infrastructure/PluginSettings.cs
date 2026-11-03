using MelloSilveiraTools.Core.Managers.File.Models;

namespace MelloSilveiraTools.Plugins.Infrastructure;

/// <summary>
/// Configuration settings for the plugin system.
/// </summary>
public record PluginSettings
{
    /// <summary>
    /// Storage provider used for discovering, loading, persisting, and archiving plugin files.
    /// </summary>
    public required StorageProvider StorageProvider { get; init; }

    /// <summary>
    /// Path to the directory where plugin DLLs are stored.
    /// </summary>
    public required string Directory { get; init; }

    /// <summary>
    /// Interval between plugin-folder inspections performed by the orchestrator background service.
    /// </summary>
    public required TimeSpan PollInterval { get; init; }

    /// <summary>
    /// Grace period that the version immediately below the newly-loaded one is kept in cache after a
    /// higher version is promoted. Once this period elapses, the previous version is also evicted
    /// on the next orchestrator pass.
    /// </summary>
    /// <remarks>
    /// Example: while versions <c>1.0</c>, <c>1.1</c> and <c>1.2</c> are loaded and <c>1.3</c> is
    /// detected, the orchestrator loads <c>1.3</c>, evicts <c>1.0</c> and <c>1.1</c> immediately,
    /// and keeps <c>1.2</c> until this retention period is exceeded.
    /// </remarks>
    public required TimeSpan PreviousVersionRetention { get; init; }

    /// <summary>
    /// Key of the <c>IPluginCachePersistence</c> implementation used when no <c>{target}</c> route
    /// value is available (e.g. inside the plugin orchestrator background service, which runs
    /// outside an HTTP request).
    /// </summary>
    /// <remarks>
    /// Must match a key registered with <c>AddKeyedSingleton&lt;IPluginCachePersistence&gt;(key, ...)</c>.
    /// </remarks>
    public required string DefaultCacheTarget { get; init; }
}
