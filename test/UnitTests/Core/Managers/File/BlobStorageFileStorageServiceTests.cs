//using Azure;
//using Azure.Storage.Blobs;
//using Azure.Storage.Blobs.Models;
//using MelloSilveiraTools.Core;
//using MelloSilveiraTools.Core.Managers.File.Models;
//using MelloSilveiraTools.Core.Managers.File.Providers;
//using MelloSilveiraTools.Core.Managers.File.Providers.BlobStorage;
//using MelloSilveiraTools.Core.Managers.File.Providers.Local;
//using MelloSilveiraTools.Core.ResiliencePipelines;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Logging.Abstractions;
//using Moq;
//using System.Text;

//namespace UnitTests.Core.Managers.File;

///// <summary>
///// Unit tests for <see cref="BlobStorageFileStorageService"/> verifying Azure Blob Storage operations,
///// disk spooling upload buffers, error translation, hierarchy listing, and dependency injection registration.
///// </summary>
//public sealed class BlobStorageFileStorageServiceTests : IDisposable
//{
//    private readonly string _testStagingDirectory;
//    private readonly Mock<BlobContainerClient> _containerClientMock;
//    private readonly Mock<BlobClient> _blobClientMock;
//    private readonly LocalFileStorageService _defaultStorage;
//    private readonly BlobStorageResiliencePipeline _resiliencePipeline;
//    private readonly BlobStorageSettings _settings;
//    private readonly BlobStorageFileStorageService _service;

//    public BlobStorageFileStorageServiceTests()
//    {
//        _testStagingDirectory = Path.Combine(Path.GetTempPath(), $"BlobStorageFileStorageServiceTests_{Guid.NewGuid():N}");
//        Directory.CreateDirectory(_testStagingDirectory);

//        _blobClientMock = new Mock<BlobClient>();
//        _containerClientMock = new Mock<BlobContainerClient>();
//        _containerClientMock
//            .Setup(c => c.GetBlobClient(It.IsAny<string>()))
//            .Returns(_blobClientMock.Object);

//        _defaultStorage = new LocalFileStorageService(new LocalFileStorageSettings { BaseDirectory = _testStagingDirectory }, TimeProvider.System);
//        _resiliencePipeline = new BlobStorageResiliencePipeline(
//            NullLogger<BlobStorageResiliencePipeline>.Instance,
//            new ResiliencePipelineSettings
//            {
//                BackoffType = Polly.DelayBackoffType.Constant,
//                DelayInMilliseconds = 100,
//                MaxRetryAttempts = 3,
//                UseJitter = false
//            });

//        _settings = new BlobStorageSettings
//        {
//            ConnectionString = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=dGVzdA==;EndpointSuffix=core.windows.net",
//            ContainerName = "test-container",
//            BasePrefix = "root-folder"
//        };

//        _service = new BlobStorageFileStorageService(
//            NullLogger<BlobStorageFileStorageService>.Instance,
//            _defaultStorage,
//            _containerClientMock.Object,
//            _settings,
//            _resiliencePipeline,
//            TimeProvider.System);
//    }

//    public void Dispose()
//    {
//        if (Directory.Exists(_testStagingDirectory))
//        {
//            try
//            {
//                Directory.Delete(_testStagingDirectory, true);
//            }
//            catch
//            {
//                // Best effort cleanup in test temp folder
//            }
//        }
//    }

//    /// <summary>
//    /// Verifies that Provider property returns <see cref="StorageProvider.BlobStorage"/>.
//    /// </summary>
//    [Fact]
//    public void Provider_ShouldReturnBlobStorage()
//    {
//        Assert.Equal(StorageProvider.BlobStorage, _service.Provider);
//    }

//    /// <summary>
//    /// Verifies that ResolveLocation incorporates BasePrefix, context, identifier, and file name.
//    /// </summary>
//    [Fact]
//    public void ResolveLocation_ShouldIncorporateBasePrefixAndContext()
//    {
//        // Act
//        string location = _service.ResolveLocation("run_1", "analysis", "model.csv");

