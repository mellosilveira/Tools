using MelloSilveiraTools.Core.Caching;
using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Providers.Dynamics;
using MelloSilveiraTools.Plugins.Infrastructure;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Persistences;
using MelloSilveiraTools.Plugins.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace UnitTests.Plugins.Infrastructure.Services;

public sealed class PluginServiceTests
{
    private readonly Mock<IPluginCachePersistence> _persistenceMock = new();
    private readonly Mock<IDynamicServiceProvider> _dynamicProviderMock = new();
    private readonly Mock<IFileStorageService> _storageMock = new();
    private readonly PluginCache _cache = new(new InMemoryTwoLevelCache());
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

    private sealed class DummyTypeProcessor : IPluginTypeProcessor
    {
        public Type ProcessableType => typeof(object);
        public List<Type> Processed { get; } = [];
        public void Process(Type type, PluginRegistrationContext context) => Processed.Add(type);
    }

    private PluginService CreateService(params IPluginTypeProcessor[] processors)
    {
        PluginFileProcessor fileProcessor = new(NullLogger<PluginFileProcessor>.Instance, _storageMock.Object, _cache, _settings);
        PluginAssemblyProcessor assemblyProcessor = new(processors, TimeProvider.System, fileProcessor, _cache, _settings);
        return new PluginService(_persistenceMock.Object, _dynamicProviderMock.Object, fileProcessor, assemblyProcessor, _cache);
    }

