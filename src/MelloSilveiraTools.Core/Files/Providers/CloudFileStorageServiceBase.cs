using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.Core.Files.Providers;

/// <summary>
/// Abstract base implementation for cloud object storage providers (such as Azure Blob Storage and AWS S3),
/// providing forward-slash virtual directory prefix construction and cloud object key path extraction.
/// </summary>
/// <param name="logger">Logger instance for tracing cloud storage operations and health probes.</param>
/// <param name="timeProvider">The time provider instance used for temporal operations.</param>
/// <param name="basePrefix">Mandatory root virtual prefix configured for the storage container or bucket.</param>
public abstract class CloudFileStorageServiceBase(
    ILogger logger,
    TimeProvider timeProvider,
    string basePrefix)
    : FileStorageServiceBase(timeProvider)
{
    private readonly string _normalizedBasePrefix = basePrefix.Trim('/');

    /// <inheritdoc />
    public override string ResolveLocation(string identifier, string context, string fileName) => BuildDirectoryPrefix(identifier, context) + fileName;

    /// <inheritdoc />
    public override (string Identifier, string Context, string FileName) ParseLocation(string location)
    {
        string relative = location.TrimStart('/');
        if (!string.IsNullOrEmpty(_normalizedBasePrefix))
        {
            string requiredPrefix = $"{_normalizedBasePrefix}/";
            if (!relative.StartsWith(requiredPrefix, StringComparison.Ordinal))
                throw new FormatException($"Storage location '{location}' does not conform to the canonical layout for provider '{Provider}'.");

            relative = relative[requiredPrefix.Length..];
        }

        string[] segments = relative.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length != 3 || location.EndsWith('/'))
            throw new FormatException($"Storage location '{location}' does not conform to the canonical layout for provider '{Provider}'.");

        return (segments[0], segments[1], segments[2]);
    }

    /// <summary>
    /// Executes an end-to-end health probe performing write (including local spooling and cloud upload),
    /// read with byte-for-byte payload verification, and deletion of a temporary probe object.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> when write, read verification, and deletion all succeed; otherwise, <see langword="false"/>.</returns>
    protected async ValueTask<bool> ProbeReadWriteDeleteAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        byte[] expectedPayload = Guid.NewGuid().ToByteArray();
        string probeLocation = ResolveLocation("__health", "probe", $".health-probe-{Guid.NewGuid():N}.tmp");
        bool writeSucceeded = false;
        bool readMatched = false;

        try
        {
            await using (Stream writeStream = await OpenWriteAsync(probeLocation, cancellationToken).ConfigureAwait(false))
            {
                await writeStream.WriteAsync(expectedPayload, cancellationToken).ConfigureAwait(false);
                await writeStream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();
            writeSucceeded = true;

            byte[] actualPayload;
            await using (Stream readStream = await OpenReadAsync(probeLocation, cancellationToken).ConfigureAwait(false))
            {
                using MemoryStream buffer = new(expectedPayload.Length);
                await readStream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
                actualPayload = buffer.ToArray();
            }

            readMatched = actualPayload.AsSpan().SequenceEqual(expectedPayload);
        }
        catch
        {
            if (writeSucceeded)
            {
                try
                {
                    await DeleteAsync(probeLocation, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // Best-effort cleanup so the original write or read failure is preserved.
                    logger.LogWarning(ex, "Failed to delete temporary health probe object '{ProbeLocation}' during cloud storage health check cleanup.", probeLocation);
                }
            }

            throw;
        }

        bool deleted = await DeleteAsync(probeLocation, cancellationToken).ConfigureAwait(false);
        return readMatched && deleted;
    }

    /// <summary>
    /// Builds a standardized forward-slash delimited virtual directory prefix for the active cloud provider,
    /// applying the configured base prefix.
    /// </summary>
    /// <param name="identifier">The unique identifier.</param>
    /// <param name="context">The contextual domain or category.</param>
    /// <returns>A formatted directory prefix ending with a forward slash.</returns>
    protected string BuildDirectoryPrefix(string identifier, string context)
    {
        string[] parts =
        [
            _normalizedBasePrefix,
            identifier.Trim('/'),
            context.Trim('/')
        ];

        string joined = string.Join('/', parts.Where(part => !string.IsNullOrEmpty(part)));
        return string.IsNullOrEmpty(joined) ? string.Empty : $"{joined}/";
    }

    /// <summary>
    /// Deconstructs a cloud object key or URI into its forward-slash delimited virtual directory path and file name.
    /// Ensures consistent behavior across Windows and Unix platforms without backslash replacement.
    /// </summary>
    /// <param name="location">The cloud object key or URI.</param>
    /// <returns>A tuple containing the virtual directory prefix (without trailing slash) and the file name (or <c>null</c> if the key represents a directory or ends with a slash).</returns>
    protected static (string Directory, string? FileName) ExtractCloudPath(string location)
    {
        if (string.IsNullOrEmpty(location))
            return (string.Empty, null);

        if (location.EndsWith('/'))
        {
            string normalizedDir = location.TrimEnd('/');
            int slashIndex = normalizedDir.LastIndexOf('/');
            string parentDirectory = slashIndex >= 0 ? normalizedDir[..slashIndex] : string.Empty;
            return (parentDirectory, null);
        }

        int lastSlash = location.LastIndexOf('/');
        if (lastSlash < 0)
            return (string.Empty, location);

        string directory = location[..lastSlash];
        string fileName = location[(lastSlash + 1)..];
        return (directory, string.IsNullOrEmpty(fileName) ? null : fileName);
    }
}
