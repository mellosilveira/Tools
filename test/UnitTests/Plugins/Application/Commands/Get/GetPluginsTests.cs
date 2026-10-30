//using MelloSilveiraTools.Core.Models;
//using MelloSilveiraTools.Plugins.Application.Commands.Get;
//using MelloSilveiraTools.Plugins.Application.Validators;
//using MelloSilveiraTools.Plugins.Infrastructure.Models;
//using MelloSilveiraTools.Plugins.Infrastructure.Services;
//using Moq;

//namespace UnitTests.Plugins.Application.Commands.Get;

//public sealed class GetPluginsTests
//{
//    private readonly Mock<IPluginService> _serviceMock = new();
//    private readonly PluginValidator<GetPluginsRequest> _validator = new();

//    private static async IAsyncEnumerable<RegisteredPlugin> CreateAsyncEnumerable(IEnumerable<RegisteredPlugin> items)
//    {
//        foreach (RegisteredPlugin item in items)
//        {
//            yield return item;
//        }

//        await Task.CompletedTask.ConfigureAwait(false);
//    }

//    [Fact]
//    public async Task ExecuteAsync_WhenVersionIsInvalid_ReturnsBadRequest()
//    {
//        GetPlugins command = new(_serviceMock.Object, _validator);
//        GetPluginsRequest request = new() { Name = "Contoso", Version = "bad-version" };

//        ListedResult<RegisteredPlugin> result = await command.ExecuteAsync(request);

//        Assert.False(result.Success);
//        Assert.Equal(StatusCode.BadRequest, result.StatusCode);
//    }

//    [Fact]
//    public async Task ExecuteAsync_WhenFullyLoadedIsNull_ReturnsAllPlugins()
//    {
//        RegisteredPlugin p1 = new(new LoadedPlugin(new DiscoveredPlugin("Plugin1", new PluginVersion(1, 0, 0), "/p1", DateTimeOffset.UtcNow), [typeof(string)], null!));
//        p1.MarkTypeLoaded(typeof(string), TimeProvider.System); // FullyLoaded = true

//        RegisteredPlugin p2 = new(new LoadedPlugin(new DiscoveredPlugin("Plugin2", new PluginVersion(1, 0, 0), "/p2", DateTimeOffset.UtcNow), [typeof(string)], null!)); // FullyLoaded = false

//        _serviceMock
//            .Setup(s => s.GetPluginsAsync("Plugin", new PluginVersion(1, 0, 0), It.IsAny<CancellationToken>()))
//            .Returns(CreateAsyncEnumerable([p1, p2]));

//        GetPlugins command = new(_serviceMock.Object, _validator);
//        GetPluginsRequest request = new() { Name = "Plugin", Version = "v1.0.0", FullyLoaded = null };

//        ListedResult<RegisteredPlugin> result = await command.ExecuteAsync(request);

//        Assert.True(result.Success);
//        Assert.Equal(StatusCode.OK, result.StatusCode);
//        Assert.NotNull(result.Data);
//        Assert.Equal(2, result.Data.Count);
//    }

//    [Fact]
//    public async Task ExecuteAsync_WhenFullyLoadedIsTrue_ReturnsOnlyFullyLoadedPlugins()
//    {
//        RegisteredPlugin p1 = new(new LoadedPlugin(new DiscoveredPlugin("Plugin1", new PluginVersion(1, 0, 0), "/p1", DateTimeOffset.UtcNow), [typeof(string)], null!));
//        p1.MarkTypeLoaded(typeof(string), TimeProvider.System); // FullyLoaded = true

//        RegisteredPlugin p2 = new(new LoadedPlugin(new DiscoveredPlugin("Plugin2", new PluginVersion(1, 0, 0), "/p2", DateTimeOffset.UtcNow), [typeof(string)], null!)); // FullyLoaded = false

//        _serviceMock
//            .Setup(s => s.GetPluginsAsync(null, null, It.IsAny<CancellationToken>()))
//            .Returns(CreateAsyncEnumerable([p1, p2]));

//        GetPlugins command = new(_serviceMock.Object, _validator);
//        GetPluginsRequest request = new() { FullyLoaded = true };

//        ListedResult<RegisteredPlugin> result = await command.ExecuteAsync(request);

//        Assert.True(result.Success);
//        Assert.NotNull(result.Data);
//        Assert.Single(result.Data);
//        Assert.Same(p1, result.Data[0]);
//    }
//}
