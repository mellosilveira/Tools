using MelloSilveiraTools.Core.Managers.File.Models;
using MelloSilveiraTools.Core.Managers.File.Providers.Local;

namespace MelloSilveiraTools.Core.Managers.File.Providers.Dfs;

/// <summary>
/// Storage-agnostic file management implementation targeting distributed file systems (DFS) and network shares.
/// Inherits from <see cref="LocalFileStorageService"/> as DFS represents a network-mounted hierarchical file system,
/// configured with <see cref="FileShare.ReadWrite"/> to facilitate concurrent distributed access.
/// </summary>
/// <param name="timeProvider">The time provider instance used for temporal operations.</param>
/// <param name="settings">DFS storage settings defining the root network share path or mount point.</param>
public class DfsFileStorageService(TimeProvider timeProvider, DfsStorageSettings settings)
    : LocalFileStorageService(timeProvider, new LocalFileStorageSettings { BaseDirectory = settings.BaseDirectory })
{
    /// <inheritdoc />
    public override StorageProvider Provider => StorageProvider.Dfs;

    /// <inheritdoc />
    protected override FileShare WriteFileShare => FileShare.ReadWrite;

    /// <inheritdoc />
    protected override FileShare ReadFileShare => FileShare.ReadWrite;
}
