using MelloSilveiraTools.Core.Caching;
using MelloSilveiraTools.Plugins.Infrastructure;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Persistences;
using System.Runtime.Loader;

namespace UnitTests.Plugins.Infrastructure;

public sealed class PluginCacheTests
{
    private readonly InMemoryTwoLevelCache _twoLevelCache = new();

    [Fact]
    public void GetOrAdd_ForDiscoveredPlugin_AddsAndReturnsPlugin()
    {
        PluginCache cache = new(_twoLevelCache);
        PluginVersion version = new(1, 0, 0);
        DiscoveredPlugin plugin = new("PluginA", version, "/pathA", DateTimeOffset.UtcNow);

        DiscoveredPlugin result1 = cache.GetOrAdd("PluginA", version, () => plugin);
        Func<DiscoveredPlugin> throwFactory = () => throw new InvalidOperationException("Should be cached");
        DiscoveredPlugin result2 = cache.GetOrAdd("PluginA", version, throwFactory);

        Assert.Same(plugin, result1);
        Assert.Same(plugin, result2);
    }

    [Fact]
    public void GetOrAdd_ForLoadedPlugin_EvictsDiscoveredAndReturnsLoaded()
    {
        PluginCache cache = new(_twoLevelCache);
        PluginVersion version = new(1, 0, 0);
        DiscoveredPlugin discovered = new("PluginA", version, "/pathA", DateTimeOffset.UtcNow);
        cache.GetOrAdd("PluginA", version, () => discovered);

        AssemblyLoadContext alc = new("TestAlc", isCollectible: true);
        LoadedPlugin loaded = new(discovered, [typeof(string)], alc);

        LoadedPlugin result = cache.GetOrAdd("PluginA", version, () => loaded);

        Assert.Same(loaded, result);
        Assert.True(cache.TryGet<LoadedPlugin>("PluginA", version, out LoadedPlugin? retrievedLoaded));
        Assert.Same(loaded, retrievedLoaded);
    }

    [Fact]
    public async Task GetOrAddAsync_ForLoadedPlugin_ReturnsCachedOrCallsFactory()
    {
        PluginCache cache = new(_twoLevelCache);
        PluginVersion version = new(1, 0, 0);
        DiscoveredPlugin discovered = new("PluginB", version, "/pathB", DateTimeOffset.UtcNow);
        AssemblyLoadContext alc = new("TestAlc2", isCollectible: true);
        LoadedPlugin loaded = new(discovered, [typeof(string)], alc);

        LoadedPlugin result1 = await cache.GetOrAddAsync("PluginB", version, async () =>
        {
            await Task.Yield();
            return loaded;
        });

        Func<Task<LoadedPlugin>> throwAsyncFactory = () => throw new InvalidOperationException("Should not be called");
        LoadedPlugin result2 = await cache.GetOrAddAsync("PluginB", version, throwAsyncFactory);

        Assert.Same(loaded, result1);
        Assert.Same(loaded, result2);
    }

    [Fact]
    public void GetOrAdd_ForRegisteredPlugin_EvictsDiscoveredAndReturnsRegistered()
    {
        PluginCache cache = new(_twoLevelCache);
        PluginVersion version = new(1, 0, 0);
        DiscoveredPlugin discovered = new("PluginC", version, "/pathC", DateTimeOffset.UtcNow);
        AssemblyLoadContext alc = new("TestAlc3", isCollectible: true);
        LoadedPlugin loaded = new(discovered, [typeof(string)], alc);
        RegisteredPlugin registered = new(loaded);

        RegisteredPlugin result = cache.GetOrAdd("PluginC", version, () => registered);

        Assert.Same(registered, result);
        Assert.True(cache.TryGet<RegisteredPlugin>("PluginC", version, out RegisteredPlugin? retrievedRegistered));
        Assert.Same(registered, retrievedRegistered);
    }

    [Fact]
    public void TryGet_WhenAbsent_ReturnsFalseAndNull()
    {
        PluginCache cache = new(_twoLevelCache);
        PluginVersion version = new(1, 0, 0);

        bool found = cache.TryGet<DiscoveredPlugin>("NonExistent", version, out DiscoveredPlugin? plugin);

        Assert.False(found);
        Assert.Null(plugin);
    }

