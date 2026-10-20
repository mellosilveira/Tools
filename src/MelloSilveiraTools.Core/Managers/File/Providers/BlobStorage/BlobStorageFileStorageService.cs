using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using MelloSilveiraTools.Core.Managers.File.Models;
using MelloSilveiraTools.Core.Managers.File.Streams;
using MelloSilveiraTools.Core.ResiliencePipelines;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.Core.Managers.File.Providers.BlobStorage;

/// <summary>
/// Storage-agnostic file management implementation targeting Azure Blob Storage and Azure-compatible providers.
/// Manages files using blob names and streams data directly via the Azure Blob Storage client without local disk staging for reads.
/// Writes are buffered to disk via a local storage service in the temporary directory before streaming to Azure Blob Storage to minimize memory overhead for large files.
/// Thread-safe for singleton lifecycle across concurrent and parallel asynchronous tasks.
/// </summary>
/// <remarks>
/// <para>
/// <b>Architectural Rationale: Disk Spooling vs. In-Memory Buffering</b>
/// </para>
/// <para>
/// High-throughput computational workloads—such as experimental data segmentation, numerical model simulations, and parametric sweep 
/// exports—frequently generate datasets exceeding tens or hundreds of megabytes. Buffering outgoing payloads entirely in RAM (e.g., 
/// via <see cref="MemoryStream"/>) introduces severe reliability and scalability vulnerabilities:
/// <list type="bullet">
/// <item>
/// <term>O(1) vs. O(N) Space Complexity &amp; Memory Cost Efficiency:</term>
/// Storage disk (NVMe/SSD) is orders of magnitude less expensive per gigabyte than physical RAM. By spooling data to the OS-managed
/// temporary directory, memory allocation per active write stream remains strictly constant (<c>O(1)</c>, governed by the underlying
/// <see cref="FileStream"/> page buffer of 64KB–128KB), regardless of whether the generated artifact is 1 MB or 50 GB. In contrast,
/// in-memory buffering scales linearly (<c>O(N)</c>) with file size and concurrent requests, precipitating catastrophic
/// <see cref="OutOfMemoryException"/> (OOM) failures under high concurrency.
/// </item>
/// <item>
/// <term>Garbage Collection &amp; Large Object Heap (LOH) Protection:</term>
/// In the .NET runtime, continuous byte array expansions in dynamically growing in-memory streams cross the 85,000-byte threshold,
/// landing directly on the Large Object Heap. Because the LOH is not compacted during Generation 0 or 1 GC sweeps, dynamic in-memory
/// buffering induces chronic memory fragmentation and forces expensive, blocking Generation 2 collections (Stop-the-World pauses).
/// Disk spooling completely bypasses LOH allocations during generation.
/// </item>
/// <item>
/// <term>Producer-Network Decoupling &amp; Latency Resilience:</term>
/// Writing to local disk decouples data generation from WAN throughput. The application writes at local bus speeds (GB/s via the OS page cache)
/// without stalling on cloud network latency or TCP windowing. Intermittent network degradation or slow transmission rates do not block compute 
/// threads. Furthermore, long simulation steps that experience delays between successive writes do not risk HTTP socket idle timeouts against 
/// the Azure Blob Storage API.
/// </item>
/// <item>
/// <term>Transactional Fault Isolation &amp; Zero-Leak Lifecycle:</term>
/// If a failure occurs during file calculation or data serialization, the partial artifact remains local and is safely cleaned up, preventing 
/// orphaned partial objects or corrupted versions in the blob container and avoiding wasteful Azure Storage API transaction costs. Upload 
/// transmission triggers only upon stream completion (<see cref="BlobDiskUploadStream.DisposeAsync"/>), and the enclosing <c>finally</c> 
/// block guarantees deterministic destruction of both the temporary file and its isolated upload partition directory.
/// </item>
/// </list>
/// </para>
/// </remarks>
/// <param name="logger">Logger instance for structured tracing of operations and transmission progress.</param>
/// <param name="defaultStorage">The default file storage service used to buffer writes before streaming to Blob Storage. In this version, a local file storage is used.</param>
/// <param name="containerClient">Azure Blob container client instance shared across the application lifecycle.</param>
/// <param name="settings">Azure Blob storage settings defining container, endpoints, and virtual prefixes.</param>
/// <param name="resiliencePipeline">Resilience pipeline for Azure Blob Storage operations providing retry, backoff, and circuit-breaker strategies.</param>
public class BlobStorageFileStorageService(
    ILogger<BlobStorageFileStorageService> logger,
    IFileStorageService defaultStorage,
    BlobContainerClient containerClient,
    BlobStorageSettings settings,
    BlobStorageResiliencePipeline resiliencePipeline) : FileStorageServiceBase
{
    private int _disposedState;

    /// <inheritdoc />
    public override StorageProvider Provider => StorageProvider.BlobStorage;

    /// <inheritdoc />
    public override string ResolveLocation(string context, string identifier, string fileName) => BuildDirectoryPrefix(context, identifier) + fileName;

    /// <inheritdoc />
    public override async ValueTask<Stream> OpenWriteAsync(string location, CancellationToken cancellationToken = default)
    {
        string fileName = ExtractCloudFileName(location)!;
        string uploadId = Guid.NewGuid().ToString("N");
        (Stream localStream, string tempLocation) = await defaultStorage.CreateWriteAsync(
            "blob-uploads",
            uploadId,
            fileName,
            cancellationToken).ConfigureAwait(false);

        logger.LogDebug("Starting temporary buffer for Azure Blob Storage upload. Container: {ContainerName}, Blob: {BlobName}, TempLocation: {TempLocation}", settings.ContainerName, location, tempLocation);
        return new BlobDiskUploadStream(logger, defaultStorage, containerClient.GetBlobClient(location), settings.ContainerName, location, localStream, tempLocation, resiliencePipeline, cancellationToken);
    }

    /// <inheritdoc />
    public override async ValueTask<Stream> OpenReadAsync(string location, CancellationToken cancellationToken = default)
    {
        BlobClient blobClient = containerClient.GetBlobClient(location);

        try
        {
            Response<BlobDownloadStreamingResult> response = await resiliencePipeline
                .ExecuteAsync(async _ => await blobClient.DownloadStreamingAsync(default, null, false, cancellationToken).ConfigureAwait(false))
                .ConfigureAwait(false);

            return new DelegatingDisposableStream(response.Value.Content, response.Value);
        }
        catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound
                                             || string.Equals(ex.ErrorCode, "BlobNotFound", StringComparison.OrdinalIgnoreCase)
                                             || string.Equals(ex.ErrorCode, "ContainerNotFound", StringComparison.OrdinalIgnoreCase))
        {
            throw new FileNotFoundException($"The requested blob was not found. Container: '{settings.ContainerName}', Blob: '{location}'.", location, ex);
        }
        catch (Exception ex)
        {
            LogBlobError(ex, nameof(OpenReadAsync), location);
            throw;
        }
    }

    /// <inheritdoc />
    public override async ValueTask<bool> ExistsAsync(string location, CancellationToken cancellationToken = default)
    {
        BlobClient blobClient = containerClient.GetBlobClient(location);

        try
        {
            return await resiliencePipeline.ExecuteAsync(async _ =>
            {
                Response<bool> response = await blobClient.ExistsAsync(cancellationToken).ConfigureAwait(false);
                return response.Value;
            }).ConfigureAwait(false);
        }
        catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound || ex.Status == (int)HttpStatusCode.Forbidden)
        {
            return false;
        }
        catch (Exception ex)
        {
            LogBlobError(ex, nameof(ExistsAsync), location);
            throw;
        }
    }

    /// <inheritdoc />
    public override async ValueTask<StorageEntryMetadata> GetMetadataAsync(string location, CancellationToken cancellationToken = default)
    {
        BlobClient blobClient = containerClient.GetBlobClient(location);

        try
        {
            BlobProperties properties = await resiliencePipeline.ExecuteAsync(async _ =>
            {
                Response<BlobProperties> response = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                return response.Value;
            }).ConfigureAwait(false);

            DateTimeOffset createdAt = properties.LastModified.ToUniversalTime();
            (string path, string? fileName) = ExtractCloudPath(location);
            return new StorageEntryMetadata(StorageProvider.BlobStorage, location, path, fileName, properties.ContentLength, createdAt);
        }
        catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound
                                             || string.Equals(ex.ErrorCode, "BlobNotFound", StringComparison.OrdinalIgnoreCase)
                                             || string.Equals(ex.ErrorCode, "ContainerNotFound", StringComparison.OrdinalIgnoreCase))
        {
            throw new FileNotFoundException($"The requested blob was not found. Container: '{settings.ContainerName}', Blob: '{location}'.", location, ex);
        }
        catch (Exception ex) when (ex is not FileNotFoundException)
        {
            LogBlobError(ex, nameof(GetMetadataAsync), location);
            throw;
        }
    }

    /// <inheritdoc />
    public override async ValueTask<bool> DeleteAsync(string location, CancellationToken cancellationToken = default)
    {
        BlobClient blobClient = containerClient.GetBlobClient(location);

        try
        {
            return await resiliencePipeline.ExecuteAsync(async _ =>
            {
                Response<bool> response = await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                return response.Value;
            }).ConfigureAwait(false);
        }
        catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex)
        {
            LogBlobError(ex, nameof(DeleteAsync), location);
            throw;
        }
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<StorageEntryMetadata> ListFilesAsync(
        string context,
        string identifier,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string prefix = BuildDirectoryPrefix(context, identifier);
        string? continuationToken = null;

        do
        {
            Page<BlobItem>? page;
            try
            {
                page = await resiliencePipeline
                    .ExecuteAsync(async _ =>
                    {
                        await foreach (Page<BlobItem> p in containerClient
                            .GetBlobsAsync(BlobTraits.None, BlobStates.None, prefix, cancellationToken)
                            .AsPages(continuationToken)
                            .WithCancellation(cancellationToken)
                            .ConfigureAwait(false))
                        {
                            return p;
                        }

                        return null;
                    })
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogBlobError(ex, nameof(ListFilesAsync), prefix);
                throw;
            }

            if (page is null)
                break;

            foreach (BlobItem blobItem in page.Values)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (blobItem.Name.EndsWith('/'))
                    continue;

                (string path, string? fileName) = ExtractCloudPath(blobItem.Name);
                DateTimeOffset createdAt = blobItem.Properties.LastModified?.ToUniversalTime() ?? DateTimeOffset.UtcNow;
                yield return new StorageEntryMetadata(StorageProvider.BlobStorage, blobItem.Name, path, fileName, blobItem.Properties.ContentLength ?? 0L, createdAt);
            }

            continuationToken = page.ContinuationToken;
        } while (!string.IsNullOrEmpty(continuationToken));
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<StorageEntryMetadata> ListEntriesAsync(string context, string identifier, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string prefix = BuildDirectoryPrefix(context, identifier);
        string? continuationToken = null;

        do
        {
            Page<BlobHierarchyItem>? page;
            try
            {
                page = await resiliencePipeline
                    .ExecuteAsync(async _ =>
                    {
                        await foreach (Page<BlobHierarchyItem> p in containerClient
                            .GetBlobsByHierarchyAsync(BlobTraits.None, BlobStates.None, "/", prefix, cancellationToken)
                            .AsPages(continuationToken)
                            .WithCancellation(cancellationToken)
                            .ConfigureAwait(false))
                        {
                            return p;
                        }

                        return null;
                    })
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogBlobError(ex, nameof(ListEntriesAsync), prefix);
                throw;
            }

            if (page is null)
                break;

            foreach (BlobHierarchyItem item in page.Values)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (item.IsPrefix)
                {
                    string directory = ExtractCloudDirectory(item.Prefix);
                    yield return new StorageEntryMetadata(StorageProvider.BlobStorage, item.Prefix, directory, null, 0L, DateTimeOffset.UtcNow);
                }
                else if (item.IsBlob)
                {
                    if (item.Blob.Name.EndsWith('/') || item.Blob.Name == prefix)
                        continue;

                    (string path, string? fileName) = ExtractCloudPath(item.Blob.Name);
                    DateTimeOffset createdAt = item.Blob.Properties.LastModified?.ToUniversalTime() ?? DateTimeOffset.UtcNow;
                    yield return new StorageEntryMetadata(StorageProvider.BlobStorage, item.Blob.Name, path, fileName, item.Blob.Properties.ContentLength ?? 0L, createdAt);
                }
            }

            continuationToken = page.ContinuationToken;
        } while (!string.IsNullOrEmpty(continuationToken));
    }

    private string BuildDirectoryPrefix(string context, string identifier) => BuildDirectoryPrefix(context, identifier, settings.BasePrefix);

    private void LogBlobError(Exception ex, string operation, string location)
    {
        if (ex is RequestFailedException reqEx)
        {
            logger.LogError(
                reqEx,
                "Azure Blob Storage HTTP error during {Operation}. Container: {ContainerName}, Blob: {BlobName}, Status: {Status}, ErrorCode: {ErrorCode}",
                operation,
                settings.ContainerName,
                location,
                reqEx.Status,
                reqEx.ErrorCode);
            return;
        }

        logger.LogError(
            ex,
            "Unexpected error during Azure Blob Storage {Operation}. Container: {ContainerName}, Blob: {BlobName}",
            operation,
            settings.ContainerName,
            location);
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposedState, 1) != 0)
            return;

        await defaultStorage.DisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disk-backed stream buffer that writes incoming data to an isolated local temporary file
    /// and streams the completed payload to Azure Blob Storage upon stream disposal.
    /// </summary>
    private sealed class BlobDiskUploadStream(
        ILogger logger,
        IFileStorageService defaultStorage,
        BlobClient blobClient,
        string containerName,
        string blobName,
        Stream localWriteStream,
        string tempLocation,
        BlobStorageResiliencePipeline resiliencePipeline,
        CancellationToken cancellationToken = default)
        : DiskSpoolUploadStreamBase(logger, defaultStorage, localWriteStream, tempLocation, cancellationToken)
    {
        protected override async ValueTask UploadPayloadAsync(Stream readStream, long fileSizeBytes, CancellationToken cancellationToken)
        {
            logger.LogInformation(
                "Starting transmission to Azure Blob Storage. Container: {ContainerName}, Blob: {BlobName}, TempLocation: {TempLocation}, Size: {SizeBytes} bytes",
                containerName, blobName, TempLocation, fileSizeBytes);

            try
            {
                long lastReportedBytes = 0;
                BlobUploadOptions uploadOptions = new()
                {
                    ProgressHandler = new Progress<long>(transferredBytes =>
                    {
                        if (fileSizeBytes > 0 && transferredBytes >= fileSizeBytes)
                        {
                            logger.LogDebug("Azure Blob Storage transmission reached 100%. Container: {ContainerName}, Blob: {BlobName}, Bytes: {TransferredBytes}", containerName, blobName, transferredBytes);
                        }
                        else if (transferredBytes - lastReportedBytes >= 10 * 1024 * 1024)
                        {
                            lastReportedBytes = transferredBytes;
                            logger.LogDebug("Azure Blob Storage upload progress: Container: {ContainerName}, Blob: {BlobName}, Bytes: {TransferredBytes}/{TotalBytes}", containerName, blobName, transferredBytes, fileSizeBytes);
                        }
                    })
                };

                await resiliencePipeline.ExecuteAsync(async _ =>
                {
                    if (readStream.CanSeek)
                        readStream.Seek(0, SeekOrigin.Begin);

                    return await blobClient.UploadAsync(readStream, uploadOptions, cancellationToken).ConfigureAwait(false);
                }).ConfigureAwait(false);

                logger.LogInformation("Azure Blob Storage upload completed successfully. Container: {ContainerName}, Blob: {BlobName}, Size: {SizeBytes} bytes.", containerName, blobName, fileSizeBytes);
            }
            catch (Exception ex)
            {
                if (ex is RequestFailedException reqEx)
                {
                    logger.LogError(
                        reqEx,
                        "Failed to upload to Azure Blob Storage due to HTTP error. Container: {ContainerName}, Blob: {BlobName}, TempLocation: {TempLocation}, Status: {Status}, ErrorCode: {ErrorCode}",
                        containerName, blobName, TempLocation, reqEx.Status, reqEx.ErrorCode);
                }
                else
                {
                    logger.LogError(ex, "Failed to upload to Azure Blob Storage. Container: {ContainerName}, Blob: {BlobName}, TempLocation: {TempLocation}", containerName, blobName, TempLocation);
                }

                throw;
            }
        }
    }
}
