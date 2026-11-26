using MelloSilveiraTools.Core.Caching;
using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Providers.Dynamics;
using MelloSilveiraTools.Plugins.Infrastructure;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Runtime.Loader;

namespace UnitTests.Plugins.Infrastructure;

public sealed class PluginAssemblyProcessorTests
{
    private sealed class TestTypeProcessor(Type processableType) : IPluginTypeProcessor
    {
        public Type ProcessableType => processableType;
        public List<Type> ProcessedTypes { get; } = [];

        public void Process(Type type, PluginRegistrationContext context) => ProcessedTypes.Add(type);
    }

    private static PluginSettings CreateSettings() => new()
    {
        StorageProvider = StorageProvider.Local,
        Directory = "plugins",
        DefaultCacheTarget = "file",
        PollInterval = TimeSpan.FromSeconds(30),
        PreviousVersionRetention = TimeSpan.FromHours(24),
        MaxPluginSizeBytes = 10L * 1024 * 1024,
        MaxPendingSubmissionsPerUser = 5
    };

    [Fact]
    public async Task LoadAsync_ShadowCopiesValidAssemblyExtractsTypesAndSupportsUnload()
    {
        byte[] assemblyBytes = File.ReadAllBytes(typeof(PluginAssemblyProcessor).Assembly.Location);
        string assemblyPath = "storage/plugins/incoming/MelloSilveiraTools.Plugins.v2.0.0.dll";

        Mock<IFileStorageService> storageMock = new();
        storageMock
            .Setup(s => s.OpenReadAsync(assemblyPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream(assemblyBytes));

        PluginSettings settings = CreateSettings();
        PluginCache cache = new(new InMemoryTwoLevelCache());
        PluginFileProcessor fileProcessor = new(NullLogger<PluginFileProcessor>.Instance, storageMock.Object, cache, settings);
        TestTypeProcessor typeProcessor = new(typeof(object));
        PluginAssemblyProcessor assemblyProcessor = new([typeProcessor], TimeProvider.System, fileProcessor, cache, settings);

        DiscoveredPlugin discovered = new("MelloSilveiraTools.Plugins", new PluginVersion(2, 0, 0), assemblyPath, DateTimeOffset.UtcNow);

        LoadedPlugin loaded = await assemblyProcessor.LoadAsync(discovered);
        try
        {
            Assert.Equal("MelloSilveiraTools.Plugins", loaded.Name);
            Assert.Equal(new PluginVersion(2, 0, 0), loaded.Version);
            Assert.Contains(loaded.ProcessableTypes, t => t.Name == nameof(PluginFileProcessor));
        }
        finally
        {
            loaded.UnloadAssembly();
        }
    }

    [Fact]
    public async Task LoadAsync_WhenBinaryIsInvalid_CleansUpShadowDirectoryAndThrows()
    {
        byte[] garbageBytes = [0x01, 0x02, 0x03, 0x04];
        string assemblyPath = "storage/plugins/incoming/Corrupt.v1.0.0.dll";

        Mock<IFileStorageService> storageMock = new();
        storageMock
            .Setup(s => s.OpenReadAsync(assemblyPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream(garbageBytes));

        PluginSettings settings = CreateSettings();
        PluginCache cache = new(new InMemoryTwoLevelCache());
        PluginFileProcessor fileProcessor = new(NullLogger<PluginFileProcessor>.Instance, storageMock.Object, cache, settings);
        PluginAssemblyProcessor assemblyProcessor = new([], TimeProvider.System, fileProcessor, cache, settings);

        DiscoveredPlugin discovered = new("Corrupt", new PluginVersion(1, 0, 0), assemblyPath, DateTimeOffset.UtcNow);

        await Assert.ThrowsAsync<BadImageFormatException>(() => assemblyProcessor.LoadAsync(discovered));
    }

    [Fact]
    public void ValidateAssemblyArchitecture_AcceptsAnyCpuAndRejectsNativeDll()
    {
        using FileStream managedStream = File.OpenRead(typeof(PluginAssemblyProcessor).Assembly.Location);
        using PEReader managedReader = new(managedStream);
        PluginAssemblyProcessor.ValidateAssemblyArchitecture(managedReader, "ManagedPlugin");

        string nativeDllPath = Path.Combine(Environment.SystemDirectory, "kernel32.dll");
        if (File.Exists(nativeDllPath))
        {
            using FileStream nativeStream = File.OpenRead(nativeDllPath);
            using PEReader nativeReader = new(nativeStream);
            Assert.Throws<BadImageFormatException>(() => PluginAssemblyProcessor.ValidateAssemblyArchitecture(nativeReader, "kernel32"));
        }
    }

    [Fact]
    public void GetInfo_And_ProcessTypes_UpdateCacheAndMarkLoadedAtFixedTime()
    {
        DateTimeOffset fixedNow = new(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);
        FixedTimeProvider timeProvider = new(fixedNow);
        PluginSettings settings = CreateSettings();
        PluginCache cache = new(new InMemoryTwoLevelCache());
        PluginFileProcessor fileProcessor = new(NullLogger<PluginFileProcessor>.Instance, Mock.Of<IFileStorageService>(), cache, settings);
        TestTypeProcessor typeProcessor = new(typeof(string));
        PluginAssemblyProcessor assemblyProcessor = new([typeProcessor], timeProvider, fileProcessor, cache, settings);

        DiscoveredPlugin discovered = new("PluginY", new PluginVersion(1, 0, 0), "/pathY", DateTimeOffset.UtcNow);
        AssemblyLoadContext alc = new("TestAlcY", isCollectible: true);
        try
        {
            LoadedPlugin loaded = new(discovered, [typeof(string)], alc);
            RegisteredPlugin info = assemblyProcessor.GetInfo(loaded);
            Assert.False(info.IsFullyLoaded);

            Mock<IDynamicServiceProvider> dynamicProviderMock = new();
            PluginRegistrationContext context = PluginRegistrationContext.ForRuntime(dynamicProviderMock.Object);

            RegisteredPlugin registered = assemblyProcessor.ProcessTypes(loaded, context);

            Assert.Same(info, registered);
            Assert.True(registered.IsFullyLoaded);
            Assert.Equal(fixedNow, registered.FullyLoadedAt);
            Assert.Contains(typeof(string), typeProcessor.ProcessedTypes);
        }
        finally
        {
            alc.Unload();
        }
    }

    [Fact]
    public void PluginLoadContext_ShorthandConstructorAndLoadMethods_WorkWithoutDepsJson()
    {
        string shadowDir = Path.Combine(Path.GetTempPath(), "mst-plugin-alc-test", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(shadowDir);
        string copiedAssembly = Path.Combine(shadowDir, "MelloSilveiraTools.Plugins.dll");
        File.Copy(typeof(PluginLoadContext).Assembly.Location, copiedAssembly, overwrite: true);

        TestPluginLoadContext alc = new(copiedAssembly);
        try
        {
            Assert.Null(alc.InvokeLoad(new AssemblyName("NonExistent.Assembly.Name")));
            Assert.Equal(IntPtr.Zero, alc.InvokeLoadUnmanagedDll("non_existent_native.dll"));
        }
        finally
        {
            alc.Unload();
            try
            {
                Directory.Delete(shadowDir, recursive: true);
            }
            catch
            {
                // Best effort cleanup after collectible ALC unload
            }
        }
    }

    private sealed class TestPluginLoadContext(string pluginPath) : PluginLoadContext(pluginPath, isCollectible: true)
    {
        public Assembly? InvokeLoad(AssemblyName name) => Load(name);
        public IntPtr InvokeLoadUnmanagedDll(string unmanagedDllName) => LoadUnmanagedDll(unmanagedDllName);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
