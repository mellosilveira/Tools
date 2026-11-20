using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Plugins.Application.Commands.Get;
using MelloSilveiraTools.Plugins.Application.Validators;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Services;
using Moq;
using System.Runtime.Loader;

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

        ListedResult<RegisteredPlugin> result = await command.ExecuteAsync(request);

        Assert.False(result.Success);
        Assert.Equal(StatusCode.BadRequest, result.StatusCode);
    }

    [Theory]
    [InlineData(null, 2)]
    [InlineData(true, 1)]
    [InlineData(false, 1)]
    public async Task ExecuteAsync_FiltersByFullyLoadedFlag(bool? fullyLoadedFilter, int expectedCount)
    {
        AssemblyLoadContext alc = new("GetPluginsTestAlc", isCollectible: true);
        RegisteredPlugin loaded = new(new LoadedPlugin(new DiscoveredPlugin("Plugin1", new PluginVersion(1, 0, 0), "/p1", DateTimeOffset.UtcNow), [typeof(string)], alc));
        loaded.MarkTypeLoaded(typeof(string), DateTimeOffset.UtcNow);
        RegisteredPlugin pending = new(new LoadedPlugin(new DiscoveredPlugin("Plugin2", new PluginVersion(1, 0, 0), "/p2", DateTimeOffset.UtcNow), [typeof(string)], alc));
        alc.Unload();

        _serviceMock
            .Setup(s => s.GetPluginsAsync(It.IsAny<string?>(), It.IsAny<PluginVersion?>(), It.IsAny<CancellationToken>()))
            .Returns(() => CreateAsyncEnumerable([loaded, pending]));

        GetPlugins command = new(_serviceMock.Object, _validator);
        GetPluginsRequest request = new() { Name = "Plugin", Version = "v1.0.0", FullyLoaded = fullyLoadedFilter };

        ListedResult<RegisteredPlugin> result = await command.ExecuteAsync(request);

        Assert.True(result.Success);
        Assert.Equal(StatusCode.OK, result.StatusCode);
        Assert.NotNull(result.Data);
        Assert.Equal(expectedCount, result.Data.Count);
    }
}
