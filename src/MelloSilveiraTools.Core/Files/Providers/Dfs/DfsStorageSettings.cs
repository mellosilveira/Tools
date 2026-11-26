namespace MelloSilveiraTools.Core.Files.Providers.Dfs;

/// <summary>
/// Configuration settings governing internal folder layouts and root network share locations for distributed file system (DFS) storage.
/// </summary>
public sealed record DfsStorageSettings
{
    /// <summary>
    /// Gets the base network share or root DFS path.
    /// </summary>
    public required string BaseDirectory { get; init; }

    /// <summary>
    /// Gets an optional sentinel file name expected to exist directly under <see cref="BaseDirectory"/>
    /// to detect unmounted DFS or network volumes that leave an empty mount-point directory behind on Unix or container hosts.
    /// </summary>
    public string? HealthMarkerFileName { get; init; }
}
