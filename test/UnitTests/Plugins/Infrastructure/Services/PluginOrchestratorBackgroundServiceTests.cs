using MelloSilveiraTools.Core.Caching;
using MelloSilveiraTools.Core.Managers.File.Models;
using MelloSilveiraTools.Core.Managers.File.Providers;
using MelloSilveiraTools.Plugins.Infrastructure;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Reflection;

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
        PollInterval = TimeSpan.FromMilliseconds(50),
        PreviousVersionRetention = TimeSpan.FromSeconds(1)
    };

    public PluginOrchestratorBackgroundServiceTests()
    {
        Mock<IServiceScope> scopeMock = new();
        Mock<IServiceProvider> serviceProviderMock = new();

        serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IPluginService)))
            .Returns(_pluginServiceMock.Object);

        scopeMock
            .Setup(s => s.ServiceProvider)
            .Returns(serviceProviderMock.Object);

        _scopeFactoryMock
            .Setup(sf => sf.CreateScope())
            .Returns(scopeMock.Object);
    }

    private static async IAsyncEnumerable<T> CreateAsyncEnumerable<T>(IEnumerable<T> items)
    {
        foreach (T item in items)
        {
            yield return item;
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        PluginFileProcessor fileProcessor = new(NullLogger<PluginFileProcessor>.Instance, _storageMock.Object, _cache, _settings);

        PluginOrchestratorBackgroundService orchestrator = new(
            NullLogger<PluginOrchestratorBackgroundService>.Instance,
            _scopeFactoryMock.Object,
            fileProcessor,
            _cache,
            _settings,
            TimeProvider.System);

        Assert.NotNull(orchestrator);
    }

    [Fact]
    public async Task InspectAsync_WhenNewerVersionFound_LoadsNewVersion()
    {
        // Arrange
        byte[] assemblyBytes = File.ReadAllBytes(typeof(PluginService).Assembly.Location);
        string fileName = "Calculators.v2.0.0.dll";
        string incomingPath = $"storage/plugins/incoming/{fileName}";

        StorageEntryMetadata entry = new(
            StorageProvider.Local,
            incomingPath,
            "storage/plugins/incoming",
            fileName,
            assemblyBytes.Length,
            DateTimeOffset.UtcNow);

        _storageMock
            .Setup(s => s.ListFilesAsync("plugins", PluginStoragePartitions.Incoming, It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncEnumerable([entry]));

        _storageMock
            .Setup(s => s.OpenReadAsync(incomingPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream(assemblyBytes));

        PluginFileProcessor fileProcessor = new(NullLogger<PluginFileProcessor>.Instance, _storageMock.Object, _cache, _settings);

        PluginOrchestratorBackgroundService orchestrator = new(
            NullLogger<PluginOrchestratorBackgroundService>.Instance,
            _scopeFactoryMock.Object,
            fileProcessor,
            _cache,
            _settings,
            TimeProvider.System);

        MethodInfo? inspectMethod = typeof(PluginOrchestratorBackgroundService)
            .GetMethod("InspectAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(inspectMethod);

        // Act
        Task inspectTask = (Task)inspectMethod.Invoke(orchestrator, [CancellationToken.None])!;
        await inspectTask;

        // Assert
        _pluginServiceMock.Verify(
            s => s.LoadPluginsOnRuntimeAsync("Calculators", new PluginVersion(2, 0, 0), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InspectAsync_WhenLowerVersionThanAlreadyLoaded_IgnoresLowerFile()
    {
        // Arrange
        byte[] assemblyBytes = File.ReadAllBytes(typeof(PluginService).Assembly.Location);
        string fileName = "Calculators.v1.0.0.dll";
        string incomingPath = $"storage/plugins/incoming/{fileName}";

        StorageEntryMetadata entry = new(
            StorageProvider.Local,
            incomingPath,
            "storage/plugins/incoming",
            fileName,
            assemblyBytes.Length,
            DateTimeOffset.UtcNow);

        _storageMock
            .Setup(s => s.ListFilesAsync("plugins", PluginStoragePartitions.Incoming, It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncEnumerable([entry]));

        _storageMock
            .Setup(s => s.OpenReadAsync(incomingPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream(assemblyBytes));

        // Pre-populate cache with v2.0.0 (fully loaded)
        PluginVersion v2 = new(2, 0, 0);
        RegisteredPlugin registeredV2 = new(new LoadedPlugin(new DiscoveredPlugin("Calculators", v2, "/p2", DateTimeOffset.UtcNow), [typeof(string)], null!));
        registeredV2.MarkTypeLoaded(typeof(string), TimeProvider.System);
        _cache.Update("Calculators", v2, registeredV2);

        PluginFileProcessor fileProcessor = new(NullLogger<PluginFileProcessor>.Instance, _storageMock.Object, _cache, _settings);

        PluginOrchestratorBackgroundService orchestrator = new(
            NullLogger<PluginOrchestratorBackgroundService>.Instance,
            _scopeFactoryMock.Object,
            fileProcessor,
            _cache,
            _settings,
            TimeProvider.System);

        MethodInfo? inspectMethod = typeof(PluginOrchestratorBackgroundService)
            .GetMethod("InspectAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(inspectMethod);

        // Act
        Task inspectTask = (Task)inspectMethod.Invoke(orchestrator, [CancellationToken.None])!;
        await inspectTask;

        // Assert - v1.0.0 should NOT be loaded because v2.0.0 is already loaded
        _pluginServiceMock.Verify(
            s => s.LoadPluginsOnRuntimeAsync("Calculators", new PluginVersion(1, 0, 0), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
