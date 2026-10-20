using MelloSilveiraTools.Core;
using MelloSilveiraTools.Core.Managers.File.Models;
using MelloSilveiraTools.Core.Managers.File.Providers;
using MelloSilveiraTools.Core.Managers.File.Providers.Dfs;
using MelloSilveiraTools.Core.ResiliencePipelines;
using Microsoft.Extensions.DependencyInjection;

namespace UnitTests.Core.Managers.File;

/// <summary>
/// Unit tests for <see cref="DfsFileStorageService"/> verifying distributed file system configuration,
/// cross-platform path validation, metadata provider tagging, and asynchronous persistence operations.
/// </summary>
public sealed class DfsFileStorageServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly DfsFileStorageService _service;

    public DfsFileStorageServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"DfsFileStorageServiceTests_{Guid.NewGuid():N}");
        DfsStorageSettings settings = new() { BaseDirectory = _testDirectory };
        _service = new DfsFileStorageService(settings);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch
            {
                // Best effort cleanup in test temp folder
            }
        }
    }

    /// <summary>
    /// Verifies that DI container correctly registers DFS file storage under <see cref="StorageProvider.Dfs"/>.
    /// </summary>
    [Fact]
    public void KeyedServiceRegistration_ShouldResolveDfsFileStorageService()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddCoreServices(
            new ResiliencePipelineSettings
            {
                BackoffType = Polly.DelayBackoffType.Constant,
                DelayInMilliseconds = 100,
                MaxRetryAttempts = 3,
                UseJitter = false
            },
            fileStorageSettings: new FileStorageSettings
            {
                Dfs = new DfsStorageSettings { BaseDirectory = _testDirectory }
            });
        ServiceProvider provider = services.BuildServiceProvider();

        // Act
        IFileStorageService? keyedService = provider.GetKeyedService<IFileStorageService>(StorageProvider.Dfs);

        // Assert
        Assert.NotNull(keyedService);
        Assert.IsType<DfsFileStorageService>(keyedService);
        Assert.Equal(StorageProvider.Dfs, keyedService.Provider);
    }

    /// <summary>
    /// Verifies that Provider returns <see cref="StorageProvider.Dfs"/>.
    /// </summary>
    [Fact]
    public void Provider_ShouldReturnDfs()
    {
        Assert.Equal(StorageProvider.Dfs, _service.Provider);
    }

    /// <summary>
    /// Verifies that constructing DFS settings with null or whitespace base directory throws <see cref="ArgumentException"/>.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyBaseDirectory_ThrowsArgumentException(string invalidPath)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
        {
            new DfsFileStorageService(new DfsStorageSettings { BaseDirectory = invalidPath });
        });
    }

    /// <summary>
    /// Verifies end-to-end asynchronous file lifecycle in DFS storage, ensuring metadata reflects the DFS provider.
    /// </summary>
    [Fact]
    public async Task CreateWrite_OpenRead_GetMetadata_List_Delete_ShouldOperateCorrectlyAsync()
    {
        // Arrange
        string context = "DistributedSimulations";
        string identifier = "Node_01";
        string fileName = "remote_output.csv";
        byte[] content = "colA,colB\n10.0,20.0\n"u8.ToArray();

        // Act - CreateWriteAsync
        (Stream writeStream, string writtenLocation) = await _service.CreateWriteAsync(context, identifier, fileName);
        Assert.True(Path.IsPathRooted(writtenLocation));
        Assert.EndsWith(fileName, writtenLocation);

        await using (writeStream)
        {
            await writeStream.WriteAsync(content);
        }

        // Assert - Metadata reflects StorageProvider.Dfs
        StorageEntryMetadata metadata = await _service.GetMetadataAsync(context, identifier, fileName);
        Assert.Equal(fileName, metadata.FileName);
        Assert.Equal(content.Length, metadata.SizeBytes);
        Assert.Equal(StorageProvider.Dfs, metadata.StorageProvider);
        Assert.True(System.IO.File.Exists(metadata.FullPath));

        // Assert - OpenReadAsync
        await using (Stream readStream = await _service.OpenReadAsync(context, identifier, fileName))
        {
            byte[] readBuffer = new byte[content.Length];
            int read = await readStream.ReadAsync(readBuffer);
            Assert.Equal(content.Length, read);
            Assert.Equal(content, readBuffer);
        }

        // Assert - ListFilesAsync
        List<StorageEntryMetadata> files = [];
        await foreach (StorageEntryMetadata item in _service.ListFilesAsync(context, identifier))
        {
            files.Add(item);
        }
        Assert.Single(files);
        Assert.Equal(fileName, files[0].FileName);
        Assert.Equal(StorageProvider.Dfs, files[0].StorageProvider);

        // Act - DeleteAsync
        bool deleted = await _service.DeleteAsync(context, identifier, fileName);
        Assert.True(deleted);

        // Verify gone
        bool existsAfterDelete = await _service.ExistsAsync(writtenLocation);
        Assert.False(existsAfterDelete);
    }

    /// <summary>
    /// Verifies that CreateWriteTimebasedAsync creates a timestamped file under DFS storage.
    /// </summary>
    [Fact]
    public async Task CreateWriteTimebasedAsync_ShouldCreateTimestampedFileAndReturnLocationAsync()
    {
        // Arrange
        string context = "DistributedSimulations";
        string identifier = "Node_02";
        string filePrefix = "BatchRun";
        string fileExtension = ".csv";
        byte[] content = "time,load\n0.0,5.0\n"u8.ToArray();

        // Act
        (Stream writeStream, string location) = await _service.CreateWriteTimebasedAsync(context, identifier, filePrefix, fileExtension);
        Assert.True(Path.IsPathRooted(location));
        string createdFileName = Path.GetFileName(location);
        Assert.StartsWith(filePrefix, createdFileName);
        Assert.EndsWith(fileExtension, createdFileName);

        await using (writeStream)
        {
            await writeStream.WriteAsync(content);
        }

        // Assert
        StorageEntryMetadata metadata = await _service.GetMetadataAsync(location);
        Assert.Equal(content.Length, metadata.SizeBytes);
        Assert.Equal(StorageProvider.Dfs, metadata.StorageProvider);

        List<StorageEntryMetadata> files = [];
        await foreach (StorageEntryMetadata item in _service.ListFilesAsync(context, identifier))
        {
            files.Add(item);
        }
        Assert.Single(files);
        Assert.StartsWith(filePrefix, files[0].FileName);
        Assert.EndsWith(fileExtension, files[0].FileName);
        Assert.Equal(content.Length, files[0].SizeBytes);
        Assert.Equal(StorageProvider.Dfs, files[0].StorageProvider);
    }
}
