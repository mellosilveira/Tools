using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Persistences;
using Moq;
using System.Runtime.Loader;
using System.Text.Json;

namespace UnitTests.Plugins.Infrastructure.Persistences;

public sealed class DatabasePluginCachePersistenceTests
{
    private readonly Mock<IRepository> _repositoryMock = new();

    [Fact]
    public async Task SaveAsync_PersistsEntriesViaRepositoryUpsert()
    {
        DiscoveredPlugin plugin = new("MathPlugin", new PluginVersion(1, 0, 0), "/path/MathPlugin.v1.0.0.dll", DateTimeOffset.UtcNow);
        PluginCacheEntry entry = new("MathPlugin", "v1.0.0", plugin);

        _repositoryMock
            .Setup(r => r.UpsertAsync(It.IsAny<PluginCacheEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);

        DatabasePluginCachePersistence persistence = new(_repositoryMock.Object);

        await persistence.SaveAsync(CreateAsyncEnumerable([entry]));

        _repositoryMock.Verify(
            r => r.UpsertAsync(
                It.Is<PluginCacheEntity>(e =>
                    e.PluginName == "MathPlugin" &&
                    e.PluginVersion == "v1.0.0" &&
                    e.StateType == nameof(DiscoveredPlugin) &&
                    !string.IsNullOrEmpty(e.StateJson)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task LoadAsync_DeserializesDiscoveredLoadedAndRegisteredPluginEntities()
    {
        DiscoveredPlugin discovered = new("PluginA", new PluginVersion(1, 0, 0), "/pathA", DateTimeOffset.UtcNow);
        AssemblyLoadContext alc = new("DbTestContext", isCollectible: true);
        LoadedPlugin loaded = new(discovered, [typeof(string)], alc);
        RegisteredPlugin registered = new(loaded);
        registered.MarkTypeLoaded(typeof(string), DateTimeOffset.UtcNow);
        alc.Unload();

        JsonSerializerOptions jsonOptions = new() { Converters = { new TypeJsonConverter() } };

        PluginCacheEntity entity1 = new()
        {
            PluginName = "PluginA",
            PluginVersion = "v1.0.0",
            StateType = nameof(DiscoveredPlugin),
            StateJson = JsonSerializer.Serialize(discovered, jsonOptions)
        };

        PluginCacheEntity entity2 = new()
        {
            PluginName = "PluginA",
            PluginVersion = "v1.0.0",
            StateType = nameof(LoadedPlugin),
            StateJson = JsonSerializer.Serialize(loaded, jsonOptions)
        };

        PluginCacheEntity entity3 = new()
        {
            PluginName = "PluginA",
            PluginVersion = "v1.0.0",
            StateType = nameof(RegisteredPlugin),
            StateJson = JsonSerializer.Serialize(registered, jsonOptions)
        };

        _repositoryMock
            .Setup(r => r.GetAsync<PluginCacheEntity, PluginCacheFilter>(
                It.Is<PluginCacheFilter>(f => f.PluginName == "PluginA" && f.PluginVersion == "v1.0.0"),
                It.IsAny<Pagination?>(),
                It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncEnumerable([entity1, entity2, entity3]));

        DatabasePluginCachePersistence persistence = new(_repositoryMock.Object);

        List<PluginCacheEntry> result = [];
        await foreach (PluginCacheEntry item in persistence.LoadAsync("PluginA", new PluginVersion(1, 0, 0)))
        {
            result.Add(item);
        }

        Assert.Equal(3, result.Count);
        Assert.IsType<DiscoveredPlugin>(result[0].State);
        Assert.IsType<LoadedPlugin>(result[1].State);
        Assert.IsType<RegisteredPlugin>(result[2].State);
        Assert.True(((RegisteredPlugin)result[2].State).IsFullyLoaded);
    }

    [Fact]
    public async Task LoadAsync_WhenStateTypeIsUnsupported_ThrowsNotSupportedException()
    {
        PluginCacheEntity invalid = new()
        {
            PluginName = "Unknown",
            PluginVersion = "v1.0.0",
            StateType = "CorruptType",
            StateJson = "{}"
        };

        _repositoryMock
            .Setup(r => r.GetAsync<PluginCacheEntity, PluginCacheFilter>(
                It.IsAny<PluginCacheFilter>(),
                It.IsAny<Pagination?>(),
                It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncEnumerable([invalid]));

        DatabasePluginCachePersistence persistence = new(_repositoryMock.Object);

        await Assert.ThrowsAsync<NotSupportedException>(async () =>
        {
            await foreach (PluginCacheEntry _ in persistence.LoadAsync())
            {
            }
        });
    }

    private static async IAsyncEnumerable<T> CreateAsyncEnumerable<T>(IEnumerable<T> items)
    {
        foreach (T item in items)
        {
            await Task.Yield();
            yield return item;
        }
    }
}
