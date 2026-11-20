using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Plugins.Application.Commands;
using MelloSilveiraTools.Plugins.Application.Commands.Load;
using MelloSilveiraTools.Plugins.Application.Validators;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Services;
using Moq;

namespace UnitTests.Plugins.Application.Commands.Load;

public sealed class LoadPluginsTests
{
    private readonly Mock<IPluginService> _serviceMock = new();
    private readonly PluginValidator _validator = new();

    [Fact]
    public async Task ExecuteAsync_WhenRequestIsValid_CallsLoadPluginsOnRuntimeAsyncAndReturnsOk()
    {
        _serviceMock
            .Setup(s => s.LoadPluginsOnRuntimeAsync("MyPlugin", new PluginVersion(1, 0, 0), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        LoadPlugins command = new(_serviceMock.Object, _validator);
        PluginsRequest request = new() { Name = "MyPlugin", Version = "v1.0.0" };

        Result result = await command.ExecuteAsync(request);

        Assert.True(result.Success);
        Assert.Equal(StatusCode.OK, result.StatusCode);
        _serviceMock.Verify(s => s.LoadPluginsOnRuntimeAsync("MyPlugin", new PluginVersion(1, 0, 0), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenVersionIsInvalid_ReturnsBadRequest()
    {
        LoadPlugins command = new(_serviceMock.Object, _validator);
        PluginsRequest request = new() { Name = "MyPlugin", Version = "invalid" };

        Result result = await command.ExecuteAsync(request);

        Assert.False(result.Success);
        Assert.Equal(StatusCode.BadRequest, result.StatusCode);
        _serviceMock.Verify(s => s.LoadPluginsOnRuntimeAsync(It.IsAny<string?>(), It.IsAny<PluginVersion?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
