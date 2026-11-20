using MelloSilveiraTools.Core.Files.Providers.AwsS3;
using MelloSilveiraTools.Core.Files.Providers.BlobStorage;
using MelloSilveiraTools.Core.Files.Providers.Dfs;
using MelloSilveiraTools.Core.Files.Providers.Local;

namespace MelloSilveiraTools.Core.Files.Providers;

/// <summary>
/// Aggregated configuration settings providing a single unified binding point for multiple storage providers.
/// Enables simultaneous configuration of Local, DFS, Azure Blob, and AWS S3 storage under a single configuration section.
/// </summary>
public sealed record FileStorageSettings
{
    /// <summary>
    /// Gets the settings governing local file storage.
    /// </summary>
    public LocalFileStorageSettings? Local { get; init; }

    /// <summary>
    /// Gets the settings governing distributed file system (DFS) storage.
    /// </summary>
    public DfsStorageSettings? Dfs { get; init; }

    /// <summary>
    /// Gets the settings governing Azure Blob Storage.
    /// </summary>
    public BlobStorageSettings? BlobStorage { get; init; }

    /// <summary>
    /// Gets the settings governing Amazon S3 storage.
    /// </summary>
    public AwsS3StorageSettings? AwsS3 { get; init; }
}
