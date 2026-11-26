using MelloSilveiraTools.Core.Files.Models;
using System.Security.Cryptography;

namespace MelloSilveiraTools.Core.Files.Providers;

/// <summary>
/// Abstract base implementation of <see cref="IFileStorageService"/> providing common functionality,
/// path resolution, time-based file naming, and bridging between location-based and context-based operations.
/// </summary>
/// <param name="timeProvider">The time provider instance used for temporal operations.</param>
public abstract class FileStorageServiceBase(TimeProvider timeProvider) : IFileStorageService
{
    /// <summary>
    /// Gets the time provider used for time-based operations and file naming.
    /// </summary>
    protected TimeProvider TimeProvider { get; } = timeProvider;

    /// <inheritdoc />
    public abstract StorageProvider Provider { get; }

    /// <inheritdoc />
    public abstract string ResolveLocation(string identifier, string context, string fileName);

    /// <inheritdoc />
    public abstract (string Identifier, string Context, string FileName) ParseLocation(string location);

    /// <inheritdoc />
    public string ExtractDirectory(string location, string? fallbackDirectory = null)
    {
        string normalized = location.Replace('\\', '/');
        int lastSlash = normalized.LastIndexOf('/');
        return lastSlash > 0 ? normalized[..lastSlash] : fallbackDirectory ?? string.Empty;
    }

    /// <inheritdoc />
    public async ValueTask<(Stream Stream, string Location)> CreateWriteAsync(string identifier, string context, string fileName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string location = ResolveLocation(identifier, context, fileName);
        Stream stream = await OpenWriteAsync(location, cancellationToken).ConfigureAwait(false);
        return (stream, location);
    }

    /// <inheritdoc />
    public ValueTask<(Stream Stream, string Location)> CreateWriteTimebasedAsync(string identifier, string context, string filePrefix, string fileExtension, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string timestampedName = GenerateTimebasedFileName(filePrefix, fileExtension);
        return CreateWriteAsync(identifier, context, timestampedName, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<(Stream Stream, string Location)> CreateWriteTimebasedAsync(string identifier, string context, string fileExtension, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string timestampedName = GenerateTimebasedFileName(fileExtension);
        return CreateWriteAsync(identifier, context, timestampedName, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<Stream> OpenReadAsync(string identifier, string context, string fileName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string location = ResolveLocation(identifier, context, fileName);
        return OpenReadAsync(location, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<bool> ExistsAsync(string identifier, string context, string fileName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string location = ResolveLocation(identifier, context, fileName);
        return ExistsAsync(location, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<StorageEntryMetadata> GetMetadataAsync(string identifier, string context, string fileName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string location = ResolveLocation(identifier, context, fileName);
        return GetMetadataAsync(location, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<bool> DeleteAsync(string identifier, string context, string fileName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string location = ResolveLocation(identifier, context, fileName);
        return DeleteAsync(location, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask MoveAsync(string sourceLocation, string destinationLocation, bool overwrite = true, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.Equals(sourceLocation, destinationLocation, StringComparison.OrdinalIgnoreCase))
            return;

        if (!overwrite && await ExistsAsync(destinationLocation, cancellationToken).ConfigureAwait(false))
            throw new IOException($"Destination storage location '{destinationLocation}' already exists.");

        await using (Stream sourceStream = await OpenReadAsync(sourceLocation, cancellationToken).ConfigureAwait(false))
        {
            await using Stream destinationStream = await OpenWriteAsync(destinationLocation, cancellationToken).ConfigureAwait(false);
            await sourceStream.CopyToAsync(destinationStream, cancellationToken).ConfigureAwait(false);
            await destinationStream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        await DeleteAsync(sourceLocation, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<string> MoveAsync(string sourceLocation, string destinationIdentifier, string destinationContext, string destinationFileName, bool overwrite = true, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string destinationLocation = ResolveLocation(destinationIdentifier, destinationContext, destinationFileName);
        await MoveAsync(sourceLocation, destinationLocation, overwrite, cancellationToken).ConfigureAwait(false);
        return destinationLocation;
    }

    /// <inheritdoc />
    public async ValueTask<string> ComputeSha256Async(string location, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using Stream stream = await OpenReadAsync(location, cancellationToken).ConfigureAwait(false);
        byte[] hashBytes = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexStringLower(hashBytes);
    }

    /// <inheritdoc />
    public ValueTask<string> ComputeSha256Async(string identifier, string context, string fileName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string location = ResolveLocation(identifier, context, fileName);
        return ComputeSha256Async(location, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<(string Location, string Directory, string Sha256Hash, long SizeBytes)> WriteAndHashAsync(string identifier, string context, string fileName, Stream sourceStream, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string targetLocation = ResolveLocation(identifier, context, fileName);
        long bytesWritten = 0;

        using IncrementalHash hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        await using Stream targetStream = await OpenWriteAsync(targetLocation, cancellationToken).ConfigureAwait(false);

        byte[] buffer = new byte[81920];
        int read;
        while ((read = await sourceStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            hasher.AppendData(buffer, 0, read);
            await targetStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            bytesWritten += read;
        }

        await targetStream.FlushAsync(cancellationToken).ConfigureAwait(false);
        string sha256Hash = Convert.ToHexStringLower(hasher.GetHashAndReset());
        string directory = ExtractDirectory(targetLocation, context);
        return (targetLocation, directory, sha256Hash, bytesWritten);
    }

    /// <inheritdoc />
    public abstract ValueTask<bool> IsHealthAsync(CancellationToken cancellationToken = default);

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
    public abstract IAsyncEnumerable<StorageEntryMetadata> ListFilesAsync(string identifier, string context, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract IAsyncEnumerable<StorageEntryMetadata> ListEntriesAsync(string identifier, string context, CancellationToken cancellationToken = default);

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
    /// Generates a standardized timestamped file name using UTC time ("yyyy-MM-dd_HH-mm-ss.fff").
    /// </summary>
    /// <param name="filePrefix">Business prefix.</param>
    /// <param name="fileExtension">File extension including the leading dot.</param>
    /// <returns>A formatted timestamped file name.</returns>
    protected string GenerateTimebasedFileName(string filePrefix, string fileExtension) => $"{filePrefix}_{GenerateTimebasedFileName(fileExtension)}";

    /// <summary>
    /// Generates a standardized timestamped file name using UTC time ("yyyy-MM-dd_HH-mm-ss.fff").
    /// </summary>
    /// <param name="fileExtension">File extension including the leading dot.</param>
    /// <returns>A formatted timestamped file name.</returns>
    protected string GenerateTimebasedFileName(string fileExtension) => $"{TimeProvider.GetUtcNow():yyyy-MM-dd_HH-mm-ss.fff}{fileExtension}";
}
