using AcceptanceTests.Plugins.Helpers;
using MelloSilveiraTools.Core;
using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Files.Providers.Local;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.ResiliencePipelines;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Plugins;
using MelloSilveiraTools.Plugins.Application.Commands.Upload;
using MelloSilveiraTools.Plugins.Infrastructure;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Persistences;
using MelloSilveiraTools.Plugins.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Polly;

namespace AcceptanceTests.Plugins;

/// <summary>
/// Physical-disk acceptance tests for plugin assembly upload, startup/runtime loading,
/// partition movement across <c>incoming</c>, <c>loaded</c>, and <c>quarantine</c> using real
/// <see cref="LocalFileStorageService"/> on Windows disk (without storage mocks).
/// </summary>
public sealed class PluginPhysicalDiskLifecycleAcceptanceTests : IAsyncLifetime
{
    private readonly string _tempBasePath = Path.Combine(AppContext.BaseDirectory, "_test-plugins-disk", Guid.NewGuid().ToString("N"));
    private readonly TestPluginTypeProcessor _typeProcessor = new();
    private readonly ServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes an isolated DI container backed by a unique temporary local storage directory
    /// and real <see cref="LocalFileStorageService"/> registrations.
    /// </summary>
    public PluginPhysicalDiskLifecycleAcceptanceTests()
    {
        Directory.CreateDirectory(_tempBasePath);

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
            .AddSingleton(Mock.Of<IRepository>())
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
    }

    /// <summary>
    /// Uploads a valid managed .NET assembly via <see cref="UploadPlugin"/> to physical disk,
    /// invokes <see cref="IPluginService.LoadPluginsOnStartupAsync"/>, and verifies that the DLL
    /// physically moves from <c>incoming</c> to <c>loaded</c>, enters <see cref="PluginCache"/>,
    /// and registers its types in the startup <see cref="IServiceCollection"/>.
    /// </summary>
    [Fact]
    public async Task UploadAndLoadPluginsOnStartup_OnPhysicalDisk_ShouldWriteToIncomingMoveToLoadedAndRegisterInServicesAsync()
    {
        // Arrange
        byte[] assemblyBytes = PluginTestFixture.GetValidPluginBytes();
        string fileName = "DiskStartup.Plugin.v1.0.0.dll";
        string incomingFilePath = Path.Combine(_tempBasePath, "plugins", PluginStoragePartitions.Incoming, fileName);
        string loadedFilePath = Path.Combine(_tempBasePath, "plugins", PluginStoragePartitions.Loaded, fileName);

        using IServiceScope scope = _serviceProvider.CreateScope();
        UploadPlugin uploadCommand = scope.ServiceProvider.GetRequiredService<UploadPlugin>();
        IPluginService pluginService = scope.ServiceProvider.GetRequiredService<IPluginService>();
        PluginCache pluginCache = scope.ServiceProvider.GetRequiredService<PluginCache>();

        FormFile formFile = new(new MemoryStream(assemblyBytes), 0, assemblyBytes.Length, "file", fileName);
        UploadPluginRequest uploadRequest = new() { File = formFile };

        // Act 1: Upload to physical disk incoming partition
        Result<StorageEntryMetadata> uploadResult = await uploadCommand.ExecuteAsync(uploadRequest);

        // Assert 1: File written physically into incoming/
        Assert.True(uploadResult.Success, uploadResult.JoinedMessages);
        Assert.NotNull(uploadResult.Data);
        Assert.True(File.Exists(incomingFilePath), $"Expected uploaded plugin DLL on disk at '{incomingFilePath}'.");
        Assert.Equal(assemblyBytes.Length, new FileInfo(incomingFilePath).Length);

        // Act 2: Startup discovery & registration into startup IServiceCollection
        ServiceCollection startupServices = [];
        await pluginService.LoadPluginsOnStartupAsync(startupServices, "DiskStartup.Plugin", new PluginVersion(1, 0, 0));

        // Assert 2: File moved from incoming/ to loaded/, cached, and registered in startupServices
        Assert.False(File.Exists(incomingFilePath), $"Plugin DLL should have been removed from '{incomingFilePath}'.");
        Assert.True(File.Exists(loadedFilePath), $"Expected loaded plugin DLL on disk at '{loadedFilePath}'.");

        bool inCache = pluginCache.TryGet("DiskStartup.Plugin", new PluginVersion(1, 0, 0), out RegisteredPlugin? registered);
        Assert.True(inCache);
        Assert.NotNull(registered);
        Assert.True(registered.IsFullyLoaded);
        Assert.NotEmpty(_typeProcessor.StartupProcessedTypes);

        Type registeredType = _typeProcessor.StartupProcessedTypes[0];
        Assert.Contains(startupServices, descriptor => descriptor.ServiceType == registeredType);

        await using ServiceProvider startupProvider = startupServices.BuildServiceProvider();
        object? resolvedInstance = startupProvider.GetService(registeredType);
        Assert.NotNull(resolvedInstance);
    }

