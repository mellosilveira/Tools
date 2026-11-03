namespace MelloSilveiraTools.Core.Managers.File.Providers.Dfs;

/// <summary>
/// Configuration settings governing internal folder layouts and root network share locations for distributed file system (DFS) storage.
/// </summary>
public sealed record DfsStorageSettings
{
    /// <summary>
    /// Gets the base network share or root DFS path.
    /// </summary>
    public required string BaseDirectory { get; init; }
}
