using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers.BlobStorage;
using MelloSilveiraTools.Core.Files.Providers.Local;
using MelloSilveiraTools.Core.ResiliencePipelines;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Polly;
using System.Net;

namespace UnitTests.Core.Files.Providers;

/// <summary>
/// Unit tests for <see cref="BlobStorageFileStorageService"/> verifying Azure Blob read/write disk spooling,
/// metadata, existence, deletion, paginated listing, exception translation, and idempotent disposal.
/// </summary>
public sealed class BlobStorageFileStorageServiceTests : IDisposable
{
    private readonly string _stagingDirectory = Path.Combine(Path.GetTempPath(), $"BlobStorageTests_{Guid.NewGuid():N}");
    private readonly Mock<BlobContainerClient> _containerMock = new();
    private readonly Mock<BlobClient> _blobMock = new();
    private readonly LocalFileStorageService _defaultStorage;
    private readonly BlobStorageResiliencePipeline _pipeline;
    private readonly BlobStorageSettings _settings;
    private readonly FixedTimeProvider _timeProvider = new(new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero));
    private readonly BlobStorageFileStorageService _service;

    public BlobStorageFileStorageServiceTests()
    {
        Directory.CreateDirectory(_stagingDirectory);
        _defaultStorage = new LocalFileStorageService(NullLoggerFactory.Instance, _timeProvider, new LocalFileStorageSettings { BaseDirectory = _stagingDirectory });
        _pipeline = new BlobStorageResiliencePipeline(
            NullLogger<BlobStorageResiliencePipeline>.Instance,
            new ResiliencePipelineSettings
            {
                BackoffType = DelayBackoffType.Constant,
                MaxRetryAttempts = 1,
                DelayInMilliseconds = 1,
                UseJitter = false
            });
        _settings = new BlobStorageSettings
        {
            ConnectionString = "UseDevelopmentStorage=true",
            ContainerName = "unit-test-container",
            BasePrefix = "prefix"
        };
        _containerMock.Setup(c => c.GetBlobClient(It.IsAny<string>())).Returns(_blobMock.Object);
        _service = new BlobStorageFileStorageService(
            NullLogger<BlobStorageFileStorageService>.Instance,
            _defaultStorage,
            _timeProvider,
            _containerMock.Object,
            _settings,
            _pipeline);
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
    /// Verifies Provider property returns StorageProvider.BlobStorage.
    /// </summary>
    [Fact]
    public void Provider_ReturnsBlobStorage() => Assert.Equal(StorageProvider.BlobStorage, _service.Provider);

    /// <summary>
    /// Verifies OpenWriteAsync spools to local disk and uploads to Azure Blob Storage on DisposeAsync,
    /// exercising all progress thresholds and fallback timestamps.
    /// </summary>
    [Fact]
    public async Task OpenWriteAsync_OnDispose_UploadsPayloadToBlob()
    {
        // Arrange
        byte[]? uploadedBytes = null;
        _blobMock
            .Setup(b => b.UploadAsync(It.IsAny<Stream>(), It.IsAny<BlobUploadOptions>(), It.IsAny<CancellationToken>()))
            .Callback<Stream, BlobUploadOptions, CancellationToken>((stream, options, _) =>
            {
                using MemoryStream ms = new();
                stream.CopyTo(ms);
                uploadedBytes = ms.ToArray();
                options.ProgressHandler?.Report(1L);                       // < length and < 10 MiB
                options.ProgressHandler?.Report(10L * 1024L * 1024L);     // >= 10 MiB threshold
                options.ProgressHandler?.Report(uploadedBytes.Length);    // >= fileSizeBytes
            })
            .ReturnsAsync(Response.FromValue(BlobsModelFactory.BlobContentInfo(default, DateTimeOffset.UtcNow, [], "", 0), Mock.Of<Response>()));

        // Act
        await using (Stream writeStream = await _service.OpenWriteAsync("run-1/ctx/report.csv"))
        {
            await writeStream.WriteAsync("blob-payload"u8.ToArray());
        }

        // Empty payload test covers fileSizeBytes == 0 in ProgressHandler
        await using (Stream emptyStream = await _service.OpenWriteAsync("run-1/ctx/empty.csv"))
        {
            await emptyStream.FlushAsync();
        }

        // Assert
        Assert.NotNull(uploadedBytes);
    }

    /// <summary>
    /// Verifies OpenWriteAsync logs and rethrows RequestFailedException and generic exceptions during upload.
    /// </summary>
    [Fact]
    public async Task OpenWriteAsync_WhenUploadFails_RethrowsException()
    {
        // Arrange - RequestFailedException
        _blobMock
            .Setup(b => b.UploadAsync(It.IsAny<Stream>(), It.IsAny<BlobUploadOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException((int)HttpStatusCode.InternalServerError, "Upload error", "ServerError", null));

        // Act & Assert
        Stream writeStream = await _service.OpenWriteAsync("run-1/ctx/fail.csv");
        await writeStream.WriteAsync("x"u8.ToArray());
        await Assert.ThrowsAsync<RequestFailedException>(() => writeStream.DisposeAsync().AsTask());

        // Arrange - generic Exception
        _blobMock
            .Setup(b => b.UploadAsync(It.IsAny<Stream>(), It.IsAny<BlobUploadOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Generic blob fail"));

        Stream genericFailStream = await _service.OpenWriteAsync("run-1/ctx/fail-generic.csv");
        await genericFailStream.WriteAsync("x"u8.ToArray());
        await Assert.ThrowsAsync<InvalidOperationException>(() => genericFailStream.DisposeAsync().AsTask());
    }

    /// <summary>
    /// Verifies OpenReadAsync returns readable stream on success and translates 404/BlobNotFound into FileNotFoundException.
    /// </summary>
    [Fact]
    public async Task OpenReadAsync_SuccessAndNotFoundAndGenericError_BehavesAsExpected()
    {
        // Arrange
        MemoryStream downloadStream = new("blob-read-data"u8.ToArray());
        BlobDownloadStreamingResult downloadResult = BlobsModelFactory.BlobDownloadStreamingResult(downloadStream);
        Mock<BlobClient> okBlob = new();
        okBlob.Setup(b => b.DownloadStreamingAsync(It.IsAny<HttpRange>(), It.IsAny<BlobRequestConditions>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(downloadResult, Mock.Of<Response>()));

        Mock<BlobClient> missingBlob = new();
        missingBlob.Setup(b => b.DownloadStreamingAsync(It.IsAny<HttpRange>(), It.IsAny<BlobRequestConditions>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException((int)HttpStatusCode.NotFound, "Not found", "BlobNotFound", null));

        Mock<BlobClient> errorBlob = new();
        errorBlob.Setup(b => b.DownloadStreamingAsync(It.IsAny<HttpRange>(), It.IsAny<BlobRequestConditions>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected failure"));

        _containerMock.Setup(c => c.GetBlobClient("ok.txt")).Returns(okBlob.Object);
        _containerMock.Setup(c => c.GetBlobClient("missing.txt")).Returns(missingBlob.Object);
        _containerMock.Setup(c => c.GetBlobClient("err.txt")).Returns(errorBlob.Object);

        // Act & Assert
        await using (Stream stream = await _service.OpenReadAsync("ok.txt"))
        using (StreamReader reader = new(stream))
            Assert.Equal("blob-read-data", await reader.ReadToEndAsync());

        await Assert.ThrowsAsync<FileNotFoundException>(() => _service.OpenReadAsync("missing.txt").AsTask());
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.OpenReadAsync("err.txt").AsTask());
    }

    /// <summary>
    /// Verifies ExistsAsync returns true/false properly and handles 404/403 and unexpected exceptions.
    /// </summary>
    [Fact]
    public async Task ExistsAsync_HandlesTrueFalseNotFoundAndErrors()
    {
        // Arrange
        Mock<BlobClient> existsBlob = new();
        existsBlob.Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        Mock<BlobClient> notFoundBlob = new();
        notFoundBlob.Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException((int)HttpStatusCode.NotFound, "NotFound"));

        Mock<BlobClient> errBlob = new();
        errBlob.Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("timeout"));

        _containerMock.Setup(c => c.GetBlobClient("exists.txt")).Returns(existsBlob.Object);
        _containerMock.Setup(c => c.GetBlobClient("missing.txt")).Returns(notFoundBlob.Object);
        _containerMock.Setup(c => c.GetBlobClient("err.txt")).Returns(errBlob.Object);

        // Act & Assert
        Assert.True(await _service.ExistsAsync("exists.txt"));
        Assert.False(await _service.ExistsAsync("missing.txt"));
        await Assert.ThrowsAsync<TimeoutException>(() => _service.ExistsAsync("err.txt").AsTask());
    }

    /// <summary>
    /// Verifies GetMetadataAsync returns StorageEntryMetadata and handles NotFound and unexpected errors.
    /// </summary>
    [Fact]
    public async Task GetMetadataAsync_ReturnsMetadataOrThrowsAppropriateExceptions()
    {
        // Arrange
        DateTimeOffset modified = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);
        BlobProperties props = BlobsModelFactory.BlobProperties(lastModified: modified, contentLength: 256L);
        Mock<BlobClient> okBlob = new();
        okBlob.Setup(b => b.GetPropertiesAsync(It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(props, Mock.Of<Response>()));

        Mock<BlobClient> missingBlob = new();
        missingBlob.Setup(b => b.GetPropertiesAsync(It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException((int)HttpStatusCode.NotFound, "NotFound", "BlobNotFound", null));

        Mock<BlobClient> errBlob = new();
        errBlob.Setup(b => b.GetPropertiesAsync(It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException((int)HttpStatusCode.BadGateway, "BadGateway", "BadGateway", null));

        _containerMock.Setup(c => c.GetBlobClient("dir/data.csv")).Returns(okBlob.Object);
        _containerMock.Setup(c => c.GetBlobClient("missing.csv")).Returns(missingBlob.Object);
        _containerMock.Setup(c => c.GetBlobClient("err.csv")).Returns(errBlob.Object);

        // Act
        StorageEntryMetadata meta = await _service.GetMetadataAsync("dir/data.csv");

        // Assert
        Assert.Equal(StorageProvider.BlobStorage, meta.StorageProvider);
        Assert.Equal("dir", meta.Directory);
        Assert.Equal("data.csv", meta.FileName);
        Assert.Equal(256L, meta.SizeBytes);

        await Assert.ThrowsAsync<FileNotFoundException>(() => _service.GetMetadataAsync("missing.csv").AsTask());
        await Assert.ThrowsAsync<RequestFailedException>(() => _service.GetMetadataAsync("err.csv").AsTask());
    }

    /// <summary>
    /// Verifies DeleteAsync handles true, NotFound false, and unexpected errors.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_HandlesSuccessNotFoundAndErrors()
    {
        // Arrange
        Mock<BlobClient> okBlob = new();
        okBlob.Setup(b => b.DeleteIfExistsAsync(It.IsAny<DeleteSnapshotsOption>(), It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        Mock<BlobClient> missingBlob = new();
        missingBlob.Setup(b => b.DeleteIfExistsAsync(It.IsAny<DeleteSnapshotsOption>(), It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException((int)HttpStatusCode.NotFound, "NotFound"));

        Mock<BlobClient> errBlob = new();
        errBlob.Setup(b => b.DeleteIfExistsAsync(It.IsAny<DeleteSnapshotsOption>(), It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("delete failed"));

        _containerMock.Setup(c => c.GetBlobClient("ok.txt")).Returns(okBlob.Object);
        _containerMock.Setup(c => c.GetBlobClient("missing.txt")).Returns(missingBlob.Object);
        _containerMock.Setup(c => c.GetBlobClient("err.txt")).Returns(errBlob.Object);

        // Act & Assert
        Assert.True(await _service.DeleteAsync("ok.txt"));
        Assert.False(await _service.DeleteAsync("missing.txt"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteAsync("err.txt").AsTask());
    }

    /// <summary>
    /// Verifies ListFilesAsync paginates across pages and skips directory marker blobs.
    /// </summary>
    [Fact]
    public async Task ListFilesAsync_HandlesPaginationAndSkipsDirectoryMarkers()
    {
        // Arrange
        Page<BlobItem> page1 = Page<BlobItem>.FromValues(
            [
                BlobsModelFactory.BlobItem("prefix/id/ctx/", properties: BlobsModelFactory.BlobItemProperties(false, contentLength: 0)),
                BlobsModelFactory.BlobItem("prefix/id/ctx/a.csv", properties: BlobsModelFactory.BlobItemProperties(false, contentLength: 10))
            ],
            continuationToken: "tok-2",
            Mock.Of<Response>());
        Page<BlobItem> page2 = Page<BlobItem>.FromValues(
            [
                BlobsModelFactory.BlobItem("prefix/id/ctx/b.csv", properties: BlobsModelFactory.BlobItemProperties(false, contentLength: 20))
            ],
            continuationToken: null,
            Mock.Of<Response>());

        _containerMock
            .Setup(c => c.GetBlobsAsync(BlobTraits.None, BlobStates.None, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncPageable<BlobItem>.FromPages([page1, page2]));

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
    /// Verifies ListEntriesAsync yields virtual directories and blobs.
    /// </summary>
    [Fact]
    public async Task ListEntriesAsync_YieldsPrefixesAndBlobs()
    {
        // Arrange
        Page<BlobHierarchyItem> page = Page<BlobHierarchyItem>.FromValues(
            [
                BlobsModelFactory.BlobHierarchyItem("prefix/id/ctx/sub/", null),
                BlobsModelFactory.BlobHierarchyItem(null, BlobsModelFactory.BlobItem("prefix/id/ctx/", properties: BlobsModelFactory.BlobItemProperties(false, contentLength: 0))),
                BlobsModelFactory.BlobHierarchyItem(null, BlobsModelFactory.BlobItem("prefix/id/ctx/file.csv", properties: BlobsModelFactory.BlobItemProperties(false, contentLength: 55)))
            ],
            continuationToken: null,
            Mock.Of<Response>());

        _containerMock
            .Setup(c => c.GetBlobsByHierarchyAsync(BlobTraits.None, BlobStates.None, "/", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncPageable<BlobHierarchyItem>.FromPages([page]));

        // Act
        List<StorageEntryMetadata> results = [];
        await foreach (StorageEntryMetadata item in _service.ListEntriesAsync("id", "ctx"))
            results.Add(item);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Null(results[0].FileName);
        Assert.Equal("prefix/id/ctx/sub/", results[0].Location);
        Assert.Equal("file.csv", results[1].FileName);
        Assert.Equal(55L, results[1].SizeBytes);
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
        Assert.Equal(StorageProvider.BlobStorage, _service.Provider);
    }

    /// <summary>
    /// Verifies empty pageable results, null LastModified/ContentLength fallbacks, and listing error propagation.
    /// </summary>
    [Fact]
    public async Task ListMethods_EmptyPagesNullPropertiesAndErrors_BehaveAsExpected()
    {
        // Empty ListFilesAsync / ListEntriesAsync (0 pages -> break)
        _containerMock
            .Setup(c => c.GetBlobsAsync(BlobTraits.None, BlobStates.None, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncPageable<BlobItem>.FromPages([]));
        _containerMock
            .Setup(c => c.GetBlobsByHierarchyAsync(BlobTraits.None, BlobStates.None, "/", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncPageable<BlobHierarchyItem>.FromPages([]));

        await foreach (StorageEntryMetadata _ in _service.ListFilesAsync("empty", "ctx")) { }
        await foreach (StorageEntryMetadata _ in _service.ListEntriesAsync("empty", "ctx")) { }

        // Page with null LastModified and null ContentLength in both ListFilesAsync and ListEntriesAsync
        Page<BlobItem> nullMetaPage = Page<BlobItem>.FromValues(
            [BlobsModelFactory.BlobItem("prefix/id/ctx/nulls.csv", properties: BlobsModelFactory.BlobItemProperties(false, lastModified: null, contentLength: null))],
            continuationToken: null,
            Mock.Of<Response>());
        Page<BlobHierarchyItem> nullHierarchyPage = Page<BlobHierarchyItem>.FromValues(
            [BlobsModelFactory.BlobHierarchyItem(null, BlobsModelFactory.BlobItem("prefix/id/ctx/nulls2.csv", properties: BlobsModelFactory.BlobItemProperties(false, lastModified: null, contentLength: null)))],
            continuationToken: null,
            Mock.Of<Response>());

        _containerMock
            .Setup(c => c.GetBlobsAsync(BlobTraits.None, BlobStates.None, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncPageable<BlobItem>.FromPages([nullMetaPage]));
        _containerMock
            .Setup(c => c.GetBlobsByHierarchyAsync(BlobTraits.None, BlobStates.None, "/", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncPageable<BlobHierarchyItem>.FromPages([nullHierarchyPage]));

        List<StorageEntryMetadata> files = [];
        await foreach (StorageEntryMetadata item in _service.ListFilesAsync("id", "ctx"))
            files.Add(item);
        Assert.Single(files);
        Assert.Equal(_timeProvider.GetUtcNow(), files[0].CreatedAt);
        Assert.Equal(0L, files[0].SizeBytes);

        List<StorageEntryMetadata> entries = [];
        await foreach (StorageEntryMetadata item in _service.ListEntriesAsync("id", "ctx"))
            entries.Add(item);
        Assert.Single(entries);
        Assert.Equal(_timeProvider.GetUtcNow(), entries[0].CreatedAt);

        // Exceptions in ListFilesAsync and ListEntriesAsync
        _containerMock
            .Setup(c => c.GetBlobsAsync(BlobTraits.None, BlobStates.None, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Throws(new RequestFailedException(500, "Blob list fail"));
        _containerMock
            .Setup(c => c.GetBlobsByHierarchyAsync(BlobTraits.None, BlobStates.None, "/", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Throws(new InvalidOperationException("Hierarchy list fail"));

        await Assert.ThrowsAsync<RequestFailedException>(async () =>
        {
            await foreach (StorageEntryMetadata _ in _service.ListFilesAsync("id", "ctx")) { }
        });
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (StorageEntryMetadata _ in _service.ListEntriesAsync("id", "ctx")) { }
        });
    }

    /// <summary>
    /// Verifies end-to-end <c>CheckHealthAsync</c> across write (with local spooling), read verification, delete,
    /// content mismatch, failed delete, read error cleanup, and disposed guard.
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_ValidatesWriteReadDeleteAndDisposedState()
    {
        // 1. Healthy round-trip (write -> read matching bytes -> delete)
        byte[]? uploadedBytes = null;
        string? probeBlobName = null;
        bool deletedCalled = false;

        _containerMock
            .Setup(c => c.GetBlobClient(It.IsAny<string>()))
            .Callback<string>(name => probeBlobName = name)
            .Returns(_blobMock.Object);

        _blobMock
            .Setup(b => b.UploadAsync(It.IsAny<Stream>(), It.IsAny<BlobUploadOptions>(), It.IsAny<CancellationToken>()))
            .Callback<Stream, BlobUploadOptions, CancellationToken>((stream, _, _) =>
            {
                using MemoryStream ms = new();
                stream.CopyTo(ms);
                uploadedBytes = ms.ToArray();
            })
            .ReturnsAsync(Response.FromValue(BlobsModelFactory.BlobContentInfo(default, DateTimeOffset.UtcNow, [], "", 0), Mock.Of<Response>()));

        _blobMock
            .Setup(b => b.DownloadStreamingAsync(It.IsAny<HttpRange>(), It.IsAny<BlobRequestConditions>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => Response.FromValue(BlobsModelFactory.BlobDownloadStreamingResult(new MemoryStream(uploadedBytes!)), Mock.Of<Response>()));

        _blobMock
            .Setup(b => b.DeleteIfExistsAsync(It.IsAny<DeleteSnapshotsOption>(), It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
            .Callback(() => deletedCalled = true)
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        Assert.True(await _service.IsHealthAsync());
        Assert.NotNull(uploadedBytes);
        Assert.Equal(16, uploadedBytes.Length);
        Assert.NotNull(probeBlobName);
        Assert.Contains("__health/probe/.health-probe-", probeBlobName);
        Assert.True(deletedCalled);

        // 2. Corrupted read bytes -> cleans up probe blob and returns false
        deletedCalled = false;
        _blobMock
            .Setup(b => b.DownloadStreamingAsync(It.IsAny<HttpRange>(), It.IsAny<BlobRequestConditions>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => Response.FromValue(BlobsModelFactory.BlobDownloadStreamingResult(new MemoryStream([0x01, 0x02])), Mock.Of<Response>()));

        Assert.False(await _service.IsHealthAsync());
        Assert.True(deletedCalled);

        // 3. Delete returning false -> returns false
        _blobMock
            .Setup(b => b.DownloadStreamingAsync(It.IsAny<HttpRange>(), It.IsAny<BlobRequestConditions>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => Response.FromValue(BlobsModelFactory.BlobDownloadStreamingResult(new MemoryStream(uploadedBytes!)), Mock.Of<Response>()));
        _blobMock
            .Setup(b => b.DeleteIfExistsAsync(It.IsAny<DeleteSnapshotsOption>(), It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

        Assert.False(await _service.IsHealthAsync());

        // 4. Read failure after write -> attempts cleanup and preserves original exception
        deletedCalled = false;
        _blobMock
            .Setup(b => b.DownloadStreamingAsync(It.IsAny<HttpRange>(), It.IsAny<BlobRequestConditions>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException((int)HttpStatusCode.Forbidden, "Read denied"));
        _blobMock
            .Setup(b => b.DeleteIfExistsAsync(It.IsAny<DeleteSnapshotsOption>(), It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
            .Callback(() => deletedCalled = true)
            .ThrowsAsync(new InvalidOperationException("Cleanup failed"));

        await Assert.ThrowsAsync<RequestFailedException>(() => _service.IsHealthAsync().AsTask());
        Assert.True(deletedCalled);

        // 5. Disposed service -> throws ObjectDisposedException
        _service.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => _service.IsHealthAsync().AsTask());
    }
}
