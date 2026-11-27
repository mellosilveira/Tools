using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Plugins.Application.Commands.Reload;
using MelloSilveiraTools.Plugins.Application.Validators;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Services;
using Moq;

namespace UnitTests.Plugins.Application.Commands.Reload;

public sealed class ReloadPluginsTests
{
    private readonly Mock<IPluginService> _serviceMock = new();
    private readonly PluginValidator<ReloadPluginsRequest> _validator = new();

    [Fact]
    public async Task ExecuteAsync_WhenRequestIsValid_CallsReloadPluginsOnRuntimeAsyncAndReturnsOk()
    {
        _serviceMock
            .Setup(s => s.ReloadPluginsOnRuntimeAsync(true, "CorePlugin", new PluginVersion(3, 0, 0), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ReloadPlugins command = new(_serviceMock.Object, _validator);
        ReloadPluginsRequest request = new() { Name = "CorePlugin", Version = "v3.0.0", Force = true };

        Result result = await command.ExecuteAsync(request);

        Assert.True(result.Success);
        Assert.Equal(StatusCode.OK, result.StatusCode);
        _serviceMock.Verify(s => s.ReloadPluginsOnRuntimeAsync(true, "CorePlugin", new PluginVersion(3, 0, 0), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenVersionIsInvalid_ReturnsBadRequest()
    {
        ReloadPlugins command = new(_serviceMock.Object, _validator);
        ReloadPluginsRequest request = new() { Name = "CorePlugin", Version = "invalid-v", Force = true };

        Result result = await command.ExecuteAsync(request);

        Assert.False(result.Success);
        Assert.Equal(StatusCode.BadRequest, result.StatusCode);
        _serviceMock.Verify(s => s.ReloadPluginsOnRuntimeAsync(It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<PluginVersion?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
