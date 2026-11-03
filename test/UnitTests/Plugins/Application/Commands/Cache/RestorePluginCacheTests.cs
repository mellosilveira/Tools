using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Plugins.Application.Commands;
using MelloSilveiraTools.Plugins.Application.Commands.Cache;
using MelloSilveiraTools.Plugins.Application.Validators;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Services;
using Moq;

namespace UnitTests.Plugins.Application.Commands.Cache;

public sealed class RestorePluginCacheTests
{
    private readonly Mock<IPluginService> _serviceMock = new();
    private readonly PluginValidator _validator = new();

    [Fact]
    public async Task ExecuteAsync_WhenRequestIsValid_CallsRestoreCacheAsyncAndReturnsOk()
    {
        _serviceMock
            .Setup(s => s.RestoreCacheAsync("TestPlugin", new PluginVersion(2, 1, 0), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        RestorePluginCache command = new(_serviceMock.Object, _validator);
        PluginsRequest request = new() { Name = "TestPlugin", Version = "v2.1.0" };

        Result result = await command.ExecuteAsync(request);

        Assert.True(result.Success);
        Assert.Equal(StatusCode.OK, result.StatusCode);
        _serviceMock.Verify(s => s.RestoreCacheAsync("TestPlugin", new PluginVersion(2, 1, 0), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenVersionIsInvalid_ReturnsBadRequestWithoutCallingService()
    {
        RestorePluginCache command = new(_serviceMock.Object, _validator);
        PluginsRequest request = new() { Name = "TestPlugin", Version = "invalid-version" };

        Result result = await command.ExecuteAsync(request);

        Assert.False(result.Success);
        Assert.Equal(StatusCode.BadRequest, result.StatusCode);
        _serviceMock.Verify(s => s.RestoreCacheAsync(It.IsAny<string?>(), It.IsAny<PluginVersion?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
