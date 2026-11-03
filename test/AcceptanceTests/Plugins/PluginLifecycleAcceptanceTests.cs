using AcceptanceTests.Plugins.Helpers;
using MelloSilveiraTools.Core.Caching;
using MelloSilveiraTools.Core.Managers.File.Models;
using MelloSilveiraTools.Core.Managers.File.Providers;
using MelloSilveiraTools.Core.Providers.Dynamics;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Plugins;
using MelloSilveiraTools.Plugins.Application.Commands.Upload;
using MelloSilveiraTools.Plugins.Infrastructure;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Persistences;
using MelloSilveiraTools.Plugins.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace AcceptanceTests.Plugins;

/// <summary>
/// Acceptance tests for plugin discovery and loading lifecycles, verifying that assemblies
/// are inspected, processable types are wired into static or dynamic DI containers, and
/// files transition safely from incoming to loaded storage partitions.
/// </summary>
public sealed class PluginLifecycleAcceptanceTests
{
    /// <summary>
    /// Verifies that executing the startup DI bootstrapping extension <see cref="PluginsDependencyInjection.AddPluginServicesAsync"/>
    /// scans incoming files, executes type processing into the static container, and promotes the assembly to the loaded partition.
    /// </summary>
    [Fact]
    public async Task AddPluginServicesAsync_WithIncomingPlugin_ShouldPromoteToLoadedAndRegisterTypes()
    {
        // Arrange
        PluginTestStorageMock localStorage = new(StorageProvider.Local);
        TestPluginTypeProcessor typeProcessor = new();

        byte[] assemblyBytes = PluginTestFixture.GetValidPluginBytes();
        string fileName = "Contoso.Feature.v1.0.0.dll";
        localStorage.AddFile("plugins", PluginStoragePartitions.Incoming, fileName, assemblyBytes);

        PluginSettings settings = new()
        {
            StorageProvider = StorageProvider.Local,
            Directory = "plugins",
            DefaultCacheTarget = PluginCacheTargets.File,
            PollInterval = TimeSpan.FromMilliseconds(50),
            PreviousVersionRetention = TimeSpan.FromHours(24)
        };

        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton<ITwoLevelCache, InMemoryTwoLevelCache>();
        services.AddSingleton<IDynamicServiceProvider, InMemoryDynamicServiceProvider>();
        services.AddSingleton<IPluginTypeProcessor>(typeProcessor);
        services.AddKeyedSingleton<IFileStorageService>(StorageProvider.Local, localStorage.Mock.Object);
        services.AddSingleton(Mock.Of<IRepository>());

        // Act
        await services.AddPluginServicesAsync(settings);

        // Assert
        bool fileInIncoming = localStorage.FileExists("plugins", PluginStoragePartitions.Incoming, fileName);
        bool fileInLoaded = localStorage.FileExists("plugins", PluginStoragePartitions.Loaded, fileName);

        Assert.False(fileInIncoming);
        Assert.True(fileInLoaded);

        Assert.Contains(typeProcessor.ProcessedTypes, t => t.Name == nameof(UploadPlugin));
    }

    /// <summary>
    /// Verifies that <see cref="IPluginService.LoadPluginsOnStartupAsync"/> reads an incoming plugin assembly,
    /// marks it as fully loaded in cache, and moves the physical file from incoming to loaded partition.
    /// </summary>
    [Fact]
    public async Task LoadPluginsOnStartupAsync_WithDiscoveredIncomingAssembly_ShouldPromoteToLoadedFolder()
    {
        // Arrange
        await using PluginTestFixture fixture = new();
        byte[] assemblyBytes = PluginTestFixture.GetValidPluginBytes();
        string fileName = "AnalyticsEngine.v1.2.3.dll";
        fixture.LocalStorage.AddFile("plugins", PluginStoragePartitions.Incoming, fileName, assemblyBytes);

        using IServiceScope scope = fixture.CreateScope();
        IPluginService pluginService = scope.ServiceProvider.GetRequiredService<IPluginService>();

        // Act
        await pluginService.LoadPluginsOnStartupAsync(fixture.Services);

        // Assert
        bool existsInIncoming = fixture.LocalStorage.FileExists("plugins", PluginStoragePartitions.Incoming, fileName);
        bool existsInLoaded = fixture.LocalStorage.FileExists("plugins", PluginStoragePartitions.Loaded, fileName);

        Assert.False(existsInIncoming);
        Assert.True(existsInLoaded);

        List<RegisteredPlugin> loadedPlugins = [];
        await foreach (RegisteredPlugin plugin in pluginService.GetPluginsAsync())
        {
            loadedPlugins.Add(plugin);
        }

        Assert.Single(loadedPlugins);
        Assert.Equal("AnalyticsEngine", loadedPlugins[0].Name);
        Assert.Equal(new PluginVersion(1, 2, 3), loadedPlugins[0].Version);
        Assert.True(loadedPlugins[0].IsFullyLoaded);
        Assert.NotNull(loadedPlugins[0].FullyLoadedAt);
    }

