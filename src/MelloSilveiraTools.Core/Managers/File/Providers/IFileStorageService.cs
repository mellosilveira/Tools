using MelloSilveiraTools.Core.Managers.File.Models;

namespace MelloSilveiraTools.Core.Managers.File.Providers;

/// <summary>
/// Storage-agnostic abstraction for file and directory management, location resolution, and stream handling.
/// Encapsulates knowledge of internal canonical directory layouts ("storage/{identifier}/{context}/")
/// independent of the underlying storage technology (e.g., Local File System, Blob Storage, Distributed File System, Amazon S3).
/// Implementations are registered in dependency injection as keyed services by <see cref="StorageProvider"/>.
/// </summary>
public interface IFileStorageService : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Gets the storage provider technology managed by this service instance.
    /// </summary>
    StorageProvider Provider { get; }

    /// <summary>
    /// Resolves the canonical storage location or URI for a file within the specified identifier and domain context partition.
    /// </summary>
    /// <param name="identifier">Unique run, experiment, or analysis identifier.</param>
    /// <param name="context">Business domain context.</param>
    /// <param name="fileName">Target file name including extension.</param>
    /// <returns>The canonical physical path or storage URI.</returns>
    string ResolveLocation(string identifier, string context, string fileName);

    /// <summary>
    /// Asynchronously opens a stream for writing to the specified physical path or storage URI.
    /// </summary>
    /// <param name="location">Physical path or storage URI.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A value task representing the asynchronous operation, returning a writable <see cref="Stream"/>.</returns>
    ValueTask<Stream> OpenWriteAsync(string location, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously creates or overwrites a file at the contextually resolved location and opens a stream for writing, returning both stream and canonical location.
    /// </summary>
    /// <param name="identifier">Unique run, experiment, or analysis identifier.</param>
    /// <param name="context">Business domain context.</param>
    /// <param name="fileName">Target file name including extension.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A value task returning a tuple containing the writable <see cref="Stream"/> and the canonical storage location.</returns>
    ValueTask<(Stream Stream, string Location)> CreateWriteAsync(string identifier, string context, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously creates a timestamped file at the contextually resolved location and opens a stream for writing, returning both stream and canonical location.
    /// </summary>
    /// <param name="identifier">Unique run, experiment, or analysis identifier.</param>
    /// <param name="context">Business domain context.</param>
    /// <param name="filePrefix">Business identifier prefix.</param>
    /// <param name="fileExtension">File extension including dot (e.g., ".csv").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A value task returning a tuple containing the writable <see cref="Stream"/> and the canonical storage location.</returns>
    ValueTask<(Stream Stream, string Location)> CreateWriteTimebasedAsync(string identifier, string context, string filePrefix, string fileExtension, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously creates a timestamped file at the contextually resolved location and opens a stream for writing, returning both stream and canonical location.
    /// </summary>
    /// <param name="identifier">Unique run, experiment, or analysis identifier.</param>
    /// <param name="context">Business domain context.</param>
    /// <param name="fileExtension">File extension including dot (e.g., ".csv").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A value task returning a tuple containing the writable <see cref="Stream"/> and the canonical storage location.</returns>
    ValueTask<(Stream Stream, string Location)> CreateWriteTimebasedAsync(string identifier, string context, string fileExtension, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously opens a stream for reading from the specified physical path or storage URI.
    /// </summary>
    /// <param name="location">Physical path or storage URI.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A value task returning a readable <see cref="Stream"/>.</returns>
    ValueTask<Stream> OpenReadAsync(string location, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously opens a stream for reading from a contextually resolved file location.
    /// </summary>
    /// <param name="identifier">Unique run, experiment, or analysis identifier.</param>
    /// <param name="context">Business domain context.</param>
    /// <param name="fileName">Target file name including extension.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A value task returning a readable <see cref="Stream"/>.</returns>
    ValueTask<Stream> OpenReadAsync(string identifier, string context, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously checks whether the file exists at the specified physical path or storage URI.
    /// </summary>
    /// <param name="location">Physical path or storage URI.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A value task returning <c>true</c> if the file exists; otherwise, <c>false</c>.</returns>
    ValueTask<bool> ExistsAsync(string location, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously checks whether the file exists at the contextually resolved file location.
    /// </summary>
    /// <param name="identifier">Unique run, experiment, or analysis identifier.</param>
    /// <param name="context">Business domain context.</param>
    /// <param name="fileName">Target file name including extension.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A value task returning <c>true</c> if the file exists; otherwise, <c>false</c>.</returns>
    ValueTask<bool> ExistsAsync(string identifier, string context, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves storage entry metadata for a specific physical path or storage URI.
    /// </summary>
    /// <param name="location">Physical path or storage URI.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A value task returning a populated <see cref="StorageEntryMetadata"/> instance.</returns>
    ValueTask<StorageEntryMetadata> GetMetadataAsync(string location, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves storage entry metadata for a contextually resolved file location.
    /// </summary>
    /// <param name="identifier">Unique run, experiment, or analysis identifier.</param>
    /// <param name="context">Business domain context.</param>
    /// <param name="fileName">Target file name including extension.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A value task returning a populated <see cref="StorageEntryMetadata"/> instance.</returns>
    ValueTask<StorageEntryMetadata> GetMetadataAsync(string identifier, string context, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously deletes the file at the specified physical path or storage URI if it exists.
    /// </summary>
    /// <param name="location">Physical path or storage URI.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A value task returning <c>true</c> if the file was deleted; otherwise, <c>false</c>.</returns>
    ValueTask<bool> DeleteAsync(string location, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously deletes the file within the specified identifier and context partition if it exists.
    /// </summary>
    /// <param name="identifier">Unique run, experiment, or analysis identifier.</param>
    /// <param name="context">Business domain context.</param>
    /// <param name="fileName">Target file name including extension.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A value task returning <c>true</c> if the file was deleted; otherwise, <c>false</c>.</returns>
    ValueTask<bool> DeleteAsync(string identifier, string context, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously lists all file metadata for files residing within the specified identifier and context partition.
    /// </summary>
    /// <param name="identifier">Unique run, experiment, or analysis identifier.</param>
    /// <param name="context">Business domain context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An asynchronous stream of <see cref="StorageEntryMetadata"/> items.</returns>
    IAsyncEnumerable<StorageEntryMetadata> ListFilesAsync(string identifier, string context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously lists all storage entry metadata (both files and directories) residing within the specified identifier and context partition.
    /// </summary>
    /// <param name="identifier">Unique run, experiment, or analysis identifier.</param>
    /// <param name="context">Business domain context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An asynchronous stream of <see cref="StorageEntryMetadata"/> items.</returns>
    IAsyncEnumerable<StorageEntryMetadata> ListEntriesAsync(string identifier, string context, CancellationToken cancellationToken = default);
}
