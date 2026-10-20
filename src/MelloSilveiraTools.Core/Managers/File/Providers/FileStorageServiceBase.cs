using MelloSilveiraTools.Core.Managers.File.Models;

namespace MelloSilveiraTools.Core.Managers.File.Providers;

/// <summary>
/// Abstract base implementation of <see cref="IFileStorageService"/> providing common functionality,
/// path resolution, time-based file naming, and bridging between location-based and context-based operations.
/// </summary>
public abstract class FileStorageServiceBase : IFileStorageService
{
    /// <inheritdoc />
    public abstract StorageProvider Provider { get; }

    /// <inheritdoc />
    public abstract string ResolveLocation(string context, string identifier, string fileName);

    /// <inheritdoc />
    public async ValueTask<(Stream Stream, string Location)> CreateWriteAsync(string context, string identifier, string fileName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string location = ResolveLocation(context, identifier, fileName);
        Stream stream = await OpenWriteAsync(location, cancellationToken).ConfigureAwait(false);
        return (stream, location);
    }

    /// <inheritdoc />
    public ValueTask<(Stream Stream, string Location)> CreateWriteTimebasedAsync(string context, string identifier, string filePrefix, string fileExtension, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string timestampedName = GenerateTimebasedFileName(filePrefix, fileExtension);
        return CreateWriteAsync(context, identifier, timestampedName, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<(Stream Stream, string Location)> CreateWriteTimebasedAsync(string context, string identifier, string fileExtension, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string timestampedName = GenerateTimebasedFileName(fileExtension);
        return CreateWriteAsync(context, identifier, timestampedName, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<Stream> OpenReadAsync(string context, string identifier, string fileName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string location = ResolveLocation(context, identifier, fileName);
        return OpenReadAsync(location, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<bool> ExistsAsync(string context, string identifier, string fileName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string location = ResolveLocation(context, identifier, fileName);
        return ExistsAsync(location, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<FileMetadata> GetMetadataAsync(string context, string identifier, string fileName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string location = ResolveLocation(context, identifier, fileName);
        return GetMetadataAsync(location, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<bool> DeleteAsync(string context, string identifier, string fileName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string location = ResolveLocation(context, identifier, fileName);
        return DeleteAsync(location, cancellationToken);
    }

    /// <inheritdoc />
    public abstract ValueTask<Stream> OpenWriteAsync(string location, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract ValueTask<Stream> OpenReadAsync(string location, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract ValueTask<bool> ExistsAsync(string location, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract ValueTask<FileMetadata> GetMetadataAsync(string location, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract ValueTask<bool> DeleteAsync(string location, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract ValueTask<IReadOnlyList<FileMetadata>> ListFilesAsync(string context, string identifier, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public virtual ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Generates a standardized timestamped file name using UTC time ("yyyy-MM-dd_HH-mm-ss").
    /// </summary>
    /// <param name="filePrefix">Business prefix.</param>
    /// <param name="fileExtension">File extension including the leading dot.</param>
    /// <returns>A formatted timestamped file name.</returns>
    protected static string GenerateTimebasedFileName(string filePrefix, string fileExtension) => $"{filePrefix}_{GenerateTimebasedFileName(fileExtension)}";

    /// <summary>
    /// Generates a standardized timestamped file name using UTC time ("yyyy-MM-dd_HH-mm-ss").
    /// </summary>
    /// <param name="fileExtension">File extension including the leading dot.</param>
    /// <returns>A formatted timestamped file name.</returns>
    protected static string GenerateTimebasedFileName(string fileExtension)
    {
        string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss");
        return $"{timestamp}{fileExtension}";
    }
}
