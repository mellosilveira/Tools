using MelloSilveiraTools.Core.Managers.File.Models;
using MelloSilveiraTools.Core.Managers.File.Providers;
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
        PreviousVersionRetention = TimeSpan.FromHours(24)
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

    private static async IAsyncEnumerable<PluginCacheEntry> CreateAsyncEnumerable(IEnumerable<PluginCacheEntry> items)
    {
        foreach (PluginCacheEntry item in items)
        {
            yield return item;
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }
}
