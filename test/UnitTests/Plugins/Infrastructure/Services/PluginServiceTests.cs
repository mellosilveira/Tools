//using MelloSilveiraTools.Core.Caching;
//using MelloSilveiraTools.Core.Managers.File.Models;
//using MelloSilveiraTools.Core.Managers.File.Providers;
//using MelloSilveiraTools.Core.Providers.Dynamics;
//using MelloSilveiraTools.Plugins.Infrastructure;
//using MelloSilveiraTools.Plugins.Infrastructure.Models;
//using MelloSilveiraTools.Plugins.Infrastructure.Persistences;
//using MelloSilveiraTools.Plugins.Infrastructure.Services;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Logging.Abstractions;
//using Moq;

//namespace UnitTests.Plugins.Infrastructure.Services;

//public sealed class PluginServiceTests
//{
//    private readonly Mock<IPluginCachePersistence> _persistenceMock = new();
//    private readonly Mock<IDynamicServiceProvider> _dynamicProviderMock = new();
//    private readonly Mock<IFileStorageService> _storageMock = new();
//    private readonly PluginCache _cache = new(new InMemoryTwoLevelCache());
//    private readonly PluginSettings _settings = new()
//    {
//        StorageProvider = StorageProvider.Local,
//        Directory = "plugins",
//        DefaultCacheTarget = "file",
//        PollInterval = TimeSpan.FromSeconds(30),
//        PreviousVersionRetention = TimeSpan.FromHours(24)
//    };

//    private sealed class DummyTypeProcessor : IPluginTypeProcessor
//    {
//        public Type ProcessableType => typeof(object);
//        public List<Type> Processed { get; } = [];
//        public void Process(Type type, PluginRegistrationContext context)
//        {
//            Processed.Add(type);
//        }
//    }

//    private static async IAsyncEnumerable<T> CreateAsyncEnumerable<T>(IEnumerable<T> items)
//    {
//        foreach (T item in items)
//        {
//            yield return item;
//        }

//        await Task.CompletedTask.ConfigureAwait(false);
//    }

//    [Fact]
//    public async Task ClearAsync_DelegatesToCache()
//    {
//        PluginService service = new(
//            _persistenceMock.Object,
//            _dynamicProviderMock.Object,
//            new PluginFileProcessor(NullLogger<PluginFileProcessor>.Instance, _storageMock.Object, _cache, _settings),
//            new PluginAssemblyProcessor([], _storageMock.Object, _cache, TimeProvider.System),
//            _cache);

//        PluginVersion version = new(1, 0, 0);
//        _cache.Update("PluginA", version, new DiscoveredPlugin("PluginA", version, "/pA", DateTimeOffset.UtcNow));

//        await service.ClearAsync();

//        Assert.False(_cache.TryGet<DiscoveredPlugin>("PluginA", version, out _));
//    }

//    [Fact]
//    public async Task PersistCacheAsync_InvokesPersistenceSaveAsync()
//    {
//        _persistenceMock
//            .Setup(p => p.SaveAsync(It.IsAny<IAsyncEnumerable<PluginCacheEntry>>(), It.IsAny<CancellationToken>()))
//            .Returns(Task.CompletedTask);

//        PluginService service = new(
//            _persistenceMock.Object,
//            _dynamicProviderMock.Object,
//            new PluginFileProcessor(NullLogger<PluginFileProcessor>.Instance, _storageMock.Object, _cache, _settings),
//            new PluginAssemblyProcessor([], _storageMock.Object, _cache, TimeProvider.System),
//            _cache);

//        await service.PersistCacheAsync("PluginA", new PluginVersion(1, 0, 0));

//        _persistenceMock.Verify(p => p.SaveAsync(It.IsAny<IAsyncEnumerable<PluginCacheEntry>>(), It.IsAny<CancellationToken>()), Times.Once);
//    }

//    [Fact]
//    public async Task RestoreCacheAsync_StreamsFromPersistenceAndUpdatesCache()
//    {
//        DiscoveredPlugin plugin = new("PluginB", new PluginVersion(1, 0, 0), "/pB", DateTimeOffset.UtcNow);
//        PluginCacheEntry entry = new("PluginB", "v1.0.0", plugin);

//        _persistenceMock
//            .Setup(p => p.LoadAsync("PluginB", new PluginVersion(1, 0, 0), It.IsAny<CancellationToken>()))
//            .Returns(CreateAsyncEnumerable([entry]));

//        PluginService service = new(
//            _persistenceMock.Object,
//            _dynamicProviderMock.Object,
//            new PluginFileProcessor(NullLogger<PluginFileProcessor>.Instance, _storageMock.Object, _cache, _settings),
//            new PluginAssemblyProcessor([], _storageMock.Object, _cache, TimeProvider.System),
//            _cache);

//        await service.RestoreCacheAsync("PluginB", new PluginVersion(1, 0, 0));

//        Assert.True(_cache.TryGet<DiscoveredPlugin>("PluginB", new PluginVersion(1, 0, 0), out DiscoveredPlugin? cached));
//        Assert.Equal("PluginB", cached?.Name);
//    }

