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
    public ValueTask<StorageEntryMetadata> GetMetadataAsync(string context, string identifier, string fileName, CancellationToken cancellationToken = default)
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
    public abstract ValueTask<StorageEntryMetadata> GetMetadataAsync(string location, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract ValueTask<bool> DeleteAsync(string location, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract IAsyncEnumerable<StorageEntryMetadata> ListFilesAsync(string context, string identifier, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract IAsyncEnumerable<StorageEntryMetadata> ListEntriesAsync(string context, string identifier, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }

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

    /// <summary>
    /// Builds a standardized forward-slash delimited virtual directory prefix for cloud object storage providers.
    /// </summary>
    /// <param name="context">The contextual domain or category.</param>
    /// <param name="identifier">The unique identifier.</param>
    /// <param name="basePrefix">An optional root prefix configured for the storage account or bucket.</param>
    /// <returns>A formatted directory prefix ending with a forward slash.</returns>
    protected static string BuildDirectoryPrefix(string context, string identifier, string? basePrefix = null)
    {
        string relativePrefix = string.IsNullOrEmpty(identifier) ? $"{context}/" : $"{context}/{identifier}/";
        return string.IsNullOrEmpty(basePrefix)
            ? relativePrefix
            : $"{basePrefix.Trim('/')}/{relativePrefix}";
    }

    /// <summary>
    /// Deconstructs a cloud object key or URI into its forward-slash delimited virtual directory path and file name.
    /// Ensures consistent behavior across Windows and Unix platforms without backslash replacement.
    /// </summary>
    /// <param name="key">The cloud object key or URI.</param>
    /// <returns>A tuple containing the virtual directory prefix (without trailing slash) and the file name (or <c>null</c> if the key represents a directory or ends with a slash).</returns>
    protected static (string Directory, string? FileName) ExtractCloudPath(string key)
    {
        if (string.IsNullOrEmpty(key))
            return (string.Empty, null);

        if (key.EndsWith('/'))
        {
            string normalizedDir = key.TrimEnd('/');
            int slashIndex = normalizedDir.LastIndexOf('/');
            string parentDirectory = slashIndex >= 0 ? normalizedDir[..slashIndex] : string.Empty;
            return (parentDirectory, null);
        }

        int lastSlash = key.LastIndexOf('/');
        if (lastSlash < 0)
            return (string.Empty, key);

        string directory = key[..lastSlash];
        string fileName = key[(lastSlash + 1)..];
        return (directory, string.IsNullOrEmpty(fileName) ? null : fileName);
    }

    /// <summary>
    /// Extracts the forward-slash delimited virtual directory path from a cloud object key or URI,
    /// ensuring consistent behavior across Windows and Unix platforms without backslash replacement.
    /// </summary>
    /// <param name="key">The cloud object key or URI.</param>
    /// <returns>The virtual directory prefix without a trailing slash, or an empty string if in root.</returns>
    protected static string ExtractCloudDirectory(string key) => ExtractCloudPath(key).Directory;

    /// <summary>
    /// Extracts the file name and extension from a cloud object key or URI.
    /// </summary>
    /// <param name="key">The cloud object key or URI.</param>
    /// <returns>The file name, or <c>null</c> if the key represents a directory or ends with a slash.</returns>
    protected static string? ExtractCloudFileName(string key) => ExtractCloudPath(key).FileName;
}
