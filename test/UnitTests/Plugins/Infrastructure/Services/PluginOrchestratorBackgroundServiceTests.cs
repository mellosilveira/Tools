using MelloSilveiraTools.Core.Caching;
using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Plugins.Infrastructure;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Reflection;
using System.Runtime.Loader;

namespace UnitTests.Plugins.Infrastructure.Services;

public sealed class PluginOrchestratorBackgroundServiceTests
{
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock = new();
    private readonly Mock<IPluginService> _pluginServiceMock = new();
    private readonly Mock<IFileStorageService> _storageMock = new();
    private readonly PluginCache _cache = new(new InMemoryTwoLevelCache());
    private readonly PluginSettings _settings = new()
    {
        StorageProvider = StorageProvider.Local,
        Directory = "plugins",
        DefaultCacheTarget = "file",
        PollInterval = TimeSpan.FromMilliseconds(15),
        PreviousVersionRetention = TimeSpan.FromMinutes(30)
    };

    public PluginOrchestratorBackgroundServiceTests()
    {
        Mock<IServiceScope> scopeMock = new();
        Mock<IServiceProvider> serviceProviderMock = new();
        serviceProviderMock.Setup(sp => sp.GetService(typeof(IPluginService))).Returns(_pluginServiceMock.Object);
        scopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);
        _scopeFactoryMock.Setup(sf => sf.CreateScope()).Returns(scopeMock.Object);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private static MethodInfo GetInspectMethod() =>
        typeof(PluginOrchestratorBackgroundService).GetMethod("InspectAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;

    [Fact]
    public async Task StartAsync_And_StopAsync_RunPollLoopAndCancelGracefully()
    {
        _storageMock
            .Setup(s => s.ListFilesAsync("plugins", PluginStoragePartitions.Incoming, It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncEnumerable<StorageEntryMetadata>([]));

        PluginFileProcessor fileProcessor = new(NullLogger<PluginFileProcessor>.Instance, _storageMock.Object, _cache, _settings);
        PluginOrchestratorBackgroundService orchestrator = new(
            NullLogger<PluginOrchestratorBackgroundService>.Instance,
            _scopeFactoryMock.Object,
            TimeProvider.System,
            fileProcessor,
            _cache,
            _settings);

        await orchestrator.StartAsync(CancellationToken.None);
        await Task.Delay(50);
        await orchestrator.StopAsync(CancellationToken.None);

        _storageMock.Verify(
            s => s.ListFilesAsync("plugins", PluginStoragePartitions.Incoming, It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task InspectAsync_PromotesHighestVersionIgnoresLowerAndEvictsExpiredVersions()
    {
        DateTimeOffset now = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
        FixedTimeProvider timeProvider = new(now);
        byte[] assemblyBytes = File.ReadAllBytes(typeof(PluginService).Assembly.Location);

        StorageEntryMetadata v3Entry = new(StorageProvider.Local, "storage/plugins/incoming/Calc.v3.0.0.dll", "storage/plugins/incoming", "Calc.v3.0.0.dll", assemblyBytes.Length, now);
        StorageEntryMetadata v1IncomingEntry = new(StorageProvider.Local, "storage/plugins/incoming/Calc.v1.0.0.dll", "storage/plugins/incoming", "Calc.v1.0.0.dll", assemblyBytes.Length, now);
        StorageEntryMetadata v1LoadedEntry = new(StorageProvider.Local, "storage/plugins/loaded/Calc.v1.0.0.dll", "storage/plugins/loaded", "Calc.v1.0.0.dll", assemblyBytes.Length, now);

        _storageMock
            .Setup(s => s.ListFilesAsync("plugins", PluginStoragePartitions.Incoming, It.IsAny<CancellationToken>()))
            .Returns(() => CreateAsyncEnumerable([v3Entry, v1IncomingEntry]));
        _storageMock
            .Setup(s => s.ListFilesAsync("plugins", PluginStoragePartitions.Loaded, It.IsAny<CancellationToken>()))
            .Returns(() => CreateAsyncEnumerable([v1LoadedEntry]));
        _storageMock
            .Setup(s => s.OpenReadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream(assemblyBytes));
        _storageMock
            .Setup(s => s.ResolveLocation("plugins", PluginStoragePartitions.Archive, "Calc.v1.0.0.dll"))
            .Returns("storage/plugins/archive/Calc.v1.0.0.dll");
        _storageMock
            .Setup(s => s.OpenWriteAsync("storage/plugins/archive/Calc.v1.0.0.dll", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream());
        _storageMock
            .Setup(s => s.DeleteAsync("storage/plugins/loaded/Calc.v1.0.0.dll", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Pre-populate v1.0.0 (expired -> past 30m retention) and v2.0.0 (recent -> inside 30m retention)
        PluginVersion v1 = new(1, 0, 0);
        PluginVersion v2 = new(2, 0, 0);
        AssemblyLoadContext alc1 = new("Orc1", isCollectible: true);
        AssemblyLoadContext alc2 = new("Orc2", isCollectible: true);
        RegisteredPlugin regV1 = new(new LoadedPlugin(new DiscoveredPlugin("Calc", v1, "/p1", now), [typeof(string)], alc1));
        regV1.MarkTypeLoaded(typeof(string), now.AddHours(-2));
        RegisteredPlugin regV2 = new(new LoadedPlugin(new DiscoveredPlugin("Calc", v2, "/p2", now), [typeof(string)], alc2));
        regV2.MarkTypeLoaded(typeof(string), now.AddMinutes(-5));
        alc1.Unload();
        alc2.Unload();

        _cache.Update("Calc", v1, regV1);
        _cache.Update("Calc", v2, regV2);

        PluginFileProcessor fileProcessor = new(NullLogger<PluginFileProcessor>.Instance, _storageMock.Object, _cache, _settings);
        PluginOrchestratorBackgroundService orchestrator = new(
            NullLogger<PluginOrchestratorBackgroundService>.Instance,
            _scopeFactoryMock.Object,
            timeProvider,
            fileProcessor,
            _cache,
            _settings);

        await (Task)GetInspectMethod().Invoke(orchestrator, [CancellationToken.None])!;

        // Promotes v3, ignores incoming v1, evicts cached v1 (past retention), retains cached v2 (previousVersion within retention)
        _pluginServiceMock.Verify(s => s.LoadPluginsOnRuntimeAsync("Calc", new PluginVersion(3, 0, 0), It.IsAny<CancellationToken>()), Times.Once);
        _pluginServiceMock.Verify(s => s.LoadPluginsOnRuntimeAsync("Calc", v1, It.IsAny<CancellationToken>()), Times.Never);
        Assert.False(_cache.TryGet<RegisteredPlugin>("Calc", v1, out _));
        Assert.True(_cache.TryGet<RegisteredPlugin>("Calc", v2, out _));
    }

    [Fact]
    public async Task InspectAsync_HandlesAlreadyLoadedExactVersionLoadErrorsAndArchiveErrors()
    {
        DateTimeOffset now = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
        FixedTimeProvider timeProvider = new(now);
        byte[] assemblyBytes = File.ReadAllBytes(typeof(PluginService).Assembly.Location);

        StorageEntryMetadata v1Incoming = new(StorageProvider.Local, "storage/plugins/incoming/Same.v1.0.0.dll", "storage/plugins/incoming", "Same.v1.0.0.dll", assemblyBytes.Length, now);
        StorageEntryMetadata v2Incoming = new(StorageProvider.Local, "storage/plugins/incoming/Fail.v2.0.0.dll", "storage/plugins/incoming", "Fail.v2.0.0.dll", assemblyBytes.Length, now);
        StorageEntryMetadata v1Loaded = new(StorageProvider.Local, "storage/plugins/loaded/ArcErr.v1.0.0.dll", "storage/plugins/loaded", "ArcErr.v1.0.0.dll", assemblyBytes.Length, now);
        StorageEntryMetadata v2ArcIncoming = new(StorageProvider.Local, "storage/plugins/incoming/ArcErr.v2.0.0.dll", "storage/plugins/incoming", "ArcErr.v2.0.0.dll", assemblyBytes.Length, now);

        _storageMock
            .Setup(s => s.ListFilesAsync("plugins", PluginStoragePartitions.Incoming, It.IsAny<CancellationToken>()))
            .Returns(() => CreateAsyncEnumerable([v1Incoming, v2Incoming, v2ArcIncoming]));
        _storageMock
            .Setup(s => s.ListFilesAsync("plugins", PluginStoragePartitions.Loaded, It.IsAny<CancellationToken>()))
            .Returns(() => CreateAsyncEnumerable([v1Loaded]));
        _storageMock
            .Setup(s => s.OpenReadAsync("storage/plugins/incoming/Same.v1.0.0.dll", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream(assemblyBytes));
        _storageMock
            .Setup(s => s.OpenReadAsync("storage/plugins/incoming/Fail.v2.0.0.dll", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream(assemblyBytes));
        _storageMock
            .Setup(s => s.OpenReadAsync("storage/plugins/incoming/ArcErr.v2.0.0.dll", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream(assemblyBytes));
        _storageMock
            .Setup(s => s.ResolveLocation("plugins", PluginStoragePartitions.Archive, "ArcErr.v1.0.0.dll"))
            .Throws(new IOException("Archive unavailable"));

        PluginVersion v1 = new(1, 0, 0);
        AssemblyLoadContext alc = new("OrcErr", isCollectible: true);
        RegisteredPlugin regSameV1 = new(new LoadedPlugin(new DiscoveredPlugin("Same", v1, "/same", now), [typeof(string)], alc));
        regSameV1.MarkTypeLoaded(typeof(string), now);
        RegisteredPlugin regArcV1 = new(new LoadedPlugin(new DiscoveredPlugin("ArcErr", v1, "/arc1", now), [typeof(string)], alc));
        alc.Unload();

        _cache.Update("Same", v1, regSameV1);
        _cache.Update("ArcErr", v1, regArcV1);

        _pluginServiceMock
            .Setup(s => s.LoadPluginsOnRuntimeAsync("Fail", new PluginVersion(2, 0, 0), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Load boom"));

        PluginFileProcessor fileProcessor = new(NullLogger<PluginFileProcessor>.Instance, _storageMock.Object, _cache, _settings);
        PluginOrchestratorBackgroundService orchestrator = new(
            NullLogger<PluginOrchestratorBackgroundService>.Instance,
            _scopeFactoryMock.Object,
            timeProvider,
            fileProcessor,
            _cache,
            _settings);

        await (Task)GetInspectMethod().Invoke(orchestrator, [CancellationToken.None])!;

        _pluginServiceMock.Verify(s => s.LoadPluginsOnRuntimeAsync("Same", v1, It.IsAny<CancellationToken>()), Times.Never);
        _pluginServiceMock.Verify(s => s.LoadPluginsOnRuntimeAsync("Fail", new PluginVersion(2, 0, 0), It.IsAny<CancellationToken>()), Times.Once);
        _pluginServiceMock.Verify(s => s.LoadPluginsOnRuntimeAsync("ArcErr", new PluginVersion(2, 0, 0), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenInspectThrowsUnexpectedException_LogsErrorAndContinuesPolling()
    {
        int calls = 0;
        _storageMock
            .Setup(s => s.ListFilesAsync("plugins", PluginStoragePartitions.Incoming, It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                calls++;
                if (calls == 1)
                    throw new IOException("Transient storage error");

                return CreateAsyncEnumerable<StorageEntryMetadata>([]);
            });

        PluginFileProcessor fileProcessor = new(NullLogger<PluginFileProcessor>.Instance, _storageMock.Object, _cache, _settings);
        PluginOrchestratorBackgroundService orchestrator = new(
            NullLogger<PluginOrchestratorBackgroundService>.Instance,
            _scopeFactoryMock.Object,
            TimeProvider.System,
            fileProcessor,
            _cache,
            _settings);

        await orchestrator.StartAsync(CancellationToken.None);
        await Task.Delay(80);
        await orchestrator.StopAsync(CancellationToken.None);

        Assert.True(calls >= 2);
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
