namespace MelloSilveiraTools.Core.Managers.File.Models;

/// <summary>
/// Specifies the underlying storage technology provider for file persistence and retrieval.
/// </summary>
public enum StorageProvider
{
    /// <summary>
    /// Local physical file system storage on the host or container machine.
    /// </summary>
    Local = 0,

    /// <summary>
    /// Cloud object or blob storage provider (e.g., Azure Blob Storage).
    /// </summary>
    BlobStorage = 1,

    /// <summary>
    /// Distributed file system provider (e.g., HDFS, SMB/UNC network share, Ceph).
    /// </summary>
    Dfs = 2,

    /// <summary>
    /// Amazon Simple Storage Service (AWS S3) provider.
    /// </summary>
    AwsS3 = 3
}