    /// <summary>
    /// Verifies physical disk partition transitions during runtime load and reload operations:
    /// loading v1 and v2 moves both DLLs to <c>loaded</c>, <c>forceLoad: false</c> moves v1 back to
    /// <c>incoming</c> while leaving v2 in <c>loaded</c>, and <c>forceLoad: true</c> reloads v2
    /// back into <c>loaded</c>.
    /// </summary>
    [Fact]
    public async Task LoadAndReloadPluginsOnRuntime_WithForceLoadFalseAndTrue_ShouldMovePhysicalDllBetweenIncomingAndLoadedAsync()
    {
        // Arrange: Stage two semantic versions physically in incoming/
        byte[] pluginBytes = PluginTestFixture.GetValidPluginBytes();
        const string pluginName = "DiskRuntime.Mod";
        string v1FileName = $"{pluginName}.v1.0.0.dll";
        string v2FileName = $"{pluginName}.v2.0.0.dll";

        string incomingDir = Path.Combine(_tempBasePath, "plugins", PluginStoragePartitions.Incoming);
        string loadedDir = Path.Combine(_tempBasePath, "plugins", PluginStoragePartitions.Loaded);
        Directory.CreateDirectory(incomingDir);

        string v1IncomingPath = Path.Combine(incomingDir, v1FileName);
        string v2IncomingPath = Path.Combine(incomingDir, v2FileName);
        string v1LoadedPath = Path.Combine(loadedDir, v1FileName);
        string v2LoadedPath = Path.Combine(loadedDir, v2FileName);

        await File.WriteAllBytesAsync(v1IncomingPath, pluginBytes);
        await File.WriteAllBytesAsync(v2IncomingPath, pluginBytes);

        using IServiceScope scope = _serviceProvider.CreateScope();
        IPluginService pluginService = scope.ServiceProvider.GetRequiredService<IPluginService>();

        // Act 1: Runtime load
        await pluginService.LoadPluginsOnRuntimeAsync(pluginName);

        // Assert 1: Both moved physically to loaded/ and returned by GetPluginsAsync
        Assert.False(File.Exists(v1IncomingPath));
        Assert.False(File.Exists(v2IncomingPath));
        Assert.True(File.Exists(v1LoadedPath));
        Assert.True(File.Exists(v2LoadedPath));

        List<RegisteredPlugin> discoveredPlugins = [];
        await foreach (RegisteredPlugin plugin in pluginService.GetPluginsAsync(pluginName))
        {
            discoveredPlugins.Add(plugin);
        }

        Assert.Equal(2, discoveredPlugins.Count);
        Assert.Contains(discoveredPlugins, p => p.Name == pluginName && p.Version == new PluginVersion(1, 0, 0));
        Assert.Contains(discoveredPlugins, p => p.Name == pluginName && p.Version == new PluginVersion(2, 0, 0));

        // Act 2: Reload v1.0.0 with forceLoad: false -> returns physically to incoming/
        await pluginService.ReloadPluginsOnRuntimeAsync(forceLoad: false, pluginName, new PluginVersion(1, 0, 0));

        // Assert 2: v1 back in incoming/, v2 still in loaded/
        Assert.True(File.Exists(v1IncomingPath));
        Assert.False(File.Exists(v1LoadedPath));
        Assert.True(File.Exists(v2LoadedPath));

        // Act 3: Reload v2.0.0 with forceLoad: true -> moved to incoming/ and immediately promoted back to loaded/
        await pluginService.ReloadPluginsOnRuntimeAsync(forceLoad: true, pluginName, new PluginVersion(2, 0, 0));

        // Assert 3: v2 is back in loaded/ and absent from incoming/
        Assert.False(File.Exists(v2IncomingPath));
        Assert.True(File.Exists(v2LoadedPath));
    }

