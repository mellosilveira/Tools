using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Plugins.Infrastructure;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Persistences;
using Moq;

namespace UnitTests.Plugins.Infrastructure.Persistences;

public sealed class JsonFilePluginCachePersistenceTests
{
    private readonly Mock<IFileStorageService> _storageMock = new();
    private readonly PluginSettings _settings = new()
    {
        StorageProvider = StorageProvider.Local,
        Directory = "plugins",
        DefaultCacheTarget = "file",
        PollInterval = TimeSpan.FromSeconds(30),
        PreviousVersionRetention = TimeSpan.FromHours(24),
        MaxPluginSizeBytes = 10L * 1024 * 1024,
        MaxPendingSubmissionsPerUser = 5
    };

    [Fact]
    public async Task LoadAsync_WhenFileDoesNotExist_YieldsEmpty()
    {
        // Arrange
        string cacheLocation = "storage/plugins/cache/plugin-cache.json";
        _storageMock
            .Setup(s => s.ResolveLocation("plugins", PluginStoragePartitions.Cache, "plugin-cache.json"))
            .Returns(cacheLocation);

        _storageMock
            .Setup(s => s.ExistsAsync(cacheLocation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        JsonFilePluginCachePersistence persistence = new(_storageMock.Object, _settings);

        // Act
        List<PluginCacheEntry> entries = [];
        await foreach (PluginCacheEntry entry in persistence.LoadAsync())
        {
            entries.Add(entry);
        }

        // Assert
        Assert.Empty(entries);
    }

    [Fact]
    public async Task SaveAsync_And_LoadAsync_RoundtripsEntriesSuccessfully()
    {
        // Arrange
        string cacheLocation = "storage/plugins/cache/plugin-cache.json";
        _storageMock
            .Setup(s => s.ResolveLocation("plugins", PluginStoragePartitions.Cache, "plugin-cache.json"))
            .Returns(cacheLocation);

        MemoryStream stream = new();

        _storageMock
            .Setup(s => s.OpenWriteAsync(cacheLocation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stream);

        _storageMock
            .Setup(s => s.ExistsAsync(cacheLocation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _storageMock
            .Setup(s => s.OpenReadAsync(cacheLocation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream(stream.ToArray()));

        JsonFilePluginCachePersistence persistence = new(_storageMock.Object, _settings);

        DiscoveredPlugin plugin = new("Calculators", new PluginVersion(1, 0, 0), "path/Calculators.v1.0.0.dll", DateTimeOffset.UtcNow);
        PluginCacheEntry entry = new("Calculators", "v1.0.0", plugin);

        // Act - Save
        await persistence.SaveAsync(CreateAsyncEnumerable([entry]));

        // Act - Load
        List<PluginCacheEntry> loaded = [];
        await foreach (PluginCacheEntry item in persistence.LoadAsync())
        {
            loaded.Add(item);
        }

        // Assert
        Assert.Single(loaded);
        Assert.Equal("Calculators", loaded[0].Name);
        Assert.Equal("v1.0.0", loaded[0].Version);
    }

    [Fact]
    public async Task LoadAsync_WithFilters_FiltersByNameAndVersion()
    {
        // Arrange
        string cacheLocation = "storage/plugins/cache/plugin-cache.json";
        _storageMock
            .Setup(s => s.ResolveLocation("plugins", PluginStoragePartitions.Cache, "plugin-cache.json"))
            .Returns(cacheLocation);

        MemoryStream stream = new();
        _storageMock
            .Setup(s => s.OpenWriteAsync(cacheLocation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stream);

        _storageMock
            .Setup(s => s.ExistsAsync(cacheLocation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _storageMock
            .Setup(s => s.OpenReadAsync(cacheLocation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream(stream.ToArray()));

        JsonFilePluginCachePersistence persistence = new(_storageMock.Object, _settings);

        DiscoveredPlugin plugin1 = new("PluginA", new PluginVersion(1, 0, 0), "/pathA", DateTimeOffset.UtcNow);
        DiscoveredPlugin plugin2 = new("PluginA", new PluginVersion(2, 0, 0), "/pathA2", DateTimeOffset.UtcNow);
        DiscoveredPlugin plugin3 = new("PluginB", new PluginVersion(1, 0, 0), "/pathB", DateTimeOffset.UtcNow);

        await persistence.SaveAsync(CreateAsyncEnumerable([
            new PluginCacheEntry("PluginA", "v1.0.0", plugin1),
            new PluginCacheEntry("PluginA", "v2.0.0", plugin2),
            new PluginCacheEntry("PluginB", "v1.0.0", plugin3)
        ]));

        // Filter by Name and Version
        List<PluginCacheEntry> filtered = [];
        await foreach (PluginCacheEntry item in persistence.LoadAsync("PluginA", new PluginVersion(2, 0, 0)))
        {
            filtered.Add(item);
        }

        Assert.Single(filtered);
        Assert.Equal("PluginA", filtered[0].Name);
        Assert.Equal("v2.0.0", filtered[0].Version);

        // Filter by Name only
        List<PluginCacheEntry> filteredByName = [];
        await foreach (PluginCacheEntry item in persistence.LoadAsync("PluginA", null))
        {
            filteredByName.Add(item);
        }

        Assert.Equal(2, filteredByName.Count);
    }

    [Fact]
    public async Task LoadAsync_WhenFileIsEmpty_YieldsEmpty()
    {
        string cacheLocation = "storage/plugins/cache/plugin-cache.json";
        _storageMock
            .Setup(s => s.ResolveLocation("plugins", PluginStoragePartitions.Cache, "plugin-cache.json"))
            .Returns(cacheLocation);

        _storageMock
            .Setup(s => s.ExistsAsync(cacheLocation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _storageMock
            .Setup(s => s.OpenReadAsync(cacheLocation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream());

        JsonFilePluginCachePersistence persistence = new(_storageMock.Object, _settings);

        List<PluginCacheEntry> loaded = [];
        await foreach (PluginCacheEntry item in persistence.LoadAsync())
        {
            loaded.Add(item);
        }

        Assert.Empty(loaded);
    }

    private sealed class NonSeekableJsonStream(byte[] bytes) : MemoryStream(bytes)
    {
        public override bool CanSeek => false;
    }

    [Fact]
    public async Task LoadAsync_HandlesRegisteredLoadedNullElementsNonArrayRootInvalidJsonAndNonSeekableStreams()
    {
        string cacheLocation = "storage/plugins/cache/plugin-cache.json";
        _storageMock
            .Setup(s => s.ResolveLocation("plugins", PluginStoragePartitions.Cache, "plugin-cache.json"))
            .Returns(cacheLocation);
        _storageMock
            .Setup(s => s.ExistsAsync(cacheLocation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        JsonFilePluginCachePersistence persistence = new(_storageMock.Object, _settings);

        // 1. RegisteredPlugin, LoadedPlugin, unknown StateType (falls back to DiscoveredPlugin), and null State
        DateTimeOffset now = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
        DiscoveredPlugin disc = new("P1", new PluginVersion(1, 0, 0), "/p1", now);
        string mixedJson = """
        [
          {"Name":"Reg","Version":"1.0.0","StateType":"RegisteredPlugin","State":{"Name":"Reg","Version":"1.0.0","FullPath":"/r","DiscoveredAt":"2026-06-01T12:00:00Z"}},
          {"Name":"Ld","Version":"1.0.0","StateType":"LoadedPlugin","State":{"Name":"Ld","Version":"1.0.0","FullPath":"/l","DiscoveredAt":"2026-06-01T12:00:00Z"}},
          {"Name":"Other","Version":"1.0.0","StateType":"CustomState","State":{"Name":"Other","Version":"1.0.0","FullPath":"/o","DiscoveredAt":"2026-06-01T12:00:00Z"}},
          {"Name":"NullState","Version":"1.0.0","StateType":"DiscoveredPlugin","State":null},
          null
        ]
        """;

        _storageMock
            .Setup(s => s.OpenReadAsync(cacheLocation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new NonSeekableJsonStream(System.Text.Encoding.UTF8.GetBytes(mixedJson)));

        List<PluginCacheEntry> mixedEntries = [];
        await foreach (PluginCacheEntry entry in persistence.LoadAsync())
        {
            mixedEntries.Add(entry);
        }

        Assert.Equal(3, mixedEntries.Count);
        Assert.IsType<RegisteredPlugin>(mixedEntries[0].State);
        Assert.IsType<LoadedPlugin>(mixedEntries[1].State);
        Assert.IsType<DiscoveredPlugin>(mixedEntries[2].State);

        // 2. Non-array JSON root object yields empty
        _storageMock
            .Setup(s => s.OpenReadAsync(cacheLocation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new NonSeekableJsonStream("{}"u8.ToArray()));

        List<PluginCacheEntry> objectRootEntries = [];
        await foreach (PluginCacheEntry entry in persistence.LoadAsync())
        {
            objectRootEntries.Add(entry);
        }
        Assert.Empty(objectRootEntries);

        // 3. Invalid JSON yields empty cleanly
        _storageMock
            .Setup(s => s.OpenReadAsync(cacheLocation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream("{invalid"u8.ToArray()));

        List<PluginCacheEntry> corruptEntries = [];
        await foreach (PluginCacheEntry entry in persistence.LoadAsync())
        {
            corruptEntries.Add(entry);
        }
        Assert.Empty(corruptEntries);

        // 4. Empty non-seekable stream yields empty
        _storageMock
            .Setup(s => s.OpenReadAsync(cacheLocation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new NonSeekableJsonStream([]));

        List<PluginCacheEntry> emptyNonSeekable = [];
        await foreach (PluginCacheEntry entry in persistence.LoadAsync())
        {
            emptyNonSeekable.Add(entry);
        }
        Assert.Empty(emptyNonSeekable);
    }

    private static async IAsyncEnumerable<PluginCacheEntry> CreateAsyncEnumerable(IEnumerable<PluginCacheEntry> items)
    {
        foreach (PluginCacheEntry item in items)
        {
            yield return item;
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }
}