    [Fact]
    public async Task ClearPersistAndRestoreCacheAsync_DelegateProperly()
    {
        PluginVersion v1 = new(1, 0, 0);
        DiscoveredPlugin pluginA = new("PluginA", v1, "/pA", DateTimeOffset.UtcNow);
        _cache.Update("PluginA", v1, pluginA);

        _persistenceMock
            .Setup(p => p.SaveAsync(It.IsAny<IAsyncEnumerable<PluginCacheEntry>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        PluginService service = CreateService();

        await service.PersistCacheAsync("PluginA", v1);
        _persistenceMock.Verify(p => p.SaveAsync(It.IsAny<IAsyncEnumerable<PluginCacheEntry>>(), It.IsAny<CancellationToken>()), Times.Once);

        await service.ClearAsync();
        Assert.False(_cache.TryGet<DiscoveredPlugin>("PluginA", v1, out _));

        DiscoveredPlugin pluginB = new("PluginB", v1, "/pB", DateTimeOffset.UtcNow);
        _persistenceMock
            .Setup(p => p.LoadAsync("PluginB", v1, It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncEnumerable([new PluginCacheEntry("PluginB", "v1.0.0", pluginB)]));

        await service.RestoreCacheAsync("PluginB", v1);
        Assert.True(_cache.TryGet<DiscoveredPlugin>("PluginB", v1, out DiscoveredPlugin? restored));
        Assert.Equal("PluginB", restored?.Name);
    }

    [Fact]
    public async Task LoadPluginsOnRuntimeAndStartupAsync_LoadsAssemblyAndMovesToLoaded()
    {
        byte[] assemblyBytes = File.ReadAllBytes(typeof(PluginService).Assembly.Location);
        string fileName = "Mod.v1.0.0.dll";
        string incomingLocation = $"storage/plugins/incoming/{fileName}";
        string loadedLocation = $"storage/plugins/loaded/{fileName}";

        StorageEntryMetadata incomingMeta = new(StorageProvider.Local, incomingLocation, "storage/plugins/incoming", fileName, assemblyBytes.Length, DateTimeOffset.UtcNow);
        _storageMock
            .Setup(s => s.ListFilesAsync("plugins", PluginStoragePartitions.Incoming, It.IsAny<CancellationToken>()))
            .Returns(() => CreateAsyncEnumerable([incomingMeta]));
        _storageMock
            .Setup(s => s.OpenReadAsync(incomingLocation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream(assemblyBytes));
        _storageMock
            .Setup(s => s.ResolveLocation("plugins", PluginStoragePartitions.Loaded, fileName))
            .Returns(loadedLocation);
        _storageMock
            .Setup(s => s.ExistsAsync(loadedLocation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _storageMock
            .Setup(s => s.OpenWriteAsync(loadedLocation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream());
        _storageMock
            .Setup(s => s.DeleteAsync(incomingLocation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        DummyTypeProcessor processor = new();
        PluginService service = CreateService(processor);

        await service.LoadPluginsOnRuntimeAsync();
        Assert.Contains(processor.Processed, t => t.Name == nameof(PluginService));
        _storageMock.Verify(s => s.MoveAsync(incomingLocation, loadedLocation, true, It.IsAny<CancellationToken>()), Times.Once);

        // Calling startup load when already cached & fully loaded still completes without throwing
        ServiceCollection services = new();
        await service.LoadPluginsOnStartupAsync(services);
        _storageMock.Verify(s => s.MoveAsync(incomingLocation, loadedLocation, true, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ReloadPluginsAndGetPluginsAsync_CoverBothForceAndNonForceAndBothPartitions()
    {
        byte[] assemblyBytes = File.ReadAllBytes(typeof(PluginService).Assembly.Location);
        string fileName = "Mod.v1.2.0.dll";
        string incomingLocation = $"storage/plugins/incoming/{fileName}";
        string loadedLocation = $"storage/plugins/loaded/{fileName}";

        StorageEntryMetadata loadedMeta = new(StorageProvider.Local, loadedLocation, "storage/plugins/loaded", fileName, assemblyBytes.Length, DateTimeOffset.UtcNow);
        StorageEntryMetadata incomingMeta = new(StorageProvider.Local, incomingLocation, "storage/plugins/incoming", fileName, assemblyBytes.Length, DateTimeOffset.UtcNow);

        _storageMock
            .Setup(s => s.ListFilesAsync("plugins", PluginStoragePartitions.Loaded, It.IsAny<CancellationToken>()))
            .Returns(() => CreateAsyncEnumerable([loadedMeta]));
        _storageMock
            .Setup(s => s.ListFilesAsync("plugins", PluginStoragePartitions.Incoming, It.IsAny<CancellationToken>()))
            .Returns(() => CreateAsyncEnumerable([incomingMeta]));
        _storageMock
            .Setup(s => s.ResolveLocation("plugins", PluginStoragePartitions.Incoming, fileName))
            .Returns(incomingLocation);
        _storageMock
            .Setup(s => s.ResolveLocation("plugins", PluginStoragePartitions.Loaded, fileName))
            .Returns(loadedLocation);
        _storageMock
            .Setup(s => s.OpenReadAsync(loadedLocation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream(assemblyBytes));
        _storageMock
            .Setup(s => s.OpenReadAsync(incomingLocation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream(assemblyBytes));
        _storageMock
            .Setup(s => s.OpenWriteAsync(incomingLocation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream());
        _storageMock
            .Setup(s => s.OpenWriteAsync(loadedLocation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream());
        _storageMock
            .Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _storageMock
            .Setup(s => s.ExistsAsync(loadedLocation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        DummyTypeProcessor processor = new();
        PluginService service = CreateService(processor);

        // Before loading into memory cache, GetPluginsAsync must NOT scan incoming/ or loaded/ on disk
        List<RegisteredPlugin> beforeLoad = await service.GetPluginsAsync().ToListAsync();
        Assert.Empty(beforeLoad);
        _storageMock.Verify(s => s.ListFilesAsync("plugins", PluginStoragePartitions.Incoming, It.IsAny<CancellationToken>()), Times.Never);

        ServiceCollection startupServices = new();
        await service.ReloadPluginsOnStartupAsync(startupServices, forceLoad: false);
        await service.ReloadPluginsOnRuntimeAsync(forceLoad: true);

        Assert.NotEmpty(processor.Processed);

        // After runtime load populates PluginCache, GetPluginsAsync returns the cached registered plugin cleanly
        List<RegisteredPlugin> afterLoad = await service.GetPluginsAsync().ToListAsync();
        Assert.Single(afterLoad);
        Assert.Equal("Mod", afterLoad[0].Name);
        Assert.True(afterLoad[0].IsFullyLoaded);
    }

    [Fact]
    public async Task InspectPluginsAsync_ScansMainPartitionsWithStorageEntryMetadataAndValidatesSafely()
    {
        byte[] assemblyBytes = File.ReadAllBytes(typeof(PluginService).Assembly.Location);
        byte[] corruptedBytes = [0x4D, 0x5A, 0x00, 0x01, 0x02];
        DateTimeOffset now = new(2026, 11, 26, 12, 0, 0, TimeSpan.Zero);

        StorageEntryMetadata loadedMeta = new(StorageProvider.Local, "storage/plugins/loaded/Mod.v1.0.0.dll", "storage/plugins/loaded", "Mod.v1.0.0.dll", assemblyBytes.Length, now);
        StorageEntryMetadata incomingMeta = new(StorageProvider.Local, "storage/plugins/incoming/Next.v2.0.0.dll", "storage/plugins/incoming", "Next.v2.0.0.dll", assemblyBytes.Length, now);
        StorageEntryMetadata pendingMeta = new(StorageProvider.Local, "storage/plugins/pending/Community.v1.0.0.dll", "storage/plugins/pending", "Community.v1.0.0.dll", assemblyBytes.Length, now);
        StorageEntryMetadata badBytesMeta = new(StorageProvider.Local, "storage/plugins/quarantine/Broken.v1.0.0.dll", "storage/plugins/quarantine", "Broken.v1.0.0.dll", corruptedBytes.Length, now);
        StorageEntryMetadata badNameMeta = new(StorageProvider.Local, "storage/plugins/quarantine/malformed.dll", "storage/plugins/quarantine", "malformed.dll", corruptedBytes.Length, now);

        _storageMock.Setup(s => s.ListFilesAsync("plugins", PluginStoragePartitions.Loaded, It.IsAny<CancellationToken>()))
            .Returns(() => CreateAsyncEnumerable([loadedMeta]));
        _storageMock.Setup(s => s.ListFilesAsync("plugins", PluginStoragePartitions.Incoming, It.IsAny<CancellationToken>()))
            .Returns(() => CreateAsyncEnumerable([incomingMeta]));
        _storageMock.Setup(s => s.ListFilesAsync("plugins", PluginStoragePartitions.Pending, It.IsAny<CancellationToken>()))
            .Returns(() => CreateAsyncEnumerable([pendingMeta]));
        _storageMock.Setup(s => s.ListFilesAsync("plugins", PluginStoragePartitions.Archive, It.IsAny<CancellationToken>()))
            .Returns(() => CreateAsyncEnumerable<StorageEntryMetadata>([]));
        _storageMock.Setup(s => s.ListFilesAsync("plugins", PluginStoragePartitions.Quarantine, It.IsAny<CancellationToken>()))
            .Returns(() => CreateAsyncEnumerable([badBytesMeta, badNameMeta]));
        _storageMock.Setup(s => s.ListFilesAsync("plugins", PluginStoragePartitions.Rejected, It.IsAny<CancellationToken>()))
            .Returns(() => CreateAsyncEnumerable<StorageEntryMetadata>([]));

        _storageMock.Setup(s => s.OpenReadAsync(loadedMeta.Location, It.IsAny<CancellationToken>())).ReturnsAsync(() => new MemoryStream(assemblyBytes));
        _storageMock.Setup(s => s.OpenReadAsync(incomingMeta.Location, It.IsAny<CancellationToken>())).ReturnsAsync(() => new MemoryStream(assemblyBytes));
        _storageMock.Setup(s => s.OpenReadAsync(pendingMeta.Location, It.IsAny<CancellationToken>())).ReturnsAsync(() => new MemoryStream(assemblyBytes));
        _storageMock.Setup(s => s.OpenReadAsync(badBytesMeta.Location, It.IsAny<CancellationToken>())).ReturnsAsync(() => new MemoryStream(corruptedBytes));

        DummyTypeProcessor processor = new();
        PluginService service = CreateService(processor);

        List<PluginInfo> all = await service.InspectPluginsAsync().ToListAsync();
        Assert.Equal(5, all.Count);

        PluginInfo loadedItem = all.Single(x => x.Partition == PluginStoragePartitions.Loaded);
        Assert.Equal("Mod", loadedItem.Name);
        Assert.Equal(new PluginVersion(1, 0, 0), loadedItem.Version);
        Assert.Equal(loadedMeta, loadedItem.File);
        Assert.True(loadedItem.IsValidAssembly);
        Assert.NotNull(loadedItem.TypesLoadedStatus);
        Assert.NotEmpty(loadedItem.TypesLoadedStatus);

        PluginInfo incomingItem = all.Single(x => x.Partition == PluginStoragePartitions.Incoming);
        Assert.Equal("Next", incomingItem.Name);
        Assert.Equal(incomingMeta, incomingItem.File);
        Assert.True(incomingItem.IsValidAssembly);
        Assert.NotNull(incomingItem.TypesLoadedStatus);
        Assert.NotEmpty(incomingItem.TypesLoadedStatus);

        PluginInfo pendingItem = all.Single(x => x.Partition == PluginStoragePartitions.Pending);
        Assert.Equal("Community", pendingItem.Name);
        Assert.Equal(pendingMeta, pendingItem.File);
        Assert.True(pendingItem.IsValidAssembly);
        Assert.NotNull(pendingItem.TypesLoadedStatus);
        Assert.Empty(pendingItem.TypesLoadedStatus);
        Assert.False(_cache.TryGet<LoadedPlugin>("Community", new PluginVersion(1, 0, 0), out _));

        List<PluginInfo> quarantined = [.. all.Where(x => x.Partition == PluginStoragePartitions.Quarantine)];
        Assert.Equal(2, quarantined.Count);
        Assert.All(quarantined, q =>
        {
            Assert.False(q.IsValidAssembly);
            Assert.False(string.IsNullOrWhiteSpace(q.ValidationError));
        });

        // Inspection must remain strictly read-only (never delete or move files)
        _storageMock.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _storageMock.Verify(s => s.MoveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);

        List<PluginInfo> pendingOnly = await service.InspectPluginsAsync(partition: PluginStoragePartitions.Pending).ToListAsync();
        Assert.Single(pendingOnly);
        Assert.Equal("Community", pendingOnly[0].Name);
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
