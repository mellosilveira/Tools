using AcceptanceTests.Plugins.Helpers;
using MelloSilveiraTools.Plugins.Infrastructure;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AcceptanceTests.Plugins;

/// <summary>
/// Acceptance tests for plugin cache persistence, verifying that memory cache states
/// serialize accurately into the cache storage partition (e.g. <c>plugin-cache.json</c>)
/// and survive memory purges through full state restoration.
/// </summary>
public sealed class PluginCacheAcceptanceTests
{
    /// <summary>
    /// Verifies that registered plugin states can be persisted to a JSON file in the cache partition,
    /// survive an in-memory cache purge, and be successfully restored.
    /// </summary>
    [Fact]
    public async Task PersistAndRestoreCacheAsync_ShouldRoundTripRegisteredPluginsThroughStoragePartition()
    {
        // Arrange
        await using PluginTestFixture fixture = new();
        byte[] validBytes = PluginTestFixture.GetValidPluginBytes();
        string fileName = "CacheablePlugin.v3.1.2.dll";

        fixture.LocalStorage.AddFile("plugins", PluginStoragePartitions.Incoming, fileName, validBytes);

        using IServiceScope scope = fixture.CreateScope();
        IPluginService pluginService = scope.ServiceProvider.GetRequiredService<IPluginService>();
        PluginCache cache = scope.ServiceProvider.GetRequiredService<PluginCache>();

        await pluginService.LoadPluginsOnStartupAsync(fixture.Services);

        // Act 1: Persist Cache
        await pluginService.PersistCacheAsync();

        // Assert 1: Cache file written to storage
        bool cacheFileExists = fixture.LocalStorage.FileExists("plugins", PluginStoragePartitions.Cache, "plugin-cache.json");
        Assert.True(cacheFileExists);

        byte[]? cacheFileBytes = fixture.LocalStorage.GetFileBytes("plugins", PluginStoragePartitions.Cache, "plugin-cache.json");
        Assert.NotNull(cacheFileBytes);
        Assert.True(cacheFileBytes.Length > 0);

        // Act 2: Purge in-memory cache
        await pluginService.ClearAsync();

        bool isPresentBeforeRestore = cache.TryGet("CacheablePlugin", new PluginVersion(3, 1, 2), out RegisteredPlugin? _);
        Assert.False(isPresentBeforeRestore);

        // Act 3: Restore Cache from storage partition
        await pluginService.RestoreCacheAsync();

        // Assert 3: State restored into cache
        bool isPresentAfterRestore = cache.TryGet("CacheablePlugin", new PluginVersion(3, 1, 2), out RegisteredPlugin? restoredPlugin);
        Assert.True(isPresentAfterRestore);
        Assert.NotNull(restoredPlugin);
        Assert.Equal("CacheablePlugin", restoredPlugin.Name);
        Assert.Equal(new PluginVersion(3, 1, 2), restoredPlugin.Version);
    }

    /// <summary>
    /// Verifies that calling <see cref="IPluginService.ClearAsync"/> flushes all active
    /// memory cache entries across versions.
    /// </summary>
    [Fact]
    public async Task ClearCacheAsync_ShouldPurgeInMemoryEntries()
    {
        // Arrange
        await using PluginTestFixture fixture = new();
        byte[] validBytes = PluginTestFixture.GetValidPluginBytes();
        string fileName = "TransientPlugin.v1.0.0.dll";

        fixture.LocalStorage.AddFile("plugins", PluginStoragePartitions.Incoming, fileName, validBytes);

        using IServiceScope scope = fixture.CreateScope();
        IPluginService pluginService = scope.ServiceProvider.GetRequiredService<IPluginService>();
        PluginCache cache = scope.ServiceProvider.GetRequiredService<PluginCache>();

        await pluginService.LoadPluginsOnStartupAsync(fixture.Services);
        Assert.True(cache.TryGet<RegisteredPlugin>("TransientPlugin", new PluginVersion(1, 0, 0), out RegisteredPlugin? _));

        // Act
        await pluginService.ClearAsync();

        // Assert
        Assert.False(cache.TryGet<RegisteredPlugin>("TransientPlugin", new PluginVersion(1, 0, 0), out RegisteredPlugin? _));
    }
}
