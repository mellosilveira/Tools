namespace MelloSilveiraTools.Infrastructure.Plugins.Models;

/// <summary>
/// Identifies a stage in the plugin processing pipeline.
/// Clearing a stage also clears all subsequent stages.
/// </summary>
public enum CacheStage
{
    Discovery = 0,
    Assembly = 1,
    Processed = 2
}