    /// <summary>
    /// Verifies that loading a newer plugin version at runtime registers the exported types into
    /// <see cref="IDynamicServiceProvider"/> and moves the new assembly into the loaded partition.
    /// </summary>
    [Fact]
    public async Task LoadPluginsOnRuntimeAsync_WithNewerVersion_ShouldPromoteToLoadedAndRegisterInDynamicProvider()
    {
        // Arrange
        await using PluginTestFixture fixture = new();
        byte[] assemblyBytes = PluginTestFixture.GetValidPluginBytes();

        string v1FileName = "DataProcessor.v1.0.0.dll";
        string v2FileName = "DataProcessor.v2.0.0.dll";

        fixture.LocalStorage.AddFile("plugins", PluginStoragePartitions.Loaded, v1FileName, assemblyBytes);
        fixture.LocalStorage.AddFile("plugins", PluginStoragePartitions.Incoming, v2FileName, assemblyBytes);

        using IServiceScope scope = fixture.CreateScope();
        IPluginService pluginService = scope.ServiceProvider.GetRequiredService<IPluginService>();
        IDynamicServiceProvider dynamicProvider = scope.ServiceProvider.GetRequiredService<IDynamicServiceProvider>();

        // Act
        await pluginService.LoadPluginsOnRuntimeAsync("DataProcessor", new PluginVersion(2, 0, 0));

        // Assert
        bool v2InIncoming = fixture.LocalStorage.FileExists("plugins", PluginStoragePartitions.Incoming, v2FileName);
        bool v2InLoaded = fixture.LocalStorage.FileExists("plugins", PluginStoragePartitions.Loaded, v2FileName);

        Assert.False(v2InIncoming);
        Assert.True(v2InLoaded);

        string? resolvedService = dynamicProvider.GetKeyedService<string>(typeof(UploadPlugin).FullName!);
        Assert.NotNull(resolvedService);
        Assert.Equal(nameof(UploadPlugin), resolvedService);
    }

    /// <summary>
    /// Verifies that calling <see cref="IPluginService.ReloadPluginsOnStartupAsync"/> on an already loaded plugin
    /// re-reads the assembly from storage and ensures type registrations are re-evaluated.
    /// </summary>
    [Fact]
    public async Task ReloadPluginsOnStartupAsync_WithLoadedPlugin_ShouldMoveBackToIncomingAndReload()
    {
        // Arrange
        await using PluginTestFixture fixture = new();
        byte[] assemblyBytes = PluginTestFixture.GetValidPluginBytes();
        string fileName = "ReusableModule.v1.0.0.dll";

        fixture.LocalStorage.AddFile("plugins", PluginStoragePartitions.Loaded, fileName, assemblyBytes);

        using IServiceScope scope = fixture.CreateScope();
        IPluginService pluginService = scope.ServiceProvider.GetRequiredService<IPluginService>();

        // Act
        await pluginService.ReloadPluginsOnStartupAsync(fixture.Services, forceLoad: true);

        // Assert
        bool fileInLoaded = fixture.LocalStorage.FileExists("plugins", PluginStoragePartitions.Loaded, fileName);
        Assert.True(fileInLoaded);

        Assert.Contains(fixture.TypeProcessor.ProcessedTypes, t => t.Name == nameof(UploadPlugin));
    }
}
