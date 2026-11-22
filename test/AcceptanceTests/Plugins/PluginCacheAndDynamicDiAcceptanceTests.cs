using AcceptanceTests.Plugins.Helpers;
using MelloSilveiraTools.Core;
using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Files.Providers.Local;
using MelloSilveiraTools.Core.Providers;
using MelloSilveiraTools.Core.Providers.Dynamics;
using MelloSilveiraTools.Core.ResiliencePipelines;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Plugins;
using MelloSilveiraTools.Plugins.Infrastructure;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Persistences;
using MelloSilveiraTools.Plugins.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Polly;
using System.Collections.Concurrent;

namespace AcceptanceTests.Plugins;

/// <summary>
/// Acceptance tests validating runtime plugin registration into <see cref="IDynamicServiceProvider"/>
/// and resolution via <see cref="ServiceLocator"/>, plus round-trip cache persistence and restoration
/// against both physical JSON files (<c>plugin-cache.json</c>) and <see cref="IRepository"/> database routing.
/// </summary>
public sealed class PluginCacheAndDynamicDiAcceptanceTests : IAsyncLifetime
{
    private readonly string _tempBasePath = Path.Combine(AppContext.BaseDirectory, "_test-plugins-cache", Guid.NewGuid().ToString("N"));
    private readonly RuntimeDiPluginTypeProcessor _typeProcessor = new();
    private readonly ConcurrentDictionary<(string Name, string Version), PluginCacheEntity> _dbCacheStore = new();
    private readonly ServiceProvider _serviceProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes an isolated DI container backed by real <see cref="LocalFileStorageService"/>,
    /// <see cref="IDynamicServiceProvider"/>, <see cref="ServiceLocator"/>, and an in-memory
    /// <see cref="IRepository"/> spy for database cache persistence testing.
    /// </summary>
    public PluginCacheAndDynamicDiAcceptanceTests()
    {
        Directory.CreateDirectory(_tempBasePath);

        Mock<IRepository> repositoryMock = new();
        repositoryMock
            .Setup(r => r.UpsertAsync(It.IsAny<PluginCacheEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PluginCacheEntity entity, CancellationToken _) =>
            {
                _dbCacheStore[(entity.PluginName, entity.PluginVersion)] = entity;
                return 1L;
            });

        repositoryMock
            .Setup(r => r.GetAsync<PluginCacheEntity, PluginCacheFilter>(
                It.IsAny<PluginCacheFilter>(),
                It.IsAny<Pagination?>(),
                It.IsAny<CancellationToken>()))
            .Returns((PluginCacheFilter filter, Pagination? _, CancellationToken cancellationToken) =>
                StreamMatchingCacheEntitiesAsync(filter, cancellationToken));

        ServiceCollection services = new();
        services
            .AddCoreServices(
                new ResiliencePipelineSettings
                {
                    BackoffType = DelayBackoffType.Constant,
                    DelayInMilliseconds = 10,
                    MaxRetryAttempts = 1,
                    UseJitter = false
                },
                fileStorageSettings: new FileStorageSettings
                {
                    Local = new LocalFileStorageSettings
                    {
                        BaseDirectory = _tempBasePath
                    }
                })
            .AddHttpContextAccessor()
            .AddSingleton<IRepository>(repositoryMock.Object)
            .AddSingleton<IPluginTypeProcessor>(_typeProcessor)
            .AddPluginServices(new PluginSettings
            {
                StorageProvider = StorageProvider.Local,
                Directory = "plugins",
                PollInterval = TimeSpan.FromMinutes(5),
                PreviousVersionRetention = TimeSpan.FromMinutes(30),
                DefaultCacheTarget = PluginCacheTargets.File
            });

        _serviceProvider = services.BuildServiceProvider();
        _httpContextAccessor = _serviceProvider.GetRequiredService<IHttpContextAccessor>();
    }