    /// <summary>
    /// Verifies physical disk security and quarantine behavior:
    /// <see cref="UploadPlugin"/> rejects non-managed binaries before touching disk, whereas a corrupted
    /// binary placed directly inside <c>incoming</c> is quarantined to <c>quarantine</c> during scanning.
    /// </summary>
    [Fact]
    public async Task UploadAndScan_WithCorruptedOrInvalidDllOnPhysicalDisk_ShouldRejectUploadAndQuarantineStagedBinaryAsync()
    {
        // Arrange
        byte[] corruptedBytes = PluginTestFixture.GetCorruptedPluginBytes();
        const string corruptedFileName = "Corrupt.Module.v1.0.0.dll";
        string incomingDir = Path.Combine(_tempBasePath, "plugins", PluginStoragePartitions.Incoming);
        string quarantineDir = Path.Combine(_tempBasePath, "plugins", PluginStoragePartitions.Quarantine);
        string incomingPath = Path.Combine(incomingDir, corruptedFileName);
        string quarantinePath = Path.Combine(quarantineDir, corruptedFileName);

        using IServiceScope scope = _serviceProvider.CreateScope();
        UploadPlugin uploadCommand = scope.ServiceProvider.GetRequiredService<UploadPlugin>();
        PluginFileProcessor fileProcessor = scope.ServiceProvider.GetRequiredService<PluginFileProcessor>();

        FormFile corruptedFormFile = new(new MemoryStream(corruptedBytes), 0, corruptedBytes.Length, "file", corruptedFileName);

        // Act 1: UploadPlugin rejects corrupted PE binary
        Result<StorageEntryMetadata> uploadResult = await uploadCommand.ExecuteAsync(new UploadPluginRequest { File = corruptedFormFile });

        // Assert 1: Rejected and not written to disk
        Assert.False(uploadResult.Success);
        Assert.Equal(StatusCode.BadRequest, uploadResult.StatusCode);
        Assert.False(File.Exists(incomingPath));

        // Act 2: Write corrupted binary directly into incoming/ and scan via PluginFileProcessor
        Directory.CreateDirectory(incomingDir);
        await File.WriteAllBytesAsync(incomingPath, corruptedBytes);

        List<DiscoveredPlugin> discovered = [];
        await foreach (DiscoveredPlugin item in fileProcessor.ScanAsync())
        {
            discovered.Add(item);
        }

        // Assert 2: Quarantined physically on disk
        Assert.Empty(discovered);
        Assert.False(File.Exists(incomingPath));
        Assert.True(File.Exists(quarantinePath), $"Expected quarantined binary on disk at '{quarantinePath}'.");
    }

    /// <summary>
    /// No-op async initialization required by <see cref="IAsyncLifetime"/>.
    /// </summary>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <summary>
    /// Disposes the DI container, cleans plugin shadow copies, and deletes temporary plugin storage directories on disk.
    /// </summary>
    public async Task DisposeAsync()
    {
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

    private sealed class TestPluginTypeProcessor : IPluginTypeProcessor
    {
        public Type ProcessableType => typeof(object);

        public List<Type> StartupProcessedTypes { get; } = [];

        public List<Type> RuntimeProcessedTypes { get; } = [];

        public void Process(Type type, PluginRegistrationContext context)
        {
            if (type.ContainsGenericParameters || type.GetConstructor(Type.EmptyTypes) is null)
                return;

            if (context.IsStartup)
            {
                StartupProcessedTypes.Add(type);
                context.Services!.AddSingleton(type);
            }
            else
            {
                RuntimeProcessedTypes.Add(type);
                context.DynamicProvider!.Add(type, type);
            }
        }
    }
}
