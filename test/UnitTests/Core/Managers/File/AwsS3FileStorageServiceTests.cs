//using Amazon.S3;
//using Amazon.S3.Model;
//using MelloSilveiraTools.Core;
//using MelloSilveiraTools.Core.Managers.File.Models;
//using MelloSilveiraTools.Core.Managers.File.Providers;
//using MelloSilveiraTools.Core.Managers.File.Providers.AwsS3;
//using MelloSilveiraTools.Core.Managers.File.Providers.Local;
//using MelloSilveiraTools.Core.ResiliencePipelines;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Logging.Abstractions;
//using Moq;
//using System.Net;
//using System.Text;

//namespace UnitTests.Core.Managers.File;

///// <summary>
///// Unit tests for <see cref="AwsS3FileStorageService"/> verifying S3 operations,
///// disk spooling upload buffers, error translation, pagination, and dependency injection registration.
///// </summary>
//public sealed class AwsS3FileStorageServiceTests : IDisposable
//{
//    private readonly string _testStagingDirectory;
//    private readonly Mock<IAmazonS3> _s3ClientMock;
//    private readonly LocalFileStorageService _defaultStorage;
//    private readonly AwsS3ResiliencePipeline _resiliencePipeline;
//    private readonly AwsS3StorageSettings _settings;
//    private readonly AwsS3FileStorageService _service;

//    public AwsS3FileStorageServiceTests()
//    {
//        _testStagingDirectory = Path.Combine(Path.GetTempPath(), $"AwsS3FileStorageServiceTests_{Guid.NewGuid():N}");
//        Directory.CreateDirectory(_testStagingDirectory);

//        _s3ClientMock = new Mock<IAmazonS3>();
//        _defaultStorage = new LocalFileStorageService(new LocalFileStorageSettings { BaseDirectory = _testStagingDirectory }, TimeProvider.System);
//        _resiliencePipeline = new AwsS3ResiliencePipeline(
//            NullLogger<AwsS3ResiliencePipeline>.Instance,
//            new ResiliencePipelineSettings
//            {
//                BackoffType = Polly.DelayBackoffType.Constant,
//                DelayInMilliseconds = 100,
//                MaxRetryAttempts = 3,
//                UseJitter = false
//            });

//        _settings = new AwsS3StorageSettings
//        {
//            BucketName = "test-bucket",
//            Region = "sa-east-1",
//            BasePrefix = "root-folder"
//        };

//        _service = new AwsS3FileStorageService(
//            NullLogger<AwsS3FileStorageService>.Instance,
//            _defaultStorage,
//            _s3ClientMock.Object,
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
//    /// Verifies that Provider property returns <see cref="StorageProvider.AwsS3"/>.
//    /// </summary>
//    [Fact]
//    public void Provider_ShouldReturnAwsS3()
//    {
//        Assert.Equal(StorageProvider.AwsS3, _service.Provider);
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
//    /// Verifies that ExistsAsync returns true when the object metadata indicates OK status.
//    /// </summary>
//    [Fact]
//    public async Task ExistsAsync_WhenObjectExists_ReturnsTrueAsync()
//    {
//        // Arrange
//        _s3ClientMock
//            .Setup(c => c.GetObjectMetadataAsync(It.IsAny<GetObjectMetadataRequest>(), It.IsAny<CancellationToken>()))
//            .ReturnsAsync(new GetObjectMetadataResponse { HttpStatusCode = HttpStatusCode.OK });

//        // Act
//        bool exists = await _service.ExistsAsync("analysis/run_1/file.csv");

//        // Assert
//        Assert.True(exists);
//    }