    /// <summary>
    /// Verifies that loading a plugin at runtime registers its processable types in
    /// <see cref="IDynamicServiceProvider"/> and resolves singleton instances identically via
    /// both <see cref="IDynamicServiceProvider"/> and <see cref="ServiceLocator"/>.
    /// </summary>
    [Fact]
    public async Task LoadPluginsOnRuntime_ShouldRegisterPluginTypesInDynamicServiceProviderAndResolveViaServiceLocatorAsync()
    {
        // Arrange
        byte[] pluginBytes = PluginTestFixture.GetValidPluginBytes();
        const string pluginName = "DynamicDi.Plugin";
        PluginVersion version = new(1, 0, 0);
        await StagePluginInIncomingAsync($"{pluginName}.v1.0.0.dll", pluginBytes);

        using IServiceScope scope = _serviceProvider.CreateScope();
        IPluginService pluginService = scope.ServiceProvider.GetRequiredService<IPluginService>();
        IDynamicServiceProvider dynamicProvider = _serviceProvider.GetRequiredService<IDynamicServiceProvider>();
        ServiceLocator serviceLocator = _serviceProvider.GetRequiredService<ServiceLocator>();
        PluginCache pluginCache = _serviceProvider.GetRequiredService<PluginCache>();

        // Act
        await pluginService.LoadPluginsOnRuntimeAsync(pluginName, version);

        // Assert
        Assert.True(pluginCache.TryGet(pluginName, version, out RegisteredPlugin? registered));
        Assert.NotNull(registered);
        Assert.True(registered.IsFullyLoaded);
        Assert.NotEmpty(_typeProcessor.RegisteredTypes);

        foreach (Type pluginType in _typeProcessor.RegisteredTypes)
        {
            object? fromDynamic = dynamicProvider.GetService(pluginType);
            object? fromLocator = serviceLocator.GetService(pluginType);

            Assert.NotNull(fromDynamic);
            Assert.NotNull(fromLocator);
            Assert.Same(fromDynamic, fromLocator);
        }
    }

    /// <summary>
    /// Verifies that persisting the two-level <see cref="PluginCache"/> to the <c>file</c> target
    /// writes a real <c>plugin-cache.json</c> file on physical disk and accurately restores cached entries
    /// across multiple versions after <see cref="IPluginService.ClearAsync"/>.
    /// </summary>
    [Fact]
    public async Task PersistAndRestoreCache_ToFileTargetOnPhysicalDisk_ShouldWriteJsonAndRestoreAfterClearAsync()
    {
        // Arrange
        byte[] pluginBytes = PluginTestFixture.GetValidPluginBytes();
        const string pluginName = "FileCache.Plugin";
        PluginVersion v1 = new(1, 0, 0);
        PluginVersion v2 = new(2, 0, 0);

        await StagePluginInIncomingAsync($"{pluginName}.v1.0.0.dll", pluginBytes);
        await StagePluginInIncomingAsync($"{pluginName}.v2.0.0.dll", pluginBytes);

        _httpContextAccessor.HttpContext = null;

        using IServiceScope scope = _serviceProvider.CreateScope();
        IPluginService pluginService = scope.ServiceProvider.GetRequiredService<IPluginService>();
        PluginCache pluginCache = scope.ServiceProvider.GetRequiredService<PluginCache>();

        await pluginService.LoadPluginsOnRuntimeAsync(pluginName);

        // Act 1: Persist cache to real disk JSON file
        await pluginService.PersistCacheAsync(pluginName);

        // Assert 1: Physical plugin-cache.json exists on disk
        string cacheFilePath = Path.Combine(_tempBasePath, "plugins", PluginStoragePartitions.Cache, "plugin-cache.json");
        Assert.True(File.Exists(cacheFilePath), $"Expected cache file on physical disk at '{cacheFilePath}'.");
        string json = await File.ReadAllTextAsync(cacheFilePath);
        Assert.Contains(pluginName, json);
        Assert.Contains("v1.0.0", json);
        Assert.Contains("v2.0.0", json);

        // Act 2: Clear and restore from disk JSON file
        await pluginService.ClearAsync();
        Assert.False(pluginCache.TryGet(pluginName, v1, out RegisteredPlugin? _));
        Assert.False(pluginCache.TryGet(pluginName, v2, out RegisteredPlugin? _));

        await pluginService.RestoreCacheAsync(pluginName);

        // Assert 2: Both versions restored in PluginCache
        Assert.True(pluginCache.TryGet(pluginName, v1, out DiscoveredPlugin? restoredV1));
        Assert.True(pluginCache.TryGet(pluginName, v2, out DiscoveredPlugin? restoredV2));
        Assert.NotNull(restoredV1);
        Assert.NotNull(restoredV2);
        Assert.Equal(pluginName, restoredV1.Name);
        Assert.Equal(pluginName, restoredV2.Name);
        Assert.Equal(v1, restoredV1.Version);
        Assert.Equal(v2, restoredV2.Version);
    }

