using MelloSilveiraTools.Core.Caching;
using MelloSilveiraTools.Plugins.Infrastructure;
using Moq;

namespace UnitTests.Plugins.Infrastructure;

public class PluginAssemblyProcessorTests
{
    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        // Arrange
        Mock<IPluginTypeProcessor> mockProcessor = new();
        List<IPluginTypeProcessor> processors = [mockProcessor.Object];
        Mock<PluginCache> mockCache = new(Mock.Of<ITwoLevelCache>());

        // Act
        PluginAssemblyProcessor processor = new(processors, mockCache.Object);

        // Assert
        Assert.NotNull(processor);
    }
}
