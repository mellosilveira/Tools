using Amazon.S3;
using Amazon.S3.Model;
using MelloSilveiraTools.Core.Managers.File.Models;
using MelloSilveiraTools.Core.Managers.File.Streams;
using MelloSilveiraTools.Core.ResiliencePipelines;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.Core.Managers.File.Providers.AwsS3;

/// <summary>
/// Storage-agnostic file management implementation targeting Amazon Simple Storage Service (AWS S3) and S3-compatible providers.
/// Manages files using S3 object keys and streams data directly via the AWS S3 client without local disk staging for reads.
/// Writes are buffered to disk via a local storage service in the temporary directory before streaming to S3 to minimize memory overhead for large files.
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
/// the AWS S3 API.
/// </item>
/// <item>
/// <term>Transactional Fault Isolation &amp; Zero-Leak Lifecycle:</term>
/// If a failure occurs during file calculation or data serialization, the partial artifact remains local and is safely cleaned up, preventing 
/// orphaned partial objects or corrupted versions in the S3 bucket and avoiding wasteful AWS API PutObject costs. Upload transmission triggers 
/// only upon stream completion (<see cref="S3DiskUploadStream.DisposeAsync"/>), and the enclosing <c>finally</c> block guarantees deterministic 
/// destruction of both the temporary file and its isolated upload partition directory.
/// </item>
/// </list>
/// </para>
/// </remarks>
/// <param name="logger">Logger instance for structured tracing of operations and transmission progress.</param>
/// <param name="defaultStorage">The default file storage service used to buffer writes before streaming to S3. In this version, a local file storage is used.</param>
/// <param name="amazonS3Client">Amazon S3 client instance shared across the application lifecycle.</param>
/// <param name="settings">AWS S3 storage settings defining bucket, region, credentials, and virtual prefixes.</param>
/// <param name="resiliencePipeline">Resilience pipeline for AWS S3 operations providing retry, backoff, and circuit-breaker strategies.</param>
/// <param name="timeProvider">The time provider instance used for temporal operations.</param>
public class AwsS3FileStorageService(
    ILogger<AwsS3FileStorageService> logger,
    IFileStorageService defaultStorage,
    IAmazonS3 amazonS3Client,
    AwsS3StorageSettings settings,
    AwsS3ResiliencePipeline resiliencePipeline,
    TimeProvider timeProvider) : FileStorageServiceBase(timeProvider)
{
    private int _disposedState;

    /// <inheritdoc />
    public override StorageProvider Provider => StorageProvider.AwsS3;

    /// <inheritdoc />
    public override string ResolveLocation(string context, string identifier, string fileName) => BuildDirectoryPrefix(context, identifier) + fileName;

    /// <inheritdoc />
    public override async ValueTask<Stream> OpenWriteAsync(string location, CancellationToken cancellationToken = default)
    {
        string fileName = ExtractCloudFileName(location)!;
        string uploadId = Guid.NewGuid().ToString("N");
        (Stream localStream, string tempLocation) = await defaultStorage.CreateWriteAsync(
            "s3-uploads",
            uploadId,
            fileName,
            cancellationToken).ConfigureAwait(false);

        logger.LogDebug("Starting temporary buffer for AWS S3 upload. Bucket: {BucketName}, Key: {Key}, TempLocation: {TempLocation}", settings.BucketName, location, tempLocation);
        return new S3DiskUploadStream(logger, defaultStorage, amazonS3Client, settings.BucketName, location, localStream, tempLocation, resiliencePipeline, cancellationToken);
    }

    /// <inheritdoc />
    public override async ValueTask<Stream> OpenReadAsync(string location, CancellationToken cancellationToken = default)
    {
        GetObjectRequest request = new() { BucketName = settings.BucketName, Key = location };

        try
        {
            GetObjectResponse response = await resiliencePipeline
                .ExecuteAsync(async _ => await amazonS3Client.GetObjectAsync(request, cancellationToken).ConfigureAwait(false))
                .ConfigureAwait(false);

            return new DelegatingDisposableStream(response.ResponseStream, response);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound
                                        || string.Equals(ex.ErrorCode, "NotFound", StringComparison.OrdinalIgnoreCase)
                                        || string.Equals(ex.ErrorCode, "NoSuchKey", StringComparison.OrdinalIgnoreCase))
        {
            throw new FileNotFoundException($"The requested S3 object was not found. Bucket: '{settings.BucketName}', Key: '{location}'.", location, ex);
        }
        catch (Exception ex)
        {
            LogS3Error(ex, nameof(OpenReadAsync), location);
            throw;
        }
    }

    /// <inheritdoc />
    public override async ValueTask<bool> ExistsAsync(string location, CancellationToken cancellationToken = default)
    {
        try
        {
            return await resiliencePipeline.ExecuteAsync(async _ =>
            {
                GetObjectMetadataRequest request = new() { BucketName = settings.BucketName, Key = location };
                GetObjectMetadataResponse response = await amazonS3Client.GetObjectMetadataAsync(request, cancellationToken).ConfigureAwait(false);
                return response.HttpStatusCode == HttpStatusCode.OK && !string.Equals(response.DeleteMarker, "true", StringComparison.OrdinalIgnoreCase);
            }).ConfigureAwait(false);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound
                                        || ex.StatusCode == HttpStatusCode.Forbidden
                                        || string.Equals(ex.ErrorCode, "NotFound", StringComparison.OrdinalIgnoreCase)
                                        || string.Equals(ex.ErrorCode, "NoSuchKey", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        catch (Exception ex)
        {
            LogS3Error(ex, nameof(ExistsAsync), location);
            throw;
        }
    }

    /// <inheritdoc />
    public override async ValueTask<StorageEntryMetadata> GetMetadataAsync(string location, CancellationToken cancellationToken = default)
    {
        try
        {
            GetObjectMetadataResponse response = await resiliencePipeline.ExecuteAsync(async _ =>
            {
                GetObjectMetadataRequest request = new() { BucketName = settings.BucketName, Key = location };
                return await amazonS3Client.GetObjectMetadataAsync(request, cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);

            if (response.HttpStatusCode != HttpStatusCode.OK || string.Equals(response.DeleteMarker, "true", StringComparison.OrdinalIgnoreCase))
                throw new FileNotFoundException($"The requested S3 object does not exist or has been deleted. Bucket: '{settings.BucketName}', Key: '{location}'.", location);

            DateTimeOffset createdAt = response.LastModified?.ToUniversalTime() ?? TimeProvider.GetUtcNow();
            (string path, string? fileName) = ExtractCloudPath(location);
            return new StorageEntryMetadata(StorageProvider.AwsS3, location, path, fileName, response.ContentLength, createdAt);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound
                                        || string.Equals(ex.ErrorCode, "NotFound", StringComparison.OrdinalIgnoreCase)
                                        || string.Equals(ex.ErrorCode, "NoSuchKey", StringComparison.OrdinalIgnoreCase))
        {
            throw new FileNotFoundException($"The requested S3 object was not found. Bucket: '{settings.BucketName}', Key: '{location}'.", location, ex);
        }
        catch (Exception ex) when (ex is not FileNotFoundException)
        {
            LogS3Error(ex, nameof(GetMetadataAsync), location);
            throw;
        }
    }

    /// <inheritdoc />
    public override async ValueTask<bool> DeleteAsync(string location, CancellationToken cancellationToken = default)
    {
        try
        {
            return await resiliencePipeline.ExecuteAsync(async _ =>
            {
                DeleteObjectRequest request = new() { BucketName = settings.BucketName, Key = location };
                DeleteObjectResponse response = await amazonS3Client.DeleteObjectAsync(request, cancellationToken).ConfigureAwait(false);
                return response.HttpStatusCode is HttpStatusCode.NoContent or HttpStatusCode.OK;
            }).ConfigureAwait(false);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound
                                        || string.Equals(ex.ErrorCode, "NotFound", StringComparison.OrdinalIgnoreCase)
                                        || string.Equals(ex.ErrorCode, "NoSuchKey", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        catch (Exception ex)
        {
            LogS3Error(ex, nameof(DeleteAsync), location);
            throw;
        }
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<StorageEntryMetadata> ListFilesAsync(string context, string identifier, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string prefix = BuildDirectoryPrefix(context, identifier);
        ListObjectsV2Request request = new() { BucketName = settings.BucketName, Prefix = prefix };

        ListObjectsV2Response response;
        do
        {
            try
            {
                response = await resiliencePipeline
                    .ExecuteAsync(async _ => await amazonS3Client.ListObjectsV2Async(request, cancellationToken).ConfigureAwait(false))
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogS3Error(ex, nameof(ListFilesAsync), prefix);
                throw;
            }

            foreach (S3Object s3Obj in response.S3Objects)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (s3Obj.Key.EndsWith('/'))
                {
                    continue;
                }

                (string path, string? fileName) = ExtractCloudPath(s3Obj.Key);
                DateTimeOffset createdAt = s3Obj.LastModified?.ToUniversalTime() ?? TimeProvider.GetUtcNow();
                yield return new StorageEntryMetadata(StorageProvider.AwsS3, s3Obj.Key, path, fileName, s3Obj.Size ?? 0L, createdAt);
            }

            request.ContinuationToken = response.NextContinuationToken;
        } while (response.IsTruncated == true);
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<StorageEntryMetadata> ListEntriesAsync(string context, string identifier, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string prefix = BuildDirectoryPrefix(context, identifier);
        ListObjectsV2Request request = new()
        {
            BucketName = settings.BucketName,
            Prefix = prefix,
            Delimiter = "/"
        };

        ListObjectsV2Response response;
        do
        {
            try
            {
                response = await resiliencePipeline
                    .ExecuteAsync(async _ => await amazonS3Client.ListObjectsV2Async(request, cancellationToken).ConfigureAwait(false))
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogS3Error(ex, nameof(ListEntriesAsync), prefix);
                throw;
            }

            foreach (string commonPrefix in response.CommonPrefixes)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string directory = ExtractCloudDirectory(commonPrefix);
                yield return new StorageEntryMetadata(StorageProvider.AwsS3, commonPrefix, directory, null, 0L, TimeProvider.GetUtcNow());
            }

            foreach (S3Object s3Obj in response.S3Objects)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (s3Obj.Key.EndsWith('/') || s3Obj.Key == prefix)
                {
                    continue;
                }

                (string path, string? fileName) = ExtractCloudPath(s3Obj.Key);
                DateTimeOffset createdAt = s3Obj.LastModified?.ToUniversalTime() ?? TimeProvider.GetUtcNow();
                yield return new StorageEntryMetadata(StorageProvider.AwsS3, s3Obj.Key, path, fileName, s3Obj.Size ?? 0L, createdAt);
            }

            request.ContinuationToken = response.NextContinuationToken;
        } while (response.IsTruncated == true);
    }

    private string BuildDirectoryPrefix(string context, string identifier) => BuildDirectoryPrefix(context, identifier, settings.BasePrefix);

    private void LogS3Error(Exception ex, string operation, string location)
    {
        if (ex is AmazonS3Exception s3Ex)
        {
            logger.LogError(
                s3Ex,
                "AWS S3 HTTP error during {Operation}. Bucket: {BucketName}, Key: {Key}, StatusCode: {StatusCode}, ErrorCode: {ErrorCode}, RequestId: {RequestId}, AmazonId2: {AmazonId2}",
                operation,
                settings.BucketName,
                location,
                (int)s3Ex.StatusCode,
                s3Ex.ErrorCode,
                s3Ex.RequestId,
                s3Ex.AmazonId2);
            return;
        }

        logger.LogError(
            ex,
            "Unexpected error during AWS S3 {Operation}. Bucket: {BucketName}, Key: {Key}",
            operation,
            settings.BucketName,
            location);
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        if (Interlocked.Exchange(ref _disposedState, 1) != 0)
            return;

        amazonS3Client.Dispose();
        defaultStorage.Dispose();

        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposedState, 1) != 0)
            return;

        amazonS3Client.Dispose();
        await defaultStorage.DisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disk-backed stream buffer that writes incoming data to an isolated local temporary file
    /// and streams the completed payload to AWS S3 upon stream disposal.
    /// </summary>
    private sealed class S3DiskUploadStream(
        ILogger logger,
        IFileStorageService defaultStorage,
        IAmazonS3 client,
        string bucketName,
        string key,
        Stream localWriteStream,
        string tempLocation,
        AwsS3ResiliencePipeline resiliencePipeline,
        CancellationToken cancellationToken = default)
        : DiskSpoolUploadStreamBase(logger, defaultStorage, localWriteStream, tempLocation, cancellationToken)
    {
        protected override async ValueTask UploadPayloadAsync(Stream readStream, long fileSizeBytes, CancellationToken cancellationToken)
        {
            logger.LogInformation(
                "Starting transmission to AWS S3. Bucket: {BucketName}, Key: {Key}, TempLocation: {TempLocation}, Size: {SizeBytes} bytes",
                bucketName, key, TempLocation, fileSizeBytes);

            try
            {
                PutObjectRequest request = new()
                {
                    BucketName = bucketName,
                    Key = key,
                    InputStream = readStream,
                    AutoCloseStream = false,
                    AutoResetStreamPosition = true
                };

                int lastReportedPercent = 0;
                request.StreamTransferProgress += (sender, args) =>
                {
                    if (args.PercentDone >= 100 || args.TransferredBytes == args.TotalBytes)
                    {
                        if (lastReportedPercent < 100)
                        {
                            lastReportedPercent = 100;
                            logger.LogDebug("AWS S3 transmission reached 100%. Bucket: {BucketName}, Key: {Key}, Bytes: {TransferredBytes}", bucketName, key, args.TransferredBytes);
                        }
                    }
                    else if (args.PercentDone >= lastReportedPercent + 10)
                    {
                        lastReportedPercent = (args.PercentDone / 10) * 10;
                        logger.LogDebug("AWS S3 upload progress: {PercentDone}%. Bucket: {BucketName}, Key: {Key}, Bytes: {TransferredBytes}/{TotalBytes}", lastReportedPercent, bucketName, key, args.TransferredBytes, args.TotalBytes);
                    }
                };

                await resiliencePipeline.ExecuteAsync(async _ =>
                {
                    if (readStream.CanSeek)
                    {
                        readStream.Seek(0, SeekOrigin.Begin);
                    }

                    return await client.PutObjectAsync(request, cancellationToken).ConfigureAwait(false);
                }).ConfigureAwait(false);

                logger.LogInformation("AWS S3 upload completed successfully. Bucket: {BucketName}, Key: {Key}, Size: {SizeBytes} bytes.", bucketName, key, fileSizeBytes);
            }
            catch (Exception ex)
            {
                if (ex is AmazonS3Exception s3Ex)
                {
                    logger.LogError(
                        s3Ex,
                        "Failed to upload to AWS S3 due to HTTP/S3 error. Bucket: {BucketName}, Key: {Key}, TempLocation: {TempLocation}, StatusCode: {StatusCode}, ErrorCode: {ErrorCode}, RequestId: {RequestId}, AmazonId2: {AmazonId2}",
                        bucketName, key, TempLocation, (int)s3Ex.StatusCode, s3Ex.ErrorCode, s3Ex.RequestId, s3Ex.AmazonId2);
                }
                else
                {
                    logger.LogError(ex, "Failed to upload to AWS S3. Bucket: {BucketName}, Key: {Key}, TempLocation: {TempLocation}", bucketName, key, TempLocation);
                }

                throw;
            }
        }
    }
}
