using MelloSilveiraTools.Core.Caching;
using MelloSilveiraTools.Core.Managers.File.Providers;
using MelloSilveiraTools.Core.Providers.Dynamics;
using MelloSilveiraTools.Plugins.Infrastructure;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Runtime.Loader;

namespace UnitTests.Plugins.Infrastructure;

public sealed class PluginAssemblyProcessorTests
{
    private sealed class TestTypeProcessor(Type processableType) : IPluginTypeProcessor
    {
        public Type ProcessableType => processableType;
        public List<Type> ProcessedTypes { get; } = [];

        public void Process(Type type, PluginRegistrationContext context)
        {
            ProcessedTypes.Add(type);
        }
    }

    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        Mock<IPluginTypeProcessor> mockProcessor = new();
        List<IPluginTypeProcessor> processors = [mockProcessor.Object];
        PluginCache cache = new(new InMemoryTwoLevelCache());
        IFileStorageService mockStorage = Mock.Of<IFileStorageService>();

        PluginAssemblyProcessor processor = new(processors, mockStorage, cache);

        Assert.NotNull(processor);
    }

    [Fact]
    public async Task LoadAsync_StreamsAssemblyFromStorageAndExtractsProcessableTypes()
    {
        // Arrange
        byte[] assemblyBytes = File.ReadAllBytes(typeof(PluginAssemblyProcessor).Assembly.Location);
        string assemblyPath = "storage/plugins/incoming/MelloSilveiraTools.Plugins.v2.0.0.dll";

        Mock<IFileStorageService> storageMock = new();
        storageMock
            .Setup(s => s.OpenReadAsync(assemblyPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream(assemblyBytes));

        TestTypeProcessor typeProcessor = new(typeof(object));
        PluginCache cache = new(new InMemoryTwoLevelCache());
        PluginAssemblyProcessor assemblyProcessor = new([typeProcessor], storageMock.Object, cache);

        DiscoveredPlugin discovered = new("MelloSilveiraTools.Plugins", new PluginVersion(2, 0, 0), assemblyPath, DateTimeOffset.UtcNow);

        // Act
        LoadedPlugin loaded = await assemblyProcessor.LoadAsync(discovered);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal("MelloSilveiraTools.Plugins", loaded.Name);
        Assert.Equal(new PluginVersion(2, 0, 0), loaded.Version);
        Assert.Contains(loaded.ProcessableTypes, t => t.Name == nameof(PluginFileProcessor));

        // Cleanup
        loaded.UnloadAssembly();
    }

    [Fact]
    public void GetInfo_ReturnsRegisteredPluginFromLoaded()
    {
        // Arrange
        PluginCache cache = new(new InMemoryTwoLevelCache());
        IFileStorageService storage = Mock.Of<IFileStorageService>();
        PluginAssemblyProcessor assemblyProcessor = new([], storage, cache);

        DiscoveredPlugin discovered = new("PluginX", new PluginVersion(1, 0, 0), "/pathX", DateTimeOffset.UtcNow);
        AssemblyLoadContext alc = new("TestAlc", isCollectible: true);
        LoadedPlugin loaded = new(discovered, [typeof(string), typeof(int)], alc);

        // Act
        RegisteredPlugin info = assemblyProcessor.GetInfo(loaded);

        // Assert
        Assert.NotNull(info);
        Assert.Equal("PluginX", info.Name);
        Assert.Equal(2, info.TypesLoadedStatus.Count);
        Assert.False(info.IsFullyLoaded);
    }

    [Fact]
    public void ProcessTypes_ExecutesTypeProcessorsAndMarksRegistered()
    {
        // Arrange
        PluginCache cache = new(new InMemoryTwoLevelCache());
        IFileStorageService storage = Mock.Of<IFileStorageService>();
        TestTypeProcessor typeProcessor = new(typeof(string));
        PluginAssemblyProcessor assemblyProcessor = new([typeProcessor], storage, cache);

        DiscoveredPlugin discovered = new("PluginY", new PluginVersion(1, 0, 0), "/pathY", DateTimeOffset.UtcNow);
        AssemblyLoadContext alc = new("TestAlcY", isCollectible: true);
        LoadedPlugin loaded = new(discovered, [typeof(string)], alc);

        Mock<IDynamicServiceProvider> dynamicProviderMock = new();
        PluginRegistrationContext context = PluginRegistrationContext.ForRuntime(dynamicProviderMock.Object);

        // Act
        RegisteredPlugin registered = assemblyProcessor.ProcessTypes(loaded, context);

        // Assert
        Assert.NotNull(registered);
        Assert.True(registered.IsFullyLoaded);
        Assert.Contains(typeof(string), typeProcessor.ProcessedTypes);
        Assert.True(cache.TryGet<RegisteredPlugin>("PluginY", new PluginVersion(1, 0, 0), out RegisteredPlugin? cached));
        Assert.Same(registered, cached);
    }
}