//        // Assert
//        Assert.Equal("root-folder/run_1/analysis/model.csv", location);
//    }

//    /// <summary>
//    /// Verifies that ExistsAsync returns true when the blob exists.
//    /// </summary>
//    [Fact]
//    public async Task ExistsAsync_WhenObjectExists_ReturnsTrueAsync()
//    {
//        // Arrange
//        _blobClientMock
//            .Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
//            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

//        // Act
//        bool exists = await _service.ExistsAsync("analysis/run_1/file.csv");

//        // Assert
//        Assert.True(exists);
//    }

//    /// <summary>
//    /// Verifies that ExistsAsync returns false when the blob does not exist.
//    /// </summary>
//    [Fact]
//    public async Task ExistsAsync_WhenObjectDoesNotExist_ReturnsFalseAsync()
//    {
//        // Arrange
//        _blobClientMock
//            .Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
//            .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

//        // Act
//        bool exists = await _service.ExistsAsync("analysis/run_1/missing.csv");

//        // Assert
//        Assert.False(exists);
//    }

//    /// <summary>
//    /// Verifies that GetMetadataAsync returns <see cref="StorageEntryMetadata"/> with valid properties.
//    /// </summary>
//    [Fact]
//    public async Task GetMetadataAsync_WhenObjectExists_ReturnsMetadataAsync()
//    {
//        // Arrange
//        DateTimeOffset now = DateTimeOffset.UtcNow;
//        BlobProperties properties = BlobsModelFactory.BlobProperties(contentLength: 2048L, lastModified: now);

//        _blobClientMock
//            .Setup(b => b.GetPropertiesAsync(It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
//            .ReturnsAsync(Response.FromValue(properties, Mock.Of<Response>()));

//        // Act
//        StorageEntryMetadata metadata = await _service.GetMetadataAsync("analysis/run_1/data.csv");

//        // Assert
//        Assert.Equal(StorageProvider.BlobStorage, metadata.StorageProvider);
//        Assert.Equal("analysis/run_1/data.csv", metadata.Location);
//        Assert.Equal("data.csv", metadata.FileName);
//        Assert.Equal(2048L, metadata.SizeBytes);
//        Assert.True(metadata.IsFile);
//        Assert.False(metadata.IsDirectory);
//    }

//    /// <summary>
//    /// Verifies that GetMetadataAsync throws <see cref="FileNotFoundException"/> when blob is missing.
//    /// </summary>
//    [Fact]
//    public async Task GetMetadataAsync_WhenObjectDoesNotExist_ThrowsFileNotFoundExceptionAsync()
//    {
//        // Arrange
//        _blobClientMock
//            .Setup(b => b.GetPropertiesAsync(It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
//            .ThrowsAsync(new RequestFailedException(404, "BlobNotFound"));

//        // Act & Assert
//        await Assert.ThrowsAsync<FileNotFoundException>(async () =>
//        {
//            await _service.GetMetadataAsync("analysis/run_1/missing.csv");
//        });
//    }

//    /// <summary>
//    /// Verifies that DeleteAsync returns true on successful blob deletion.
//    /// </summary>
//    [Fact]
//    public async Task DeleteAsync_WhenSuccessful_ReturnsTrueAsync()
//    {
//        // Arrange
//        _blobClientMock
//            .Setup(b => b.DeleteIfExistsAsync(It.IsAny<DeleteSnapshotsOption>(), It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
//            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

//        // Act
//        bool result = await _service.DeleteAsync("analysis/run_1/data.csv");

//        // Assert
//        Assert.True(result);
//    }

//    /// <summary>
//    /// Verifies that DeleteAsync returns false when the target blob is not found.
//    /// </summary>
//    [Fact]
//    public async Task DeleteAsync_WhenNotFound_ReturnsFalseAsync()
//    {
//        // Arrange
//        _blobClientMock
//            .Setup(b => b.DeleteIfExistsAsync(It.IsAny<DeleteSnapshotsOption>(), It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
//            .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

