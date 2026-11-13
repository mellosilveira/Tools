using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Plugins.Application.Commands.Cache;
using MelloSilveiraTools.Plugins.Infrastructure.Services;
using Moq;

namespace UnitTests.Plugins.Application.Commands.Cache;

public sealed class ClearPluginCacheTests
{
    [Fact]
    public async Task ExecuteAsync_CallsServiceClearAsyncAndReturnsOk()
    {
        Mock<IPluginService> serviceMock = new();
        serviceMock.Setup(s => s.ClearAsync()).Returns(Task.CompletedTask);

        ClearPluginCache command = new(serviceMock.Object);

        Result result = await command.ExecuteAsync();

        Assert.True(result.Success);
        Assert.Equal(StatusCode.OK, result.StatusCode);
        serviceMock.Verify(s => s.ClearAsync(), Times.Once);
    }
}