    [Fact]
    public void Update_ReplacesEntryInCache()
    {
        PluginCache cache = new(_twoLevelCache);
        PluginVersion version = new(1, 0, 0);
        DiscoveredPlugin plugin1 = new("PluginD", version, "/pathD1", DateTimeOffset.UtcNow);
        DiscoveredPlugin plugin2 = new("PluginD", version, "/pathD2", DateTimeOffset.UtcNow);

        cache.Update("PluginD", version, plugin1);
        Assert.True(cache.TryGet<DiscoveredPlugin>("PluginD", version, out DiscoveredPlugin? retrieved1));
        Assert.Same(plugin1, retrieved1);

        cache.Update("PluginD", version, plugin2);
        Assert.True(cache.TryGet<DiscoveredPlugin>("PluginD", version, out DiscoveredPlugin? retrieved2));
        Assert.Same(plugin2, retrieved2);
    }

    [Fact]
    public void Clear_SpecificVersion_EvictsAllStageKeysAndUnloadsAssembly()
    {
        PluginCache cache = new(_twoLevelCache);
        PluginVersion version = new(1, 0, 0);
        DiscoveredPlugin discovered = new("PluginE", version, "/pathE", DateTimeOffset.UtcNow);
        AssemblyLoadContext alc = new("TestAlc4", isCollectible: true);
        LoadedPlugin loaded = new(discovered, [typeof(string)], alc);

        cache.GetOrAdd("PluginE", version, () => loaded);
        Assert.True(cache.TryGet<LoadedPlugin>("PluginE", version, out _));

        cache.Clear("PluginE", version);

        Assert.False(cache.TryGet<LoadedPlugin>("PluginE", version, out _));
        Assert.False(cache.TryGet<DiscoveredPlugin>("PluginE", version, out _));
        Assert.False(cache.TryGet<RegisteredPlugin>("PluginE", version, out _));
    }

    [Fact]
    public async Task ClearAsync_PurgesAllCachedPlugins()
    {
        PluginCache cache = new(_twoLevelCache);
        PluginVersion v1 = new(1, 0, 0);
        PluginVersion v2 = new(2, 0, 0);

        cache.Update("PluginF", v1, new DiscoveredPlugin("PluginF", v1, "/p1", DateTimeOffset.UtcNow));
        cache.Update("PluginG", v2, new DiscoveredPlugin("PluginG", v2, "/p2", DateTimeOffset.UtcNow));

        await cache.ClearAsync();

        Assert.False(cache.TryGet<DiscoveredPlugin>("PluginF", v1, out _));
        Assert.False(cache.TryGet<DiscoveredPlugin>("PluginG", v2, out _));
    }

    [Fact]
    public async Task Stream_StreamsEntriesWithPluginCacheEntryMapping()
    {
        PluginCache cache = new(_twoLevelCache);
        PluginVersion v1 = new(1, 0, 0);
        PluginVersion v2 = new(1, 1, 0);
        DiscoveredPlugin plugin1 = new("PluginH", v1, "/pH1", DateTimeOffset.UtcNow);
        DiscoveredPlugin plugin2 = new("PluginH", v2, "/pH2", DateTimeOffset.UtcNow);

        cache.Update("PluginH", v1, plugin1);
        cache.Update("PluginH", v2, plugin2);

        List<PluginCacheEntry> entries = [];
        await foreach (PluginCacheEntry entry in cache.Stream())
        {
            entries.Add(entry);
        }

        Assert.Equal(2, entries.Count);
        Assert.All(entries, e => Assert.Equal("PluginH", e.Name));

        // Filtered stream
        List<PluginCacheEntry> filtered = [];
        await foreach (PluginCacheEntry entry in cache.Stream("PluginH", v2))
        {
            filtered.Add(entry);
        }

        Assert.Single(filtered);
        Assert.Equal("v1.1.0", filtered[0].Version);
    }
}
