using MelloSilveiraTools.Core.Managers.File.Models;
using MelloSilveiraTools.Core.Managers.File.Providers.Local;

namespace MelloSilveiraTools.Core.Managers.File.Providers.Dfs;

/// <summary>
/// Storage-agnostic file management implementation targeting distributed file systems (DFS) and network shares.
/// Inherits from <see cref="LocalFileStorageService"/> as DFS represents a network-mounted hierarchical file system,
/// configured with <see cref="FileShare.ReadWrite"/> to facilitate concurrent distributed access.
/// Validates base directory platform compatibility to prevent common deployment misconfigurations across operating systems.
/// </summary>
/// <param name="settings">DFS storage settings defining the root network share path or mount point.</param>
public class DfsFileStorageService(DfsStorageSettings settings) : LocalFileStorageService(ValidateAndCreateSettings(settings))
{
    /// <inheritdoc />
    public override StorageProvider Provider => StorageProvider.Dfs;

    /// <inheritdoc />
    protected override FileShare WriteFileShare => FileShare.ReadWrite;

    /// <inheritdoc />
    protected override FileShare ReadFileShare => FileShare.ReadWrite;

    private static LocalFileStorageSettings ValidateAndCreateSettings(DfsStorageSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.BaseDirectory);

        string baseDirectory = settings.BaseDirectory.Trim();

        if (!OperatingSystem.IsWindows())
            ValidateNonWindowsPath(baseDirectory);

        return new LocalFileStorageSettings { BaseDirectory = baseDirectory };
    }

    private static void ValidateNonWindowsPath(string path)
    {
        if (path.StartsWith(@"\\", StringComparison.Ordinal))
        {
            string platformName = OperatingSystem.IsLinux()
                ? "Linux"
                : OperatingSystem.IsMacOS() ? "macOS" : "Unix-like";

            string exampleMount = OperatingSystem.IsMacOS() ? "/Volumes/dfs" : "/mnt/dfs";

            throw new PlatformNotSupportedException(
                $"Windows UNC network share path '{path}' cannot be accessed directly on {platformName}. " +
                $"In {platformName} and containerized environments, mount the network share to a local directory (e.g., '{exampleMount}') " +
                "and configure BaseDirectory with the mount point path.");
        }

        if (path.Length >= 2 && char.IsAsciiLetter(path[0]) && path[1] == ':')
        {
            throw new PlatformNotSupportedException(
                $"Windows drive path '{path}' is not valid on non-Windows operating systems. " +
                "Configure BaseDirectory with the local directory or mount point path (e.g., '/mnt/dfs' or './data/dfs').");
        }
    }
}