//    /// <summary>
//    /// Verifies that ExistsAsync returns false when S3 throws AmazonS3Exception with NotFound status.
//    /// </summary>
//    [Fact]
//    public async Task ExistsAsync_WhenObjectDoesNotExist_ReturnsFalseAsync()
//    {
//        // Arrange
//        _s3ClientMock
//            .Setup(c => c.GetObjectMetadataAsync(It.IsAny<GetObjectMetadataRequest>(), It.IsAny<CancellationToken>()))
//            .ThrowsAsync(new AmazonS3Exception("Not Found") { StatusCode = HttpStatusCode.NotFound, ErrorCode = "NotFound" });

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
//        DateTime now = DateTime.UtcNow;
//        _s3ClientMock
//            .Setup(c => c.GetObjectMetadataAsync(It.IsAny<GetObjectMetadataRequest>(), It.IsAny<CancellationToken>()))
//            .ReturnsAsync(new GetObjectMetadataResponse
//            {
//                HttpStatusCode = HttpStatusCode.OK,
//                ContentLength = 2048L,
//                LastModified = now
//            });

//        // Act
//        StorageEntryMetadata metadata = await _service.GetMetadataAsync("analysis/run_1/data.csv");

//        // Assert
//        Assert.Equal(StorageProvider.AwsS3, metadata.StorageProvider);
//        Assert.Equal("analysis/run_1/data.csv", metadata.Location);
//        Assert.Equal("data.csv", metadata.FileName);
//        Assert.Equal(2048L, metadata.SizeBytes);
//        Assert.True(metadata.IsFile);
//        Assert.False(metadata.IsDirectory);
//    }

//    /// <summary>
//    /// Verifies that GetMetadataAsync throws <see cref="FileNotFoundException"/> when object is missing.
//    /// </summary>
//    [Fact]
//    public async Task GetMetadataAsync_WhenObjectDoesNotExist_ThrowsFileNotFoundExceptionAsync()
//    {
//        // Arrange
//        _s3ClientMock
//            .Setup(c => c.GetObjectMetadataAsync(It.IsAny<GetObjectMetadataRequest>(), It.IsAny<CancellationToken>()))
//            .ThrowsAsync(new AmazonS3Exception("Not Found") { StatusCode = HttpStatusCode.NotFound, ErrorCode = "NoSuchKey" });

//        // Act & Assert
//        await Assert.ThrowsAsync<FileNotFoundException>(async () =>
//        {
//            await _service.GetMetadataAsync("analysis/run_1/missing.csv");
//        });
//    }

//    /// <summary>
//    /// Verifies that DeleteAsync returns true on successful object deletion.
//    /// </summary>
//    [Fact]
//    public async Task DeleteAsync_WhenSuccessful_ReturnsTrueAsync()
//    {
//        // Arrange
//        _s3ClientMock
//            .Setup(c => c.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), It.IsAny<CancellationToken>()))
//            .ReturnsAsync(new DeleteObjectResponse { HttpStatusCode = HttpStatusCode.NoContent });

//        // Act
//        bool result = await _service.DeleteAsync("analysis/run_1/data.csv");

//        // Assert
//        Assert.True(result);
//    }

//    /// <summary>
//    /// Verifies that DeleteAsync returns false when the target object is not found.
//    /// </summary>
//    [Fact]
//    public async Task DeleteAsync_WhenNotFound_ReturnsFalseAsync()
//    {
//        // Arrange
//        _s3ClientMock
//            .Setup(c => c.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), It.IsAny<CancellationToken>()))
//            .ThrowsAsync(new AmazonS3Exception("Not Found") { StatusCode = HttpStatusCode.NotFound, ErrorCode = "NotFound" });

//        // Act
//        bool result = await _service.DeleteAsync("analysis/run_1/data.csv");

//        // Assert
//        Assert.False(result);
//    }

//    /// <summary>
//    /// Verifies that OpenReadAsync returns a readable stream with S3 contents.
//    /// </summary>
//    [Fact]
//    public async Task OpenReadAsync_WhenObjectExists_ReturnsReadableStreamAsync()
//    {
//        // Arrange
//        byte[] payload = "test data content"u8.ToArray();
//        GetObjectResponse getResponse = new()
//        {
//            HttpStatusCode = HttpStatusCode.OK,
//            ResponseStream = new MemoryStream(payload)
//        };

//        _s3ClientMock
//            .Setup(c => c.GetObjectAsync(It.IsAny<GetObjectRequest>(), It.IsAny<CancellationToken>()))
//            .ReturnsAsync(getResponse);