//        // Act
//        bool result = await _service.DeleteAsync("analysis/run_1/data.csv");

//        // Assert
//        Assert.False(result);
//    }

//    /// <summary>
//    /// Verifies that OpenReadAsync returns a readable stream with blob contents.
//    /// </summary>
//    [Fact]
//    public async Task OpenReadAsync_WhenObjectExists_ReturnsReadableStreamAsync()
//    {
//        // Arrange
//        byte[] payload = "test blob content"u8.ToArray();
//        BlobDownloadStreamingResult streamingResult = BlobsModelFactory.BlobDownloadStreamingResult(content: new MemoryStream(payload));

//        _blobClientMock
//            .Setup(b => b.DownloadStreamingAsync(It.IsAny<HttpRange>(), It.IsAny<BlobRequestConditions>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
//            .ReturnsAsync(Response.FromValue(streamingResult, Mock.Of<Response>()));

//        // Act
//        await using Stream stream = await _service.OpenReadAsync("analysis/run_1/file.txt");
//        byte[] buffer = new byte[payload.Length];
//        int read = await stream.ReadAsync(buffer);

//        // Assert
//        Assert.Equal(payload.Length, read);
//        Assert.Equal(payload, buffer);
//    }

//    /// <summary>
//    /// Verifies disk-buffered upload workflow: writing to stream spools locally and uploads to Blob Storage upon disposal.
//    /// </summary>
//    [Fact]
//    public async Task OpenWriteAsync_SpoolsToDiskAndUploadsOnDisposeAsync()
//    {
//        // Arrange
//        byte[] uploadData = Encoding.UTF8.GetBytes("step1,step2\n1.0,2.0\n");
//        bool uploadCalled = false;

//        _blobClientMock
//            .Setup(b => b.UploadAsync(It.IsAny<Stream>(), It.IsAny<BlobUploadOptions>(), It.IsAny<CancellationToken>()))
//            .Callback<Stream, BlobUploadOptions, CancellationToken>((_, _, _) => uploadCalled = true)
//            .ReturnsAsync(Response.FromValue(BlobsModelFactory.BlobContentInfo(ETag.All, DateTimeOffset.UtcNow, [], "", "", 1), Mock.Of<Response>()));

//        // Act
//        Stream writeStream = await _service.OpenWriteAsync("analysis/run_1/output.csv");
//        await using (writeStream)
//        {
//            await writeStream.WriteAsync(uploadData);
//        }

//        // Assert
//        Assert.True(uploadCalled);
//    }

//    /// <summary>
//    /// Verifies that ListFilesAsync streams blobs and ignores virtual directory markers.
//    /// </summary>
//    [Fact]
//    public async Task ListFilesAsync_StreamsFilesAndFiltersDirectoryMarkersAsync()
//    {
//        // Arrange
//        DateTimeOffset now = DateTimeOffset.UtcNow;
//        BlobItem dirMarker = BlobsModelFactory.BlobItem(name: "root-folder/run_1/exp/", properties: BlobsModelFactory.BlobItemProperties(false, contentLength: 0L, lastModified: now));
//        BlobItem file1 = BlobsModelFactory.BlobItem(name: "root-folder/run_1/exp/file1.csv", properties: BlobsModelFactory.BlobItemProperties(false, contentLength: 100L, lastModified: now));
//        BlobItem file2 = BlobsModelFactory.BlobItem(name: "root-folder/run_1/exp/file2.csv", properties: BlobsModelFactory.BlobItemProperties(false, contentLength: 200L, lastModified: now));

//        Page<BlobItem> page = Page<BlobItem>.FromValues([dirMarker, file1, file2], null, Mock.Of<Response>());
//        AsyncPageable<BlobItem> pageable = AsyncPageable<BlobItem>.FromPages([page]);

