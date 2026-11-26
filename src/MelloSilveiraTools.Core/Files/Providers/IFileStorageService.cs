using MelloSilveiraTools.Core.Files.Models;

namespace MelloSilveiraTools.Core.Files.Providers;

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
    /// Parses a canonical storage location produced by <see cref="ResolveLocation(string, string, string)"/>
    /// back into its <c>(Identifier, Context, FileName)</c> components.
    /// </summary>
    /// <param name="location">Physical path or cloud object key to parse.</param>
    /// <returns>A tuple containing the extracted identifier, domain context, and file name.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="location"/> does not conform to this provider's canonical layout.</exception>
    (string Identifier, string Context, string FileName) ParseLocation(string location);

    /// <summary>
    /// Extracts the normalized forward-slash directory prefix from a storage <paramref name="location"/>,
    /// falling back to <paramref name="fallbackDirectory"/> (or <see cref="string.Empty"/> when <see langword="null"/>)
    /// when no directory separator is present.
    /// </summary>
    /// <param name="location">Physical path or storage URI.</param>
    /// <param name="fallbackDirectory">Optional fallback directory returned when <paramref name="location"/> has no directory separator.</param>
    /// <returns>The normalized forward-slash directory path.</returns>
    string ExtractDirectory(string location, string? fallbackDirectory = null);

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

    /// <summary>
    /// Asynchronously moves a file from <paramref name="sourceLocation"/> to <paramref name="destinationLocation"/>.
    /// </summary>
    /// <param name="sourceLocation">Current physical path or storage URI of the source file.</param>
    /// <param name="destinationLocation">Target physical path or storage URI.</param>
    /// <param name="overwrite">When <see langword="true"/>, replaces any existing destination file; when <see langword="false"/>, throws <see cref="IOException"/> if the destination already exists.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="IOException">Thrown when <paramref name="overwrite"/> is <see langword="false"/> and <paramref name="destinationLocation"/> already exists.</exception>
    ValueTask MoveAsync(string sourceLocation, string destinationLocation, bool overwrite = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously moves a file from <paramref name="sourceLocation"/> into the destination partition resolved from
    /// <paramref name="destinationIdentifier"/>, <paramref name="destinationContext"/>, and <paramref name="destinationFileName"/>.
    /// </summary>
    /// <param name="sourceLocation">Current physical path or storage URI of the source file.</param>
    /// <param name="destinationIdentifier">Destination identifier.</param>
    /// <param name="destinationContext">Destination domain context.</param>
    /// <param name="destinationFileName">Destination file name including extension.</param>
    /// <param name="overwrite">When <see langword="true"/>, replaces any existing destination file; when <see langword="false"/>, throws <see cref="IOException"/> if the destination already exists.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resolved canonical storage location of the moved file.</returns>
    /// <exception cref="IOException">Thrown when <paramref name="overwrite"/> is <see langword="false"/> and the destination already exists.</exception>
    ValueTask<string> MoveAsync(string sourceLocation, string destinationIdentifier, string destinationContext, string destinationFileName, bool overwrite = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously computes the lowercase hexadecimal SHA-256 digest of the file at <paramref name="location"/>.
    /// </summary>
    /// <param name="location">Physical path or storage URI.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The lowercase hexadecimal SHA-256 hash string.</returns>
    ValueTask<string> ComputeSha256Async(string location, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously computes the lowercase hexadecimal SHA-256 digest of the file at the contextually resolved location.
    /// </summary>
    /// <param name="identifier">Unique run, experiment, or analysis identifier.</param>
    /// <param name="context">Business domain context.</param>
    /// <param name="fileName">Target file name including extension.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The lowercase hexadecimal SHA-256 hash string.</returns>
    ValueTask<string> ComputeSha256Async(string identifier, string context, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously streams <paramref name="sourceStream"/> into the contextually resolved location while computing
    /// its lowercase hexadecimal SHA-256 digest and written byte count in a single pass.
    /// </summary>
    /// <param name="identifier">Unique run, experiment, or analysis identifier.</param>
    /// <param name="context">Business domain context.</param>
    /// <param name="fileName">Target file name including extension.</param>
    /// <param name="sourceStream">Readable source stream to persist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the resolved storage location, normalized directory, lowercase SHA-256 hash, and total written bytes.</returns>
    ValueTask<(string Location, string Directory, string Sha256Hash, long SizeBytes)> WriteAndHashAsync(string identifier, string context, string fileName, Stream sourceStream, CancellationToken cancellationToken = default);
}