    /// <summary>
    /// Verifies that routing <c>{target} = "database"</c> via <see cref="HttpContext.Request"/>
    /// resolves <see cref="DatabasePluginCachePersistence"/>, upserts <see cref="PluginCacheEntity"/> rows
    /// via <see cref="IRepository"/>, clears the in-memory cache, and restores entries back from repository rows.
    /// </summary>
    [Fact]
    public async Task PersistAndRestoreCache_ToDatabaseTargetViaHttpContextRouteValue_ShouldUpsertAndRestoreFromRepositoryAsync()
    {
        // Arrange
        byte[] pluginBytes = PluginTestFixture.GetValidPluginBytes();
        const string pluginName = "DbCache.Plugin";
        PluginVersion v1 = new(1, 0, 0);
        PluginVersion v2 = new(2, 0, 0);

        await StagePluginInIncomingAsync($"{pluginName}.v1.0.0.dll", pluginBytes);
        await StagePluginInIncomingAsync($"{pluginName}.v2.0.0.dll", pluginBytes);

        DefaultHttpContext httpContext = new();
        httpContext.Request.RouteValues["target"] = PluginCacheTargets.Database;
        _httpContextAccessor.HttpContext = httpContext;

        using IServiceScope scope = _serviceProvider.CreateScope();
        IPluginService pluginService = scope.ServiceProvider.GetRequiredService<IPluginService>();
        PluginCache pluginCache = scope.ServiceProvider.GetRequiredService<PluginCache>();

        await pluginService.LoadPluginsOnRuntimeAsync(pluginName);

        // Act 1: Persist via scoped DatabasePluginCachePersistence
        await pluginService.PersistCacheAsync(pluginName);

        // Assert 1: Repository received both version rows
        Assert.True(_dbCacheStore.ContainsKey((pluginName, "v1.0.0")));
        Assert.True(_dbCacheStore.ContainsKey((pluginName, "v2.0.0")));

        // Act 2: Clear in-memory cache and restore from repository
        await pluginService.ClearAsync();
        Assert.False(pluginCache.TryGet(pluginName, v1, out DiscoveredPlugin? _));

        await pluginService.RestoreCacheAsync(pluginName);

        // Assert 2: Cache entries restored cleanly from DatabasePluginCachePersistence
        Assert.True(pluginCache.TryGet(pluginName, v1, out DiscoveredPlugin? restoredV1));
        Assert.True(pluginCache.TryGet(pluginName, v2, out DiscoveredPlugin? restoredV2));
        Assert.NotNull(restoredV1);
        Assert.NotNull(restoredV2);
        Assert.Equal(pluginName, restoredV1.Name);
        Assert.Equal(pluginName, restoredV2.Name);
        Assert.Equal(v1, restoredV1.Version);
        Assert.Equal(v2, restoredV2.Version);
    }

    /// <summary>
    /// No-op async initialization required by <see cref="IAsyncLifetime"/>.
    /// </summary>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <summary>
    /// Disposes the DI container and cleans up temporary plugin storage directories on disk.
    /// </summary>
    public async Task DisposeAsync()
    {
        _httpContextAccessor.HttpContext = null;
        using (IServiceScope cleanupScope = _serviceProvider.CreateScope())
        {
            cleanupScope.ServiceProvider.GetRequiredService<PluginFileProcessor>().CleanShadowCopies();
        }

        await _serviceProvider.DisposeAsync();

        if (Directory.Exists(_tempBasePath))
        {
            try
            {
                Directory.Delete(_tempBasePath, recursive: true);
            }
            catch
            {
                // Best-effort cleanup of temp directory
            }
        }
    }

    private async Task StagePluginInIncomingAsync(string fileName, byte[] content)
    {
        string incomingDir = Path.Combine(_tempBasePath, "plugins", PluginStoragePartitions.Incoming);
        Directory.CreateDirectory(incomingDir);
        await File.WriteAllBytesAsync(Path.Combine(incomingDir, fileName), content);
    }

    private async IAsyncEnumerable<PluginCacheEntity> StreamMatchingCacheEntitiesAsync(
        PluginCacheFilter filter,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();
        foreach (KeyValuePair<(string Name, string Version), PluginCacheEntity> pair in _dbCacheStore)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!string.IsNullOrWhiteSpace(filter.PluginName) && !string.Equals(pair.Value.PluginName, filter.PluginName, StringComparison.Ordinal))
                continue;

            if (!string.IsNullOrWhiteSpace(filter.PluginVersion) && !string.Equals(pair.Value.PluginVersion, filter.PluginVersion, StringComparison.Ordinal))
                continue;

            yield return pair.Value;
        }
    }

    private sealed class RuntimeDiPluginTypeProcessor : IPluginTypeProcessor
    {
        public Type ProcessableType => typeof(object);

        public List<Type> RegisteredTypes { get; } = [];

        public void Process(Type type, PluginRegistrationContext context)
        {
            if (type.ContainsGenericParameters || type.GetConstructor(Type.EmptyTypes) is null)
                return;

            RegisteredTypes.Add(type);
            if (context.IsStartup)
                context.Services!.AddSingleton(type);
            else
                context.DynamicProvider!.Add(type, type);
        }
    }
}