//    [Fact]
//    public async Task LoadPluginsOnRuntimeAsync_LoadsAssemblyRegistersTypesAndMovesToLoaded()
//    {
//        byte[] assemblyBytes = File.ReadAllBytes(typeof(PluginService).Assembly.Location);
//        string fileName = "Plugin.v1.0.0.dll";
//        string incomingLocation = $"storage/plugins/incoming/{fileName}";
//        string loadedLocation = $"storage/plugins/loaded/{fileName}";

//        StorageEntryMetadata incomingMetadata = new(
//            StorageProvider.Local,
//            incomingLocation,
//            "storage/plugins/incoming",
//            fileName,
//            assemblyBytes.Length,
//            DateTimeOffset.UtcNow);

//        _storageMock
//            .Setup(s => s.ListFilesAsync("plugins", PluginStoragePartitions.Incoming, It.IsAny<CancellationToken>()))
//            .Returns(CreateAsyncEnumerable([incomingMetadata]));

//        _storageMock
//            .Setup(s => s.OpenReadAsync(incomingLocation, It.IsAny<CancellationToken>()))
//            .ReturnsAsync(() => new MemoryStream(assemblyBytes));

//        _storageMock
//            .Setup(s => s.ResolveLocation("plugins", PluginStoragePartitions.Loaded, fileName))
//            .Returns(loadedLocation);

//        _storageMock
//            .Setup(s => s.ExistsAsync(loadedLocation, It.IsAny<CancellationToken>()))
//            .ReturnsAsync(false);

//        MemoryStream destinationStream = new();
//        _storageMock
//            .Setup(s => s.OpenWriteAsync(loadedLocation, It.IsAny<CancellationToken>()))
//            .ReturnsAsync(destinationStream);

//        _storageMock
//            .Setup(s => s.DeleteAsync(incomingLocation, It.IsAny<CancellationToken>()))
//            .ReturnsAsync(true);

//        DummyTypeProcessor dummyProcessor = new();

//        PluginFileProcessor fileProcessor = new(NullLogger<PluginFileProcessor>.Instance, _storageMock.Object, _cache, _settings);
//        PluginAssemblyProcessor assemblyProcessor = new([dummyProcessor], _storageMock.Object, _cache, TimeProvider.System);

//        PluginService service = new(
//            _persistenceMock.Object,
//            _dynamicProviderMock.Object,
//            fileProcessor,
//            assemblyProcessor,
//            _cache);

//        // Act
//        await service.LoadPluginsOnRuntimeAsync();

//        // Assert
//        Assert.NotEmpty(dummyProcessor.Processed);
//        _storageMock.Verify(s => s.DeleteAsync(incomingLocation, It.IsAny<CancellationToken>()), Times.Once);
//    }

//    [Fact]
//    public async Task LoadPluginsOnStartupAsync_RegistersTypesUsingServiceCollection()
//    {
//        byte[] assemblyBytes = File.ReadAllBytes(typeof(PluginService).Assembly.Location);
//        string fileName = "PluginStartup.v1.0.0.dll";
//        string incomingLocation = $"storage/plugins/incoming/{fileName}";
//        string loadedLocation = $"storage/plugins/loaded/{fileName}";

//        StorageEntryMetadata incomingMetadata = new(
//            StorageProvider.Local,
//            incomingLocation,
//            "storage/plugins/incoming",
//            fileName,
//            assemblyBytes.Length,
//            DateTimeOffset.UtcNow);

//        _storageMock
//            .Setup(s => s.ListFilesAsync("plugins", PluginStoragePartitions.Incoming, It.IsAny<CancellationToken>()))
//            .Returns(CreateAsyncEnumerable([incomingMetadata]));

//        _storageMock
//            .Setup(s => s.OpenReadAsync(incomingLocation, It.IsAny<CancellationToken>()))
//            .ReturnsAsync(() => new MemoryStream(assemblyBytes));

//        _storageMock
//            .Setup(s => s.ResolveLocation("plugins", PluginStoragePartitions.Loaded, fileName))
//            .Returns(loadedLocation);

//        _storageMock
//            .Setup(s => s.ExistsAsync(loadedLocation, It.IsAny<CancellationToken>()))
//            .ReturnsAsync(false);

//        MemoryStream destinationStream = new();
//        _storageMock
//            .Setup(s => s.OpenWriteAsync(loadedLocation, It.IsAny<CancellationToken>()))
//            .ReturnsAsync(destinationStream);

//        _storageMock
//            .Setup(s => s.DeleteAsync(incomingLocation, It.IsAny<CancellationToken>()))
//            .ReturnsAsync(true);

//        DummyTypeProcessor dummyProcessor = new();

//        PluginFileProcessor fileProcessor = new(NullLogger<PluginFileProcessor>.Instance, _storageMock.Object, _cache, _settings);
//        PluginAssemblyProcessor assemblyProcessor = new([dummyProcessor], _storageMock.Object, _cache, TimeProvider.System);

//        PluginService service = new(
//            _persistenceMock.Object,
//            _dynamicProviderMock.Object,
//            fileProcessor,
//            assemblyProcessor,
//            _cache);

//        ServiceCollection services = new();

//        // Act
//        await service.LoadPluginsOnStartupAsync(services);

//        // Assert
//        Assert.NotEmpty(dummyProcessor.Processed);
//        _storageMock.Verify(s => s.DeleteAsync(incomingLocation, It.IsAny<CancellationToken>()), Times.Once);
//    }
//}
