using MelloSilveiraTools.Core;
using MelloSilveiraTools.Core.Managers.File.Models;
using MelloSilveiraTools.Core.Managers.File.Providers;
using MelloSilveiraTools.Core.Managers.File.Providers.Local;
using MelloSilveiraTools.Core.ResiliencePipelines;
using Microsoft.Extensions.DependencyInjection;

namespace UnitTests.Core.Managers.File;

/// <summary>
/// Unit tests for <see cref="LocalFileStorageService"/> verifying end-to-end file persistence,
/// streaming reads and writes, metadata resolution for files and directories, directory traversal safeguards,
/// recursive directory sizing, and dependency injection container resolution.
/// </summary>
public sealed class LocalFileStorageServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly LocalFileStorageService _service;

    public LocalFileStorageServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"LocalFileStorageServiceTests_{Guid.NewGuid():N}");
        LocalFileStorageSettings settings = new() { BaseDirectory = _testDirectory };
        _service = new LocalFileStorageService(settings);
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
    /// Verifies that <see cref="CoreDependencyInjection.AddCoreServices"/> registers the local file storage service
    /// under the <see cref="StorageProvider.Local"/> key as well as the default service.
    /// </summary>
    [Fact]
    public void KeyedServiceRegistration_ShouldResolveLocalFileStorageService()
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
                Local = new LocalFileStorageSettings { BaseDirectory = _testDirectory }
            });
        ServiceProvider provider = services.BuildServiceProvider();

        // Act
        IFileStorageService? keyedService = provider.GetKeyedService<IFileStorageService>(StorageProvider.Local);
        IFileStorageService? defaultService = provider.GetService<IFileStorageService>();

        // Assert
        Assert.NotNull(keyedService);
        Assert.NotNull(defaultService);
        Assert.IsType<LocalFileStorageService>(keyedService);
        Assert.Equal(StorageProvider.Local, keyedService.Provider);
    }

    /// <summary>
    /// Verifies that the Provider property returns <see cref="StorageProvider.Local"/>.
    /// </summary>
    [Fact]
    public void Provider_ShouldReturnLocal()
    {
        Assert.Equal(StorageProvider.Local, _service.Provider);
    }

    /// <summary>
    /// Verifies that attempting to resolve a path that escapes the base directory throws an <see cref="InvalidOperationException"/>.
    /// </summary>
    [Fact]
    public void ResolveLocation_PathTraversalAttempt_ShouldThrowInvalidOperationException()
    {
        // Arrange & Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            _service.ResolveLocation("context", "id", @"../../../../boot.ini");
        });
    }

    /// <summary>
    /// Verifies that attempting to escape into a sibling directory with a similar prefix throws an <see cref="InvalidOperationException"/>.
    /// </summary>
    [Fact]
    public void ResolveLocation_SiblingDirectoryEscapeAttempt_ShouldThrowInvalidOperationException()
    {
        // Arrange & Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            _service.ResolveLocation("..", Path.GetFileName(_testDirectory) + "_other", "file.csv");
        });
    }

    /// <summary>
    /// Verifies full asynchronous workflow: creating, writing, reading, inspecting metadata,
    /// listing files and directories, and deleting.
    /// </summary>
    [Fact]
    public async Task CreateWrite_OpenRead_GetMetadata_List_Delete_ShouldOperateCorrectlyAsync()
    {
        // Arrange
        string context = "ExperimentalData";
        string identifier = "Run_100";
        string fileName = "test_points.csv";
        byte[] content = "col1,col2\n1.0,2.0\n"u8.ToArray();

        // Act - CreateWriteAsync
        (Stream writeStream, string writtenLocation) = await _service.CreateWriteAsync(context, identifier, fileName);
        Assert.True(Path.IsPathRooted(writtenLocation));
        Assert.EndsWith(fileName, writtenLocation);

        await using (writeStream)
        {
            await writeStream.WriteAsync(content);
        }

        // Assert - ExistsAsync
        bool existsByLocation = await _service.ExistsAsync(writtenLocation);
        bool existsByTriple = await _service.ExistsAsync(context, identifier, fileName);
        Assert.True(existsByLocation);
        Assert.True(existsByTriple);

        // Assert - Metadata for File
        StorageEntryMetadata fileMetadata = await _service.GetMetadataAsync(context, identifier, fileName);
        Assert.Equal(fileName, fileMetadata.FileName);
        Assert.Equal(content.Length, fileMetadata.SizeBytes);
        Assert.Equal(StorageProvider.Local, fileMetadata.StorageProvider);
        Assert.True(fileMetadata.IsFile);
        Assert.False(fileMetadata.IsDirectory);
        Assert.True(System.IO.File.Exists(fileMetadata.FullPath));

        // Assert - Metadata by full path
        StorageEntryMetadata metadataByLocation = await _service.GetMetadataAsync(writtenLocation);
        Assert.Equal(fileMetadata.FileName, metadataByLocation.FileName);
        Assert.Equal(fileMetadata.SizeBytes, metadataByLocation.SizeBytes);
        Assert.Equal(fileMetadata.FullPath, metadataByLocation.FullPath);

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
        Assert.True(files[0].IsFile);

        // Assert - ListEntriesAsync (both files and directories)
        List<StorageEntryMetadata> entries = [];
        await foreach (StorageEntryMetadata item in _service.ListEntriesAsync(context, identifier))
        {
            entries.Add(item);
        }
        Assert.Single(entries);
        Assert.Equal(fileName, entries[0].FileName);

        // Act - Delete file
        bool deleted = await _service.DeleteAsync(context, identifier, fileName);
        Assert.True(deleted);

        // Verify file is gone
        bool existsAfterDelete = await _service.ExistsAsync(writtenLocation);
        Assert.False(existsAfterDelete);
    }

    /// <summary>
    /// Verifies that querying metadata on a directory returns directory metadata with null file name
    /// and recursive cumulative size of all files across nested subdirectories.
    /// </summary>
    [Fact]
    public async Task GetMetadataAsync_ForDirectory_ShouldReturnCumulativeSizeAndNullFileNameAsync()
    {
        // Arrange
        string context = "HierarchicalData";
        string identifier = "Sample_1";

        // Create a root file in identifier folder (10 bytes)
        (Stream stream1, string path1) = await _service.CreateWriteAsync(context, identifier, "file1.txt");
        await using (stream1)
        {
            await stream1.WriteAsync(new byte[10]);
        }

        // Create a subfolder with a nested file (25 bytes)
        string subfolderPath = Path.Combine(Path.GetDirectoryName(path1)!, "SubFolder");
        Directory.CreateDirectory(subfolderPath);
        string nestedFilePath = Path.Combine(subfolderPath, "file2.txt");
        await System.IO.File.WriteAllBytesAsync(nestedFilePath, new byte[25]);

        // Act - GetMetadataAsync on the identifier directory
        string identifierDir = Path.GetDirectoryName(path1)!;
        StorageEntryMetadata dirMetadata = await _service.GetMetadataAsync(identifierDir);

        // Assert
        Assert.Null(dirMetadata.FileName);
        Assert.True(dirMetadata.IsDirectory);
        Assert.False(dirMetadata.IsFile);
        Assert.Equal(35L, dirMetadata.SizeBytes); // 10 + 25 bytes
        Assert.Equal(StorageProvider.Local, dirMetadata.StorageProvider);

        // Act - ListEntriesAsync in identifier folder should contain file1.txt and SubFolder
        List<StorageEntryMetadata> entries = [];
        await foreach (StorageEntryMetadata entry in _service.ListEntriesAsync(context, identifier))
        {
            entries.Add(entry);
        }

        Assert.Equal(2, entries.Count);
        Assert.Contains(entries, e => e.IsFile && e.FileName == "file1.txt" && e.SizeBytes == 10L);
        Assert.Contains(entries, e => e.IsDirectory && e.FileName == null && e.SizeBytes == 25L);
    }

    /// <summary>
    /// Verifies that <see cref="LocalFileStorageService.GetMetadataAsync"/> throws <see cref="FileNotFoundException"/>
    /// when the requested path does not exist.
    /// </summary>
    [Fact]
    public async Task GetMetadataAsync_NonExistentLocation_ShouldThrowFileNotFoundExceptionAsync()
    {
        // Arrange
        string nonExistentPath = Path.Combine(_testDirectory, "non_existent", "missing.csv");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(async () =>
        {
            await _service.GetMetadataAsync(nonExistentPath);
        });
    }

    /// <summary>
    /// Verifies that CreateWriteTimebasedAsync creates a timestamped file and respects the prefix.
    /// </summary>
    [Fact]
    public async Task CreateWriteTimebasedAsync_ShouldCreateTimestampedFileAndReturnLocationAsync()
    {
        // Arrange
        string context = "SensitivityAnalysis";
        string identifier = "Sweep_200";
        string filePrefix = "StressSweep";
        string fileExtension = ".csv";
        byte[] content = "time,stress\n0.0,10.0\n"u8.ToArray();

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
        Assert.Equal(StorageProvider.Local, metadata.StorageProvider);

        List<StorageEntryMetadata> files = [];
        await foreach (StorageEntryMetadata item in _service.ListFilesAsync(context, identifier))
        {
            files.Add(item);
        }

        Assert.Single(files);
        Assert.StartsWith(filePrefix, files[0].FileName);
        Assert.EndsWith(fileExtension, files[0].FileName);
        Assert.Equal(content.Length, files[0].SizeBytes);
    }

    /// <summary>
    /// Verifies that CreateWriteTimebasedAsync without prefix generates a timestamp-led file name.
    /// </summary>
    [Fact]
    public async Task CreateWriteTimebasedAsync_WithoutPrefix_ShouldOmitPrefixInFileNameAsync()
    {
        // Arrange
        string context = "SensitivityAnalysis";
        string identifier = "Sweep_300";
        string fileExtension = ".csv";

        // Act
        (Stream writeStream, string location) = await _service.CreateWriteTimebasedAsync(context, identifier, fileExtension);
        await using (writeStream)
        {
            await writeStream.WriteAsync("time,stress\n"u8.ToArray());
        }

        // Assert
        string createdFileName = Path.GetFileName(location);
        Assert.EndsWith(fileExtension, createdFileName);
        Assert.True(char.IsDigit(createdFileName[0]));
    }
}
