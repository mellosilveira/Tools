using AcceptanceTests.Plugins.Helpers;
using MelloSilveiraTools.Plugins.Infrastructure;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection;

namespace AcceptanceTests.Plugins;

/// <summary>
/// Acceptance tests for <see cref="PluginOrchestratorBackgroundService"/>, verifying automated
/// runtime inspection, newer version promotion, and archival of superseded versions past retention.
/// </summary>
public sealed class PluginOrchestratorAcceptanceTests
{
    private static async Task InvokeInspectPassAsync(PluginOrchestratorBackgroundService orchestrator, CancellationToken cancellationToken = default)
    {
        MethodInfo? inspectMethod = typeof(PluginOrchestratorBackgroundService)
            .GetMethod("InspectAsync", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(inspectMethod);
        Task inspectTask = (Task)inspectMethod.Invoke(orchestrator, [cancellationToken])!;
        await inspectTask.ConfigureAwait(false);
    }

    /// <summary>
    /// Verifies that when a newer version arrives in the incoming partition and the previous version's
    /// retention period has elapsed, the orchestrator promotes the new version to loaded and archives the old version.
    /// </summary>
    [Fact]
    public async Task Orchestrator_WhenNewVersionArrivesAndRetentionElapsed_ShouldPromoteAndArchiveOldVersion()
    {
        // Arrange: retention configured to zero so obsolete version is immediately archived
        await using PluginTestFixture fixture = new(retention: TimeSpan.Zero);
        byte[] validBytes = PluginTestFixture.GetValidPluginBytes();

        string v1FileName = "Calculators.v1.0.0.dll";
        string v2FileName = "Calculators.v2.0.0.dll";

        // Pre-load v1.0.0 through startup lifecycle so it resides in loaded partition and cache
        fixture.LocalStorage.AddFile("plugins", PluginStoragePartitions.Incoming, v1FileName, validBytes);
        using (IServiceScope initScope = fixture.CreateScope())
        {
            IPluginService pluginService = initScope.ServiceProvider.GetRequiredService<IPluginService>();
            await pluginService.LoadPluginsOnStartupAsync(fixture.Services);
        }

        Assert.True(fixture.LocalStorage.FileExists("plugins", PluginStoragePartitions.Loaded, v1FileName));

        // Place v2.0.0 in incoming
        fixture.LocalStorage.AddFile("plugins", PluginStoragePartitions.Incoming, v2FileName, validBytes);

        using (IServiceScope scope = fixture.CreateScope())
        {
            IServiceScopeFactory scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
            PluginFileProcessor fileProcessor = scope.ServiceProvider.GetRequiredService<PluginFileProcessor>();
            PluginCache cache = scope.ServiceProvider.GetRequiredService<PluginCache>();

            PluginOrchestratorBackgroundService orchestrator = new(
                NullLogger<PluginOrchestratorBackgroundService>.Instance,
                scopeFactory,
                fileProcessor,
                cache,
                fixture.Settings);

            // Act: execute an orchestrator inspection pass
            await InvokeInspectPassAsync(orchestrator);

            // Assert: v2 promoted to loaded, v1 archived
            bool v2InLoaded = fixture.LocalStorage.FileExists("plugins", PluginStoragePartitions.Loaded, v2FileName);
            bool v2InIncoming = fixture.LocalStorage.FileExists("plugins", PluginStoragePartitions.Incoming, v2FileName);
            bool v1InLoaded = fixture.LocalStorage.FileExists("plugins", PluginStoragePartitions.Loaded, v1FileName);
            bool v1InArchive = fixture.LocalStorage.FileExists("plugins", PluginStoragePartitions.Archive, v1FileName);

            Assert.True(v2InLoaded);
            Assert.False(v2InIncoming);
            Assert.False(v1InLoaded);
            Assert.True(v1InArchive);

            // Verify cache state: v1 cleared, v2 active
            Assert.False(cache.TryGet<RegisteredPlugin>("Calculators", new PluginVersion(1, 0, 0), out RegisteredPlugin? _));
            Assert.True(cache.TryGet<RegisteredPlugin>("Calculators", new PluginVersion(2, 0, 0), out RegisteredPlugin? _));
        }
    }

    /// <summary>
    /// Verifies that when a newer version arrives and the retention period for the previous version
    /// has not yet elapsed, the orchestrator promotes the new version without evicting the previous one yet.
    /// </summary>
    [Fact]
    public async Task Orchestrator_WhenNewVersionArrivesAndRetentionActive_ShouldPromoteAndRetainPreviousVersion()
    {
        // Arrange: retention configured to 24 hours
        await using PluginTestFixture fixture = new(retention: TimeSpan.FromHours(24));
        byte[] validBytes = PluginTestFixture.GetValidPluginBytes();

        string v1FileName = "Engine.v1.0.0.dll";
        string v2FileName = "Engine.v2.0.0.dll";

        // Pre-load v1.0.0
        fixture.LocalStorage.AddFile("plugins", PluginStoragePartitions.Incoming, v1FileName, validBytes);
        using (IServiceScope initScope = fixture.CreateScope())
        {
            IPluginService pluginService = initScope.ServiceProvider.GetRequiredService<IPluginService>();
            await pluginService.LoadPluginsOnStartupAsync(fixture.Services);
        }

        // Place v2.0.0 in incoming
        fixture.LocalStorage.AddFile("plugins", PluginStoragePartitions.Incoming, v2FileName, validBytes);

        using (IServiceScope scope = fixture.CreateScope())
        {
            IServiceScopeFactory scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
            PluginFileProcessor fileProcessor = scope.ServiceProvider.GetRequiredService<PluginFileProcessor>();
            PluginCache cache = scope.ServiceProvider.GetRequiredService<PluginCache>();

            PluginOrchestratorBackgroundService orchestrator = new(
                NullLogger<PluginOrchestratorBackgroundService>.Instance,
                scopeFactory,
                fileProcessor,
                cache,
                fixture.Settings);

            // Act
            await InvokeInspectPassAsync(orchestrator);

            // Assert: v2 is loaded, but v1 is kept in loaded (not archived yet due to active retention)
            bool v2InLoaded = fixture.LocalStorage.FileExists("plugins", PluginStoragePartitions.Loaded, v2FileName);
            bool v1InLoaded = fixture.LocalStorage.FileExists("plugins", PluginStoragePartitions.Loaded, v1FileName);
            bool v1InArchive = fixture.LocalStorage.FileExists("plugins", PluginStoragePartitions.Archive, v1FileName);

            Assert.True(v2InLoaded);
            Assert.True(v1InLoaded);
            Assert.False(v1InArchive);
        }
    }

    /// <summary>
    /// Verifies that if an older plugin version file is placed in incoming while a higher version
    /// is already loaded, the orchestrator ignores the older file without modifying loaded state.
    /// </summary>
    [Fact]
    public async Task Orchestrator_WhenOlderVersionArrivesInIncoming_ShouldIgnoreFile()
    {
        // Arrange
        await using PluginTestFixture fixture = new();
        byte[] validBytes = PluginTestFixture.GetValidPluginBytes();

        string v2FileName = "CoreModule.v2.0.0.dll";
        string v1FileName = "CoreModule.v1.0.0.dll";

        // Pre-load v2.0.0
        fixture.LocalStorage.AddFile("plugins", PluginStoragePartitions.Incoming, v2FileName, validBytes);
        using (IServiceScope initScope = fixture.CreateScope())
        {
            IPluginService pluginService = initScope.ServiceProvider.GetRequiredService<IPluginService>();
            await pluginService.LoadPluginsOnStartupAsync(fixture.Services);
        }

        // Drop older v1.0.0 into incoming
        fixture.LocalStorage.AddFile("plugins", PluginStoragePartitions.Incoming, v1FileName, validBytes);

        using (IServiceScope scope = fixture.CreateScope())
        {
            IServiceScopeFactory scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
            PluginFileProcessor fileProcessor = scope.ServiceProvider.GetRequiredService<PluginFileProcessor>();
            PluginCache cache = scope.ServiceProvider.GetRequiredService<PluginCache>();

            PluginOrchestratorBackgroundService orchestrator = new(
                NullLogger<PluginOrchestratorBackgroundService>.Instance,
                scopeFactory,
                fileProcessor,
                cache,
                fixture.Settings);

            // Act
            await InvokeInspectPassAsync(orchestrator);

            // Assert: v1 is not loaded into cache
            Assert.False(cache.TryGet<RegisteredPlugin>("CoreModule", new PluginVersion(1, 0, 0), out RegisteredPlugin? _));
            Assert.True(cache.TryGet<RegisteredPlugin>("CoreModule", new PluginVersion(2, 0, 0), out RegisteredPlugin? _));
        }
    }
}
