using AcceptanceTests.Plugins.Helpers;
using MelloSilveiraTools.Plugins.Infrastructure;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using Microsoft.Extensions.DependencyInjection;

namespace AcceptanceTests.Plugins;

/// <summary>
/// Acceptance tests verifying quarantine isolation: files in the incoming storage partition
/// that do not conform to naming conventions or lack valid managed .NET metadata must be
/// automatically moved to the quarantine partition without crashing or halting valid processing.
/// </summary>
public sealed class PluginQuarantineAcceptanceTests
{
    /// <summary>
    /// Verifies that an assembly in incoming storage lacking the mandatory versioning pattern
    /// is isolated into the quarantine partition and removed from incoming.
    /// </summary>
    [Fact]
    public async Task ScanAsync_WithMalformedFileNameInIncoming_ShouldMoveToQuarantinePartition()
    {
        // Arrange
        await using PluginTestFixture fixture = new();
        byte[] validBytes = PluginTestFixture.GetValidPluginBytes();
        string malformedFileName = "UnversionedPlugin.dll";

        fixture.LocalStorage.AddFile("plugins", PluginStoragePartitions.Incoming, malformedFileName, validBytes);

        using IServiceScope scope = fixture.CreateScope();
        PluginFileProcessor fileProcessor = scope.ServiceProvider.GetRequiredService<PluginFileProcessor>();

        // Act
        List<DiscoveredPlugin> discovered = [];
        await foreach (DiscoveredPlugin plugin in fileProcessor.ScanAsync())
        {
            discovered.Add(plugin);
        }

        // Assert
        Assert.Empty(discovered);

        bool inIncoming = fixture.LocalStorage.FileExists("plugins", PluginStoragePartitions.Incoming, malformedFileName);
        bool inQuarantine = fixture.LocalStorage.FileExists("plugins", PluginStoragePartitions.Quarantine, malformedFileName);

        Assert.False(inIncoming);
        Assert.True(inQuarantine);
    }

    /// <summary>
    /// Verifies that a binary file having a valid naming convention but containing corrupted
    /// non-managed PE headers is moved to the quarantine partition.
    /// </summary>
    [Fact]
    public async Task ScanAsync_WithCorruptedNonManagedBinaryInIncoming_ShouldMoveToQuarantinePartition()
    {
        // Arrange
        await using PluginTestFixture fixture = new();
        byte[] corruptedBytes = PluginTestFixture.GetCorruptedPluginBytes();
        string corruptedFileName = "BrokenModule.v1.0.0.dll";

        fixture.LocalStorage.AddFile("plugins", PluginStoragePartitions.Incoming, corruptedFileName, corruptedBytes);

        using IServiceScope scope = fixture.CreateScope();
        PluginFileProcessor fileProcessor = scope.ServiceProvider.GetRequiredService<PluginFileProcessor>();

        // Act
        List<DiscoveredPlugin> discovered = [];
        await foreach (DiscoveredPlugin plugin in fileProcessor.ScanAsync())
        {
            discovered.Add(plugin);
        }

        // Assert
        Assert.Empty(discovered);

        bool inIncoming = fixture.LocalStorage.FileExists("plugins", PluginStoragePartitions.Incoming, corruptedFileName);
        bool inQuarantine = fixture.LocalStorage.FileExists("plugins", PluginStoragePartitions.Quarantine, corruptedFileName);

        Assert.False(inIncoming);
        Assert.True(inQuarantine);
    }

    /// <summary>
    /// Verifies that when valid and invalid candidate files coexist in the incoming storage partition,
    /// invalid candidates are quarantined while valid assemblies are successfully discovered and returned.
    /// </summary>
    [Fact]
    public async Task ScanAsync_WithMixedCandidates_ShouldQuarantineInvalidAndDiscoverValid()
    {
        // Arrange
        await using PluginTestFixture fixture = new();
        byte[] validBytes = PluginTestFixture.GetValidPluginBytes();
        byte[] corruptedBytes = PluginTestFixture.GetNonManagedPluginBytes();

        string validFileName = "ConformingModule.v2.4.1.dll";
        string invalidName = "MalformedName.dll";
        string unmanagedName = "NativeDll.v1.0.0.dll";

        fixture.LocalStorage.AddFile("plugins", PluginStoragePartitions.Incoming, validFileName, validBytes);
        fixture.LocalStorage.AddFile("plugins", PluginStoragePartitions.Incoming, invalidName, validBytes);
        fixture.LocalStorage.AddFile("plugins", PluginStoragePartitions.Incoming, unmanagedName, corruptedBytes);

        using IServiceScope scope = fixture.CreateScope();
        PluginFileProcessor fileProcessor = scope.ServiceProvider.GetRequiredService<PluginFileProcessor>();

        // Act
        List<DiscoveredPlugin> discovered = [];
        await foreach (DiscoveredPlugin plugin in fileProcessor.ScanAsync())
        {
            discovered.Add(plugin);
        }

        // Assert
        Assert.Single(discovered);
        Assert.Equal("ConformingModule", discovered[0].Name);
        Assert.Equal(new PluginVersion(2, 4, 1), discovered[0].Version);

        Assert.True(fixture.LocalStorage.FileExists("plugins", PluginStoragePartitions.Quarantine, invalidName));
        Assert.True(fixture.LocalStorage.FileExists("plugins", PluginStoragePartitions.Quarantine, unmanagedName));

        Assert.False(fixture.LocalStorage.FileExists("plugins", PluginStoragePartitions.Incoming, invalidName));
        Assert.False(fixture.LocalStorage.FileExists("plugins", PluginStoragePartitions.Incoming, unmanagedName));
        Assert.True(fixture.LocalStorage.FileExists("plugins", PluginStoragePartitions.Incoming, validFileName));
    }
}
