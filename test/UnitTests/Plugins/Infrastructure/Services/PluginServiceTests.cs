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
        PreviousVersionRetention = TimeSpan.FromHours(24)
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
        PluginAssemblyProcessor assemblyProcessor = new(processors, TimeProvider.System, fileProcessor, _cache);
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
        _storageMock.Verify(s => s.DeleteAsync(incomingLocation, It.IsAny<CancellationToken>()), Times.Once);

        // Calling startup load when already cached & fully loaded still completes without throwing
        ServiceCollection services = new();
        await service.LoadPluginsOnStartupAsync(services);
        _storageMock.Verify(s => s.DeleteAsync(incomingLocation, It.IsAny<CancellationToken>()), Times.Exactly(2));
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

        List<RegisteredPlugin> listed = await service.GetPluginsAsync().ToListAsync();
        Assert.Equal(2, listed.Count);

        ServiceCollection startupServices = new();
        await service.ReloadPluginsOnStartupAsync(startupServices, forceLoad: false);
        await service.ReloadPluginsOnRuntimeAsync(forceLoad: true);

        Assert.NotEmpty(processor.Processed);
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
