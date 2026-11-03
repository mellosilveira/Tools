using MelloSilveiraTools.Core.Caching;
using MelloSilveiraTools.Core.Managers.File.Models;
using MelloSilveiraTools.Core.Managers.File.Providers;
using MelloSilveiraTools.Plugins.Infrastructure;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace UnitTests.Plugins.Infrastructure;

public sealed class PluginFileProcessorTests
{
    private readonly Mock<IFileStorageService> _storageMock = new();
    private readonly PluginCache _cache = new(new InMemoryTwoLevelCache());
    private readonly PluginSettings _settings = new()
    {
        StorageProvider = StorageProvider.Local,
        Directory = "plugins",
        DefaultCacheTarget = "file",
        PollInterval = TimeSpan.FromSeconds(30),
        PreviousVersionRetention = TimeSpan.FromHours(24)
    };

    [Fact]
    public async Task ScanAsync_WhenValidManagedPlugin_YieldsDiscoveredPlugin()
    {
        // Arrange
        byte[] validBytes = File.ReadAllBytes(typeof(PluginFileProcessor).Assembly.Location);
        string fileName = "MyModule.v1.0.0.dll";
        string filePath = "storage/plugins/incoming/MyModule.v1.0.0.dll";

        StorageEntryMetadata entry = new(
            StorageProvider.Local,
            filePath,
            "storage/plugins/incoming",
            fileName,
            validBytes.Length,
            DateTimeOffset.UtcNow);

        _storageMock
            .Setup(s => s.ListFilesAsync("plugins", PluginStoragePartitions.Incoming, It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncEnumerable([entry]));

        _storageMock
            .Setup(s => s.OpenReadAsync(filePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(validBytes));

        PluginFileProcessor processor = new(NullLogger<PluginFileProcessor>.Instance, _storageMock.Object, _cache, _settings);

        // Act
        List<DiscoveredPlugin> discovered = [];
        await foreach (DiscoveredPlugin plugin in processor.ScanAsync())
        {
            discovered.Add(plugin);
        }

        // Assert
        Assert.Single(discovered);
        Assert.Equal("MyModule", discovered[0].Name);
        Assert.Equal(new PluginVersion(1, 0, 0), discovered[0].Version);
    }

    [Fact]
    public async Task ScanAsync_WhenPatternDoesNotMatch_MovesToQuarantine()
    {
        // Arrange
        string fileName = "MalformedFile.dll";
        string filePath = "storage/plugins/incoming/MalformedFile.dll";
        string quarantinePath = "storage/plugins/quarantine/MalformedFile.dll";

        StorageEntryMetadata entry = new(
            StorageProvider.Local,
            filePath,
            "storage/plugins/incoming",
            fileName,
            100L,
            DateTimeOffset.UtcNow);

        _storageMock
            .Setup(s => s.ListFilesAsync("plugins", PluginStoragePartitions.Incoming, It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncEnumerable([entry]));

        _storageMock
            .Setup(s => s.ResolveLocation("plugins", PluginStoragePartitions.Quarantine, fileName))
            .Returns(quarantinePath);

        _storageMock
            .Setup(s => s.OpenReadAsync(filePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream([1, 2, 3]));

        _storageMock
            .Setup(s => s.OpenWriteAsync(quarantinePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream());

        PluginFileProcessor processor = new(NullLogger<PluginFileProcessor>.Instance, _storageMock.Object, _cache, _settings);

        // Act
        List<DiscoveredPlugin> discovered = [];
        await foreach (DiscoveredPlugin plugin in processor.ScanAsync())
        {
            discovered.Add(plugin);
        }

        // Assert
        Assert.Empty(discovered);
        _storageMock.Verify(s => s.DeleteAsync(filePath, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ScanAsync_WhenNotManagedDotNetAssembly_MovesToQuarantine()
    {
        // Arrange
        byte[] fakeNativeBytes = [0x4D, 0x5A, 0x00, 0x00, 0x00];
        string fileName = "NativeLib.v1.0.0.dll";
        string filePath = "storage/plugins/incoming/NativeLib.v1.0.0.dll";
        string quarantinePath = "storage/plugins/quarantine/NativeLib.v1.0.0.dll";

        StorageEntryMetadata entry = new(
            StorageProvider.Local,
            filePath,
            "storage/plugins/incoming",
            fileName,
            fakeNativeBytes.Length,
            DateTimeOffset.UtcNow);

        _storageMock
            .Setup(s => s.ListFilesAsync("plugins", PluginStoragePartitions.Incoming, It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncEnumerable([entry]));

        _storageMock
            .Setup(s => s.OpenReadAsync(filePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream(fakeNativeBytes));

        _storageMock
            .Setup(s => s.ResolveLocation("plugins", PluginStoragePartitions.Quarantine, fileName))
            .Returns(quarantinePath);

        _storageMock
            .Setup(s => s.OpenWriteAsync(quarantinePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream());

        PluginFileProcessor processor = new(NullLogger<PluginFileProcessor>.Instance, _storageMock.Object, _cache, _settings);

        // Act
        List<DiscoveredPlugin> discovered = [];
        await foreach (DiscoveredPlugin plugin in processor.ScanAsync())
        {
            discovered.Add(plugin);
        }

        // Assert
        Assert.Empty(discovered);
        _storageMock.Verify(s => s.DeleteAsync(filePath, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MoveToLoadedFolderAsync_WhenDestinationAbsent_MovesDirectly()
    {
        // Arrange
        string fileName = "Plugin.v1.0.0.dll";
        string sourcePath = "storage/plugins/incoming/Plugin.v1.0.0.dll";
        string destinationPath = "storage/plugins/loaded/Plugin.v1.0.0.dll";

        DiscoveredPlugin plugin = new("Plugin", new PluginVersion(1, 0, 0), sourcePath, DateTimeOffset.UtcNow);

        _storageMock
            .Setup(s => s.ResolveLocation("plugins", PluginStoragePartitions.Loaded, fileName))
            .Returns(destinationPath);

        _storageMock
            .Setup(s => s.ExistsAsync(destinationPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _storageMock
            .Setup(s => s.OpenReadAsync(sourcePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream([1, 2, 3]));

        MemoryStream destinationStream = new();
        _storageMock
            .Setup(s => s.OpenWriteAsync(destinationPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(destinationStream);

        PluginFileProcessor processor = new(NullLogger<PluginFileProcessor>.Instance, _storageMock.Object, _cache, _settings);

        // Act
        await processor.MoveToLoadedFolderAsync(plugin);

        // Assert
        _storageMock.Verify(s => s.DeleteAsync(sourcePath, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MoveToMainFolderAsync_MovesBackToIncomingPartition()
    {
        // Arrange
        string fileName = "Plugin.v1.0.0.dll";
        string sourcePath = "storage/plugins/loaded/Plugin.v1.0.0.dll";
        string incomingPath = "storage/plugins/incoming/Plugin.v1.0.0.dll";

        DiscoveredPlugin plugin = new("Plugin", new PluginVersion(1, 0, 0), sourcePath, DateTimeOffset.UtcNow);

        _storageMock
            .Setup(s => s.ResolveLocation("plugins", PluginStoragePartitions.Incoming, fileName))
            .Returns(incomingPath);

        _storageMock
            .Setup(s => s.OpenReadAsync(sourcePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream([1, 2, 3]));

        MemoryStream incomingStream = new();
        _storageMock
            .Setup(s => s.OpenWriteAsync(incomingPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incomingStream);

        PluginFileProcessor processor = new(NullLogger<PluginFileProcessor>.Instance, _storageMock.Object, _cache, _settings);

        // Act
        await processor.MoveToMainFolderAsync(plugin);

        // Assert
        _storageMock.Verify(s => s.ResolveLocation("plugins", PluginStoragePartitions.Incoming, fileName), Times.Once);
        _storageMock.Verify(s => s.DeleteAsync(sourcePath, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExistsInLoadedFolderAsync_ChecksExistence()
    {
        // Arrange
        string fileName = "Plugin.v1.0.0.dll";
        string sourcePath = "storage/plugins/incoming/Plugin.v1.0.0.dll";
        string loadedPath = "storage/plugins/loaded/Plugin.v1.0.0.dll";

        DiscoveredPlugin plugin = new("Plugin", new PluginVersion(1, 0, 0), sourcePath, DateTimeOffset.UtcNow);

        _storageMock
            .Setup(s => s.ResolveLocation("plugins", PluginStoragePartitions.Loaded, fileName))
            .Returns(loadedPath);

        _storageMock
            .Setup(s => s.ExistsAsync(loadedPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        PluginFileProcessor processor = new(NullLogger<PluginFileProcessor>.Instance, _storageMock.Object, _cache, _settings);

        // Act
        bool exists = await processor.ExistsInLoadedFolderAsync(plugin);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task MoveToArchiveFolderAsync_MovesToArchivePartition()
    {
        // Arrange
        string fileName = "Plugin.v1.0.0.dll";
        string sourcePath = "storage/plugins/loaded/Plugin.v1.0.0.dll";
        string archivePath = "storage/plugins/archive/Plugin.v1.0.0.dll";

        DiscoveredPlugin plugin = new("Plugin", new PluginVersion(1, 0, 0), sourcePath, DateTimeOffset.UtcNow);

        _storageMock
            .Setup(s => s.ResolveLocation("plugins", PluginStoragePartitions.Archive, fileName))
            .Returns(archivePath);

        _storageMock
            .Setup(s => s.OpenReadAsync(sourcePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream([1, 2, 3]));

        MemoryStream archiveStream = new();
        _storageMock
            .Setup(s => s.OpenWriteAsync(archivePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(archiveStream);

        PluginFileProcessor processor = new(NullLogger<PluginFileProcessor>.Instance, _storageMock.Object, _cache, _settings);

        // Act
        await processor.MoveToArchiveFolderAsync(plugin);

        // Assert
        _storageMock.Verify(s => s.ResolveLocation("plugins", PluginStoragePartitions.Archive, fileName), Times.Once);
        _storageMock.Verify(s => s.DeleteAsync(sourcePath, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static async IAsyncEnumerable<StorageEntryMetadata> CreateAsyncEnumerable(IEnumerable<StorageEntryMetadata> items)
    {
        foreach (StorageEntryMetadata item in items)
        {
            yield return item;
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }
}
