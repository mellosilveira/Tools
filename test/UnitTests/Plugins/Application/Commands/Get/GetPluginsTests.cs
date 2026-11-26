using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Plugins.Application.Commands.Get;
using MelloSilveiraTools.Plugins.Application.Validators;
using MelloSilveiraTools.Plugins.Infrastructure;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Services;
using Moq;
using System.Runtime.Loader;
using System.Text.Json;

namespace UnitTests.Plugins.Application.Commands.Get;

public sealed class GetPluginsTests
{
    private readonly Mock<IPluginService> _serviceMock = new();
    private readonly PluginValidator<GetPluginsRequest> _validator = new();

    private static async IAsyncEnumerable<RegisteredPlugin> CreateAsyncEnumerable(IEnumerable<RegisteredPlugin> items)
    {
        foreach (RegisteredPlugin item in items)
        {
            await Task.Yield();
            yield return item;
        }
    }

    [Fact]
    public async Task ExecuteAsync_WhenVersionIsInvalid_ReturnsBadRequest()
    {
        GetPlugins command = new(_serviceMock.Object, _validator);
        GetPluginsRequest request = new() { Name = "Contoso", Version = "bad-version" };

        ListedResult<PluginInfo> result = await command.ExecuteAsync(request);

        Assert.False(result.Success);
        Assert.Equal(StatusCode.BadRequest, result.StatusCode);
    }

    [Theory]
    [InlineData(null, 2)]
    [InlineData(true, 1)]
    [InlineData(false, 1)]
    public async Task ExecuteAsync_FiltersByFullyLoadedFlag_AndOmitsInternalFilePaths(bool? fullyLoadedFilter, int expectedCount)
    {
        AssemblyLoadContext alc = new("GetPluginsTestAlc", isCollectible: true);
        RegisteredPlugin loaded = new(new LoadedPlugin(new DiscoveredPlugin("Plugin1", new PluginVersion(1, 0, 0), "/secret/server/path/p1.dll", DateTimeOffset.UtcNow), [typeof(string)], alc));
        loaded.MarkTypeLoaded(typeof(string), DateTimeOffset.UtcNow);
        RegisteredPlugin pending = new(new LoadedPlugin(new DiscoveredPlugin("Plugin2", new PluginVersion(1, 0, 0), "/secret/server/path/p2.dll", DateTimeOffset.UtcNow), [typeof(string)], alc));
        alc.Unload();

        _serviceMock
            .Setup(s => s.GetPluginsAsync(It.IsAny<string?>(), It.IsAny<PluginVersion?>(), It.IsAny<CancellationToken>()))
            .Returns(() => CreateAsyncEnumerable([loaded, pending]));

        GetPlugins command = new(_serviceMock.Object, _validator);
        GetPluginsRequest request = new() { Name = "Plugin", Version = "v1.0.0", FullyLoaded = fullyLoadedFilter };

        ListedResult<PluginInfo> result = await command.ExecuteAsync(request);

        Assert.True(result.Success);
        Assert.Equal(StatusCode.OK, result.StatusCode);
        Assert.NotNull(result.Data);
        Assert.Equal(expectedCount, result.Data.Count);

        string json = JsonSerializer.Serialize(result.Data);
        Assert.DoesNotContain("/secret/server/path", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("FullPath", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"File\"", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InspectPlugins_WhenVersionOrPartitionIsInvalid_ReturnsBadRequest()
    {
        InspectPlugins command = new(_serviceMock.Object, new PluginValidator<InspectPluginsRequest>());

        ListedResult<PluginInfo> badVersion = await command.ExecuteAsync(new InspectPluginsRequest { Version = "not-semver" });
        Assert.False(badVersion.Success);
        Assert.Equal(StatusCode.BadRequest, badVersion.StatusCode);

        ListedResult<PluginInfo> badPartition = await command.ExecuteAsync(new InspectPluginsRequest { Partition = "../etc" });
        Assert.False(badPartition.Success);
        Assert.Equal(StatusCode.BadRequest, badPartition.StatusCode);
    }

    [Fact]
    public async Task InspectPlugins_PreservesStorageEntryMetadataAcrossPartitions()
    {
        DateTimeOffset now = new(2026, 11, 26, 12, 0, 0, TimeSpan.Zero);
        StorageEntryMetadata loadedFile = new(StorageProvider.Local, "/storage/plugins/loaded/P1.v1.0.0.dll", "/storage/plugins/loaded", "P1.v1.0.0.dll", 4096, now);
        StorageEntryMetadata pendingFile = new(StorageProvider.Local, "/storage/plugins/pending/P2.v1.0.0.dll", "/storage/plugins/pending", "P2.v1.0.0.dll", 2048, now);

        PluginInfo loadedItem = new()
        {
            Partition = PluginStoragePartitions.Loaded,
            Name = "P1",
            Version = new PluginVersion(1, 0, 0),
            File = loadedFile,
            IsValidAssembly = true,
            IsFullyLoaded = true,
            FullyLoadedAt = now,
            TypesLoadedStatus = new Dictionary<string, bool> { ["System.String"] = true }
        };

        PluginInfo pendingItem = new()
        {
            Partition = PluginStoragePartitions.Pending,
            Name = "P2",
            Version = new PluginVersion(1, 0, 0),
            File = pendingFile,
            IsValidAssembly = true,
            IsFullyLoaded = false,
            TypesLoadedStatus = new Dictionary<string, bool>()
        };

        _serviceMock
            .Setup(s => s.InspectPluginsAsync(It.IsAny<string?>(), It.IsAny<PluginVersion?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(() => CreateInspectedAsyncEnumerable([loadedItem, pendingItem]));

        InspectPlugins command = new(_serviceMock.Object, new PluginValidator<InspectPluginsRequest>());
        ListedResult<PluginInfo> result = await command.ExecuteAsync(new InspectPluginsRequest());

        Assert.True(result.Success);
        Assert.Equal(StatusCode.OK, result.StatusCode);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Count);

        string json = JsonSerializer.Serialize(result.Data);
        Assert.Contains("StorageProvider", json, StringComparison.Ordinal);
        Assert.Contains("SizeBytes", json, StringComparison.Ordinal);
    }

    private static async IAsyncEnumerable<PluginInfo> CreateInspectedAsyncEnumerable(IEnumerable<PluginInfo> items)
    {
        foreach (PluginInfo item in items)
        {
            await Task.Yield();
            yield return item;
        }
    }
}