//        // Act
//        await using Stream stream = await _service.OpenReadAsync("analysis/run_1/file.txt");
//        byte[] buffer = new byte[payload.Length];
//        int read = await stream.ReadAsync(buffer);

//        // Assert
//        Assert.Equal(payload.Length, read);
//        Assert.Equal(payload, buffer);
//    }

//    /// <summary>
//    /// Verifies disk-buffered upload workflow: writing to stream spools locally and uploads to S3 upon disposal.
//    /// </summary>
//    [Fact]
//    public async Task OpenWriteAsync_SpoolsToDiskAndUploadsOnDisposeAsync()
//    {
//        // Arrange
//        byte[] uploadData = Encoding.UTF8.GetBytes("step1,step2\n1.0,2.0\n");
//        PutObjectRequest? capturedRequest = null;

//        _s3ClientMock
//            .Setup(c => c.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
//            .Callback<PutObjectRequest, CancellationToken>((req, _) => capturedRequest = req)
//            .ReturnsAsync(new PutObjectResponse { HttpStatusCode = HttpStatusCode.OK });

//        // Act
//        Stream writeStream = await _service.OpenWriteAsync("analysis/run_1/output.csv");
//        await using (writeStream)
//        {
//            await writeStream.WriteAsync(uploadData);
//        }

//        // Assert
//        Assert.NotNull(capturedRequest);
//        Assert.Equal("test-bucket", capturedRequest.BucketName);
//        Assert.Equal("analysis/run_1/output.csv", capturedRequest.Key);
//        Assert.NotNull(capturedRequest.InputStream);
//    }

//    /// <summary>
//    /// Verifies that ListFilesAsync streams S3 objects and ignores directory markers.
//    /// </summary>
//    [Fact]
//    public async Task ListFilesAsync_StreamsFilesAndFiltersDirectoryMarkersAsync()
//    {
//        // Arrange
//        ListObjectsV2Response response = new()
//        {
//            HttpStatusCode = HttpStatusCode.OK,
//            S3Objects =
//            [
//                new S3Object { Key = "root-folder/run_1/exp/", Size = 0 },
//                new S3Object { Key = "root-folder/run_1/exp/file1.csv", Size = 100 },
//                new S3Object { Key = "root-folder/run_1/exp/file2.csv", Size = 200 }
//            ],
//            IsTruncated = false
//        };

//        _s3ClientMock
//            .Setup(c => c.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
//            .ReturnsAsync(response);

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
//    /// Verifies that ListEntriesAsync returns both directory prefixes and objects.
//    /// </summary>
//    [Fact]
//    public async Task ListEntriesAsync_StreamsBothDirectoriesAndFilesAsync()
//    {
//        // Arrange
//        ListObjectsV2Response response = new()
//        {
//            HttpStatusCode = HttpStatusCode.OK,
//            CommonPrefixes = ["root-folder/run_1/exp/subfolder/"],
//            S3Objects =
//            [
//                new S3Object { Key = "root-folder/run_1/exp/data.csv", Size = 500 }
//            ],
//            IsTruncated = false
//        };

//        _s3ClientMock
//            .Setup(c => c.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
//            .ReturnsAsync(response);

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
//    /// Verifies that DI container correctly registers AWS S3 storage under <see cref="StorageProvider.AwsS3"/>.
//    /// </summary>
//    [Fact]
//    public void KeyedServiceRegistration_ShouldResolveAwsS3FileStorageService()
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
//                AwsS3 = new AwsS3StorageSettings
//                {
//                    BucketName = "test-bucket",
//                    Region = "sa-east-1"
//                }
//            });
//        ServiceProvider provider = services.BuildServiceProvider();

//        // Act
//        IFileStorageService? keyedService = provider.GetKeyedService<IFileStorageService>(StorageProvider.AwsS3);

//        // Assert
//        Assert.NotNull(keyedService);
//        Assert.IsType<AwsS3FileStorageService>(keyedService);
//        Assert.Equal(StorageProvider.AwsS3, keyedService.Provider);
//    }
//}
