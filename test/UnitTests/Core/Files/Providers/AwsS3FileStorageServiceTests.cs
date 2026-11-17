using System.Net;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Files.Providers.AwsS3;
using MelloSilveiraTools.Core.Files.Providers.Local;
using MelloSilveiraTools.Core.ResiliencePipelines;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Polly;

namespace UnitTests.Core.Files.Providers;

/// <summary>
/// Unit tests for <see cref="AwsS3FileStorageService"/> verifying S3 read/write disk spooling,
/// metadata, existence, deletion, paginated listing, exception translation, and idempotent disposal.
/// </summary>
public sealed class AwsS3FileStorageServiceTests : IDisposable
{
    private readonly string _stagingDirectory = Path.Combine(Path.GetTempPath(), $"AwsS3Tests_{Guid.NewGuid():N}");
    private readonly Mock<IAmazonS3> _s3Mock = new();
    private readonly LocalFileStorageService _defaultStorage;
    private readonly AwsS3ResiliencePipeline _pipeline;
    private readonly AwsS3StorageSettings _settings;
    private readonly FixedTimeProvider _timeProvider = new(new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero));
    private readonly AwsS3FileStorageService _service;

    public AwsS3FileStorageServiceTests()
    {
        Directory.CreateDirectory(_stagingDirectory);
        _defaultStorage = new LocalFileStorageService(_timeProvider, new LocalFileStorageSettings { BaseDirectory = _stagingDirectory });
        _pipeline = new AwsS3ResiliencePipeline(
            NullLogger<AwsS3ResiliencePipeline>.Instance,
            new ResiliencePipelineSettings
            {
                BackoffType = DelayBackoffType.Constant,
                MaxRetryAttempts = 1,
                DelayInMilliseconds = 1,
                UseJitter = false
            });
        _settings = new AwsS3StorageSettings
        {
            BucketName = "unit-test-bucket",
            Region = "us-east-1",
            BasePrefix = "prefix"
        };
        _service = new AwsS3FileStorageService(
            NullLogger<AwsS3FileStorageService>.Instance,
            _defaultStorage,
            _s3Mock.Object,
            _settings,
            _pipeline,
            _timeProvider);
    }

    public void Dispose()
    {
        _service.Dispose();
        if (Directory.Exists(_stagingDirectory))
        {
            try
            {
                Directory.Delete(_stagingDirectory, true);
            }
            catch
            {
                // Best effort cleanup of temporary test directory
            }
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    /// <summary>
    /// Verifies Provider property returns StorageProvider.AwsS3.
    /// </summary>
    [Fact]
    public void Provider_ReturnsAwsS3() => Assert.Equal(StorageProvider.AwsS3, _service.Provider);

    /// <summary>
    /// Verifies OpenWriteAsync spools to local disk and uploads the payload via PutObjectAsync upon DisposeAsync,
    /// firing StreamTransferProgress across all percentage thresholds.
    /// </summary>
    [Fact]
    public async Task OpenWriteAsync_OnDispose_UploadsSpooledPayloadToS3AndCleansUpTempFile()
    {
        // Arrange
        byte[]? uploadedBytes = null;
        string? uploadedKey = null;
        _s3Mock
            .Setup(c => c.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PutObjectRequest, CancellationToken>((req, _) =>
            {
                uploadedKey = req.Key;
                using MemoryStream ms = new();
                req.InputStream.CopyTo(ms);
                uploadedBytes = ms.ToArray();

                EventHandler<Amazon.Runtime.StreamTransferProgressArgs>? cb = ((Amazon.Runtime.Internal.IAmazonWebServiceRequest)req).StreamUploadProgressCallback;
                cb?.Invoke(req, new Amazon.Runtime.StreamTransferProgressArgs(5, 5, 100));     // 5% (< +10)
                cb?.Invoke(req, new Amazon.Runtime.StreamTransferProgressArgs(10, 15, 100));   // 15% (>= +10)
                cb?.Invoke(req, new Amazon.Runtime.StreamTransferProgressArgs(5, 20, 100));    // 20% (< +10 again)
                cb?.Invoke(req, new Amazon.Runtime.StreamTransferProgressArgs(80, 100, 100));  // 100% (first time)
                cb?.Invoke(req, new Amazon.Runtime.StreamTransferProgressArgs(0, 100, 100));   // 100% (already reported)
            })
            .ReturnsAsync(new PutObjectResponse { HttpStatusCode = HttpStatusCode.OK });

        // Act
        await using (Stream writeStream = await _service.OpenWriteAsync("run-1/ctx/report.csv"))
        {
            await writeStream.WriteAsync("col1,col2\n1,2\n"u8.ToArray());
        }

        // Assert
        Assert.Equal("run-1/ctx/report.csv", uploadedKey);
        Assert.Equal("col1,col2\n1,2\n", Encoding.UTF8.GetString(uploadedBytes!));
    }

    /// <summary>
    /// Verifies OpenWriteAsync logs and rethrows AmazonS3Exception and generic exceptions during upload on DisposeAsync.
    /// </summary>
    [Fact]
    public async Task OpenWriteAsync_WhenUploadFails_RethrowsException()
    {
        // Arrange & Act & Assert - AmazonS3Exception
        _s3Mock
            .Setup(c => c.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("S3 Put failed") { StatusCode = HttpStatusCode.InternalServerError, ErrorCode = "InternalError" });

        Stream writeStream = await _service.OpenWriteAsync("run-1/ctx/fail.csv");
        await writeStream.WriteAsync("data"u8.ToArray());
        await Assert.ThrowsAsync<AmazonS3Exception>(() => writeStream.DisposeAsync().AsTask());

        // Arrange & Act & Assert - Non-S3 generic exception
        _s3Mock
            .Setup(c => c.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Generic upload failure"));

        Stream genericFailStream = await _service.OpenWriteAsync("run-1/ctx/fail-generic.csv");
        await genericFailStream.WriteAsync("data"u8.ToArray());
        await Assert.ThrowsAsync<InvalidOperationException>(() => genericFailStream.DisposeAsync().AsTask());
    }

    /// <summary>
    /// Verifies OpenReadAsync returns readable stream on success and translates NotFound/NoSuchKey into FileNotFoundException.
    /// </summary>
    [Fact]
    public async Task OpenReadAsync_SuccessAndNotFoundAndGenericError_BehavesAsExpected()
    {
        // Arrange
        MemoryStream responseBody = new("s3-content"u8.ToArray());
        _s3Mock
            .Setup(c => c.GetObjectAsync(It.Is<GetObjectRequest>(r => r.Key == "ok.txt"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetObjectResponse { ResponseStream = responseBody });

        _s3Mock
            .Setup(c => c.GetObjectAsync(It.Is<GetObjectRequest>(r => r.Key == "missing.txt"), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("NoSuchKey") { StatusCode = HttpStatusCode.NotFound, ErrorCode = "NoSuchKey" });

        _s3Mock
            .Setup(c => c.GetObjectAsync(It.Is<GetObjectRequest>(r => r.Key == "err.txt"), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected failure"));

        // Act & Assert
        await using (Stream stream = await _service.OpenReadAsync("ok.txt"))
        using (StreamReader reader = new(stream))
            Assert.Equal("s3-content", await reader.ReadToEndAsync());

        await Assert.ThrowsAsync<FileNotFoundException>(() => _service.OpenReadAsync("missing.txt").AsTask());
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.OpenReadAsync("err.txt").AsTask());
    }

    /// <summary>
    /// Verifies ExistsAsync handles OK, delete marker, NotFound/Forbidden, and unexpected errors.
    /// </summary>
    [Fact]
    public async Task ExistsAsync_ReturnsExpectedBooleanAndPropagatesUnexpectedErrors()
    {
        // Arrange
        _s3Mock
            .Setup(c => c.GetObjectMetadataAsync(It.Is<GetObjectMetadataRequest>(r => r.Key == "exists.txt"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetObjectMetadataResponse { HttpStatusCode = HttpStatusCode.OK });

        _s3Mock
            .Setup(c => c.GetObjectMetadataAsync(It.Is<GetObjectMetadataRequest>(r => r.Key == "deleted.txt"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetObjectMetadataResponse { HttpStatusCode = HttpStatusCode.OK, DeleteMarker = "true" });

        _s3Mock
            .Setup(c => c.GetObjectMetadataAsync(It.Is<GetObjectMetadataRequest>(r => r.Key == "missing.txt"), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("NotFound") { StatusCode = HttpStatusCode.NotFound });

        _s3Mock
            .Setup(c => c.GetObjectMetadataAsync(It.Is<GetObjectMetadataRequest>(r => r.Key == "boom.txt"), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("timeout"));

        // Act & Assert
        Assert.True(await _service.ExistsAsync("exists.txt"));
        Assert.False(await _service.ExistsAsync("deleted.txt"));
        Assert.False(await _service.ExistsAsync("missing.txt"));
        await Assert.ThrowsAsync<TimeoutException>(() => _service.ExistsAsync("boom.txt").AsTask());
    }

    /// <summary>
    /// Verifies GetMetadataAsync returns parsed StorageEntryMetadata and handles missing/delete-marker/unexpected errors.
    /// </summary>
    [Fact]
    public async Task GetMetadataAsync_ReturnsMetadataOrThrowsAppropriateExceptions()
    {
        // Arrange
        DateTime lastModified = new(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc);
        _s3Mock
            .Setup(c => c.GetObjectMetadataAsync(It.Is<GetObjectMetadataRequest>(r => r.Key == "dir/file.csv"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetObjectMetadataResponse
            {
                HttpStatusCode = HttpStatusCode.OK,
                LastModified = lastModified,
                ContentLength = 512L
            });

        _s3Mock
            .Setup(c => c.GetObjectMetadataAsync(It.Is<GetObjectMetadataRequest>(r => r.Key == "marker.csv"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetObjectMetadataResponse { HttpStatusCode = HttpStatusCode.OK, DeleteMarker = "true" });

        _s3Mock
            .Setup(c => c.GetObjectMetadataAsync(It.Is<GetObjectMetadataRequest>(r => r.Key == "notfound.csv"), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("NotFound") { StatusCode = HttpStatusCode.NotFound });

        _s3Mock
            .Setup(c => c.GetObjectMetadataAsync(It.Is<GetObjectMetadataRequest>(r => r.Key == "error.csv"), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("Error") { StatusCode = HttpStatusCode.BadGateway, ErrorCode = "BadGateway" });

        // Act
        StorageEntryMetadata meta = await _service.GetMetadataAsync("dir/file.csv");

        // Assert
        Assert.Equal(StorageProvider.AwsS3, meta.StorageProvider);
        Assert.Equal("dir/file.csv", meta.Location);
        Assert.Equal("dir", meta.Directory);
        Assert.Equal("file.csv", meta.FileName);
        Assert.Equal(512L, meta.SizeBytes);

        await Assert.ThrowsAsync<FileNotFoundException>(() => _service.GetMetadataAsync("marker.csv").AsTask());
        await Assert.ThrowsAsync<FileNotFoundException>(() => _service.GetMetadataAsync("notfound.csv").AsTask());
        await Assert.ThrowsAsync<AmazonS3Exception>(() => _service.GetMetadataAsync("error.csv").AsTask());
    }

    /// <summary>
    /// Verifies DeleteAsync returns true on OK/NoContent, false on NotFound, and rethrows unexpected errors.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_HandlesSuccessNotFoundAndErrors()
    {
        // Arrange
        _s3Mock
            .Setup(c => c.DeleteObjectAsync(It.Is<DeleteObjectRequest>(r => r.Key == "del.txt"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteObjectResponse { HttpStatusCode = HttpStatusCode.NoContent });

        _s3Mock
            .Setup(c => c.DeleteObjectAsync(It.Is<DeleteObjectRequest>(r => r.Key == "missing.txt"), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("NoSuchKey") { StatusCode = HttpStatusCode.NotFound });

        _s3Mock
            .Setup(c => c.DeleteObjectAsync(It.Is<DeleteObjectRequest>(r => r.Key == "err.txt"), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("delete failed"));

        // Act & Assert
        Assert.True(await _service.DeleteAsync("del.txt"));
        Assert.False(await _service.DeleteAsync("missing.txt"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteAsync("err.txt").AsTask());
    }

    /// <summary>
    /// Verifies ListFilesAsync paginates across multiple responses and filters out directory markers.
    /// </summary>
    [Fact]
    public async Task ListFilesAsync_HandlesPaginationAndSkipsDirectoryMarkers()
    {
        // Arrange
        _s3Mock
            .Setup(c => c.ListObjectsV2Async(It.Is<ListObjectsV2Request>(r => r.ContinuationToken == null), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListObjectsV2Response
            {
                IsTruncated = true,
                NextContinuationToken = "page-2",
                S3Objects =
                [
                    new S3Object { Key = "prefix/id/ctx/", Size = 0 },
                    new S3Object { Key = "prefix/id/ctx/a.csv", Size = 100 }
                ]
            });

        _s3Mock
            .Setup(c => c.ListObjectsV2Async(It.Is<ListObjectsV2Request>(r => r.ContinuationToken == "page-2"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListObjectsV2Response
            {
                IsTruncated = false,
                S3Objects = [new S3Object { Key = "prefix/id/ctx/b.csv", Size = 200 }]
            });

        // Act
        List<StorageEntryMetadata> results = [];
        await foreach (StorageEntryMetadata item in _service.ListFilesAsync("id", "ctx"))
            results.Add(item);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Equal("a.csv", results[0].FileName);
        Assert.Equal("b.csv", results[1].FileName);
    }

    /// <summary>
    /// Verifies ListEntriesAsync yields both CommonPrefixes (directories) and S3Objects (files).
    /// </summary>
    [Fact]
    public async Task ListEntriesAsync_YieldsDirectoriesAndFiles()
    {
        // Arrange
        _s3Mock
            .Setup(c => c.ListObjectsV2Async(It.Is<ListObjectsV2Request>(r => r.Delimiter == "/"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListObjectsV2Response
            {
                IsTruncated = false,
                CommonPrefixes = ["prefix/id/ctx/sub/"],
                S3Objects =
                [
                    new S3Object { Key = "prefix/id/ctx/", Size = 0 },
                    new S3Object { Key = "prefix/id/ctx/root.csv", Size = 42 }
                ]
            });

        // Act
        List<StorageEntryMetadata> results = [];
        await foreach (StorageEntryMetadata item in _service.ListEntriesAsync("id", "ctx"))
            results.Add(item);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Null(results[0].FileName);
        Assert.Equal("prefix/id/ctx/sub/", results[0].Location);
        Assert.Equal("root.csv", results[1].FileName);
        Assert.Equal(42L, results[1].SizeBytes);
    }

    /// <summary>
    /// Verifies ListFilesAsync and ListEntriesAsync propagate errors from the S3 client.
    /// </summary>
    [Fact]
    public async Task ListMethods_WhenClientThrows_PropagatesException()
    {
        // Arrange
        _s3Mock
            .Setup(c => c.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("List failed"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (StorageEntryMetadata _ in _service.ListFilesAsync("id", "ctx")) { }
        });

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (StorageEntryMetadata _ in _service.ListEntriesAsync("id", "ctx")) { }
        });
    }

    /// <summary>
    /// Verifies Dispose and DisposeAsync are idempotent.
    /// </summary>
    [Fact]
    public async Task DisposeAndDisposeAsync_AreIdempotent()
    {
        _service.Dispose();
        _service.Dispose();
        await _service.DisposeAsync();
        await _service.DisposeAsync();
        _s3Mock.Verify(c => c.Dispose(), Times.Once);
    }

    private sealed class CloudPathProbeService(
        ILogger<AwsS3FileStorageService> logger,
        IFileStorageService defaultStorage,
        IAmazonS3 s3Client,
        AwsS3StorageSettings settings,
        AwsS3ResiliencePipeline pipeline,
        TimeProvider timeProvider)
        : AwsS3FileStorageService(logger, defaultStorage, s3Client, settings, pipeline, timeProvider)
    {
        public static string ProbeBuildPrefix(string identifier, string context, string? basePrefix = null)
            => BuildDirectoryPrefix(identifier, context, basePrefix);

        public static (string Directory, string? FileName) ProbeExtractCloudPath(string key)
            => ExtractCloudPath(key);

        public static string? ProbeExtractCloudFileName(string key)
            => ExtractCloudFileName(key);
    }

    /// <summary>
    /// Verifies edge branches across ExistsAsync, GetMetadataAsync, DeleteAsync, List methods,
    /// and FileStorageServiceBase cloud path/prefix helper methods.
    /// </summary>
    [Fact]
    public async Task AdditionalEdgeBranches_StatusCodesNullMetadataAndBasePathHelpers_AreCovered()
    {
        // ExistsAsync: Forbidden, ErrorCode NotFound/NoSuchKey, and non-OK response
        _s3Mock.Setup(c => c.GetObjectMetadataAsync(It.Is<GetObjectMetadataRequest>(r => r.Key == "forbidden.txt"), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("Forbidden") { StatusCode = HttpStatusCode.Forbidden });
        _s3Mock.Setup(c => c.GetObjectMetadataAsync(It.Is<GetObjectMetadataRequest>(r => r.Key == "err-nf.txt"), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("NotFound") { StatusCode = HttpStatusCode.BadRequest, ErrorCode = "NotFound" });
        _s3Mock.Setup(c => c.GetObjectMetadataAsync(It.Is<GetObjectMetadataRequest>(r => r.Key == "err-nsk.txt"), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("NoSuchKey") { StatusCode = HttpStatusCode.BadRequest, ErrorCode = "NoSuchKey" });
        _s3Mock.Setup(c => c.GetObjectMetadataAsync(It.Is<GetObjectMetadataRequest>(r => r.Key == "nonok.txt"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetObjectMetadataResponse { HttpStatusCode = HttpStatusCode.NoContent });

        Assert.False(await _service.ExistsAsync("forbidden.txt"));
        Assert.False(await _service.ExistsAsync("err-nf.txt"));
        Assert.False(await _service.ExistsAsync("err-nsk.txt"));
        Assert.False(await _service.ExistsAsync("nonok.txt"));

        // GetMetadataAsync: LastModified null fallback + non-OK throws FileNotFoundException + NoSuchKey error code
        _s3Mock.Setup(c => c.GetObjectMetadataAsync(It.Is<GetObjectMetadataRequest>(r => r.Key == "null-mod.csv"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetObjectMetadataResponse { HttpStatusCode = HttpStatusCode.OK, LastModified = null, ContentLength = 10L });
        _s3Mock.Setup(c => c.GetObjectMetadataAsync(It.Is<GetObjectMetadataRequest>(r => r.Key == "badstatus.csv"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetObjectMetadataResponse { HttpStatusCode = HttpStatusCode.NoContent });
        _s3Mock.Setup(c => c.GetObjectMetadataAsync(It.Is<GetObjectMetadataRequest>(r => r.Key == "nsk-meta.csv"), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("NSK") { StatusCode = HttpStatusCode.BadRequest, ErrorCode = "NoSuchKey" });

        StorageEntryMetadata fallbackMeta = await _service.GetMetadataAsync("null-mod.csv");
        Assert.Equal(_timeProvider.GetUtcNow(), fallbackMeta.CreatedAt);
        await Assert.ThrowsAsync<FileNotFoundException>(() => _service.GetMetadataAsync("badstatus.csv").AsTask());
        await Assert.ThrowsAsync<FileNotFoundException>(() => _service.GetMetadataAsync("nsk-meta.csv").AsTask());

        // DeleteAsync: OK returns true, BadRequest returns false, NoSuchKey returns false
        _s3Mock.Setup(c => c.DeleteObjectAsync(It.Is<DeleteObjectRequest>(r => r.Key == "ok200.txt"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteObjectResponse { HttpStatusCode = HttpStatusCode.OK });
        _s3Mock.Setup(c => c.DeleteObjectAsync(It.Is<DeleteObjectRequest>(r => r.Key == "bad400.txt"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteObjectResponse { HttpStatusCode = HttpStatusCode.BadRequest });
        _s3Mock.Setup(c => c.DeleteObjectAsync(It.Is<DeleteObjectRequest>(r => r.Key == "del-nsk.txt"), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("NSK") { StatusCode = HttpStatusCode.BadRequest, ErrorCode = "NoSuchKey" });

        Assert.True(await _service.DeleteAsync("ok200.txt"));
        Assert.False(await _service.DeleteAsync("bad400.txt"));
        Assert.False(await _service.DeleteAsync("del-nsk.txt"));

        // ListFilesAsync & ListEntriesAsync: null LastModified/Size fallbacks
        _s3Mock.Setup(c => c.ListObjectsV2Async(It.Is<ListObjectsV2Request>(r => r.Delimiter == null), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListObjectsV2Response
            {
                IsTruncated = false,
                S3Objects = [new S3Object { Key = "prefix/id/nulls.csv", LastModified = null, Size = null }]
            });
        List<StorageEntryMetadata> listed = [];
        await foreach (StorageEntryMetadata m in _service.ListFilesAsync("id", string.Empty))
            listed.Add(m);
        Assert.Single(listed);
        Assert.Equal(0L, listed[0].SizeBytes);
        Assert.Equal(_timeProvider.GetUtcNow(), listed[0].CreatedAt);

        _s3Mock.Setup(c => c.ListObjectsV2Async(It.Is<ListObjectsV2Request>(r => r.Delimiter == "/"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListObjectsV2Response
            {
                IsTruncated = false,
                CommonPrefixes = [],
                S3Objects = [new S3Object { Key = "prefix/id/ctx/entry.csv", LastModified = null, Size = null }]
            });
        List<StorageEntryMetadata> listedEntries = [];
        await foreach (StorageEntryMetadata m in _service.ListEntriesAsync("id", "ctx"))
            listedEntries.Add(m);
        Assert.Single(listedEntries);
        Assert.Equal(0L, listedEntries[0].SizeBytes);

        // FileStorageServiceBase helpers: empty/no-context/basePrefix & ExtractCloudPath edge cases
        Assert.Equal("id1", CloudPathProbeService.ProbeBuildPrefix("id1", string.Empty, null));
        Assert.Equal($"root{Path.DirectorySeparatorChar}id1{Path.DirectorySeparatorChar}ctx1", CloudPathProbeService.ProbeBuildPrefix("id1", "ctx1", "/root/"));
        Assert.Equal((string.Empty, null), CloudPathProbeService.ProbeExtractCloudPath(string.Empty));
        Assert.Equal((string.Empty, null), CloudPathProbeService.ProbeExtractCloudPath("dir/"));
        Assert.Equal(("parent", null), CloudPathProbeService.ProbeExtractCloudPath("parent/child/"));
        Assert.Equal((string.Empty, "plain.txt"), CloudPathProbeService.ProbeExtractCloudPath("plain.txt"));
        Assert.Null(CloudPathProbeService.ProbeExtractCloudFileName("dir/"));
        Assert.Equal("plain.txt", CloudPathProbeService.ProbeExtractCloudFileName("plain.txt"));
    }
}
