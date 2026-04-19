using MelloSilveiraTools.Infrastructure.Plugins.Models;

namespace MelloSilveiraTools.Infrastructure.Plugins.Persistences;

/// <summary>
/// Persists and restores plugin cache state to/from non-volatile storage.
/// </summary>
public interface IPluginCachePersistence
{
    /// <summary>
    /// Saves the current plugin descriptors and state.
    /// </summary>
    Task SaveAsync(IReadOnlyDictionary<string, PluginBaseInfo> descriptors, IReadOnlyDictionary<string, PluginInfo> states);

    /// <summary>
    /// Loads previously saved state. Returns null if no data exists.
    /// </summary>
    Task<(Dictionary<string, PluginBaseInfo> Descriptors, Dictionary<string, PluginInfo> States)> LoadAsync();
}