//        _containerClientMock
//            .Setup(c => c.GetBlobsAsync(It.IsAny<BlobTraits>(), It.IsAny<BlobStates>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
//            .Returns(pageable);

//        // Act
//        List<StorageEntryMetadata> files = [];
//        await foreach (StorageEntryMetadata item in _service.ListFilesAsync("run_1", "exp"))
//        {
//            files.Add(item);
//        }

//        // Assert
//        Assert.Equal(2, files.Count);
//        Assert.All(files, f => Assert.True(f.IsFile));
//        Assert.Contains(files, f => f.FileName == "file1.csv" && f.SizeBytes == 100L);
//        Assert.Contains(files, f => f.FileName == "file2.csv" && f.SizeBytes == 200L);
//    }

//    /// <summary>
//    /// Verifies that ListEntriesAsync returns both directory prefixes and blobs.
//    /// </summary>
//    [Fact]
//    public async Task ListEntriesAsync_StreamsBothDirectoriesAndFilesAsync()
//    {
//        // Arrange
//        DateTimeOffset now = DateTimeOffset.UtcNow;
//        BlobHierarchyItem prefixItem = BlobsModelFactory.BlobHierarchyItem("root-folder/run_1/exp/subfolder/", null);
//        BlobItem blobItem = BlobsModelFactory.BlobItem(name: "root-folder/run_1/exp/data.csv", properties: BlobsModelFactory.BlobItemProperties(false, contentLength: 500L, lastModified: now));
//        BlobHierarchyItem fileItem = BlobsModelFactory.BlobHierarchyItem(null, blobItem);

//        Page<BlobHierarchyItem> page = Page<BlobHierarchyItem>.FromValues([prefixItem, fileItem], null, Mock.Of<Response>());
//        AsyncPageable<BlobHierarchyItem> pageable = AsyncPageable<BlobHierarchyItem>.FromPages([page]);

//        _containerClientMock
//            .Setup(c => c.GetBlobsByHierarchyAsync(It.IsAny<BlobTraits>(), It.IsAny<BlobStates>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
//            .Returns(pageable);

//        // Act
//        List<StorageEntryMetadata> entries = [];
//        await foreach (StorageEntryMetadata entry in _service.ListEntriesAsync("run_1", "exp"))
//        {
//            entries.Add(entry);
//        }

//        // Assert
//        Assert.Equal(2, entries.Count);
//        Assert.Contains(entries, e => e.IsDirectory && e.FileName == null);
//        Assert.Contains(entries, e => e.IsFile && e.FileName == "data.csv" && e.SizeBytes == 500L);
//    }

//    /// <summary>
//    /// Verifies that DI container correctly registers Azure Blob Storage under <see cref="StorageProvider.BlobStorage"/>.
//    /// </summary>
//    [Fact]
//    public void KeyedServiceRegistration_ShouldResolveBlobStorageFileStorageService()
//    {
//        // Arrange
//        ServiceCollection services = new();
//        services.AddCoreServices(
//            new ResiliencePipelineSettings
//            {
//                BackoffType = Polly.DelayBackoffType.Constant,
//                DelayInMilliseconds = 100,
//                MaxRetryAttempts = 3,
//                UseJitter = false
//            },
//            fileStorageSettings: new FileStorageSettings
//            {
//                BlobStorage = new BlobStorageSettings
//                {
//                    ConnectionString = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=dGVzdA==;EndpointSuffix=core.windows.net",
//                    ContainerName = "test-container"
//                }
//            });
//        ServiceProvider provider = services.BuildServiceProvider();

//        // Act
//        IFileStorageService? keyedService = provider.GetKeyedService<IFileStorageService>(StorageProvider.BlobStorage);

//        // Assert
//        Assert.NotNull(keyedService);
//        Assert.IsType<BlobStorageFileStorageService>(keyedService);
//        Assert.Equal(StorageProvider.BlobStorage, keyedService.Provider);
//    }
//}
