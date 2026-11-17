using MelloSilveiraTools.Core.Providers.Dynamics;
using MelloSilveiraTools.Plugins.Infrastructure;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;

namespace UnitTests.Plugins.Infrastructure.Models;

public sealed class PluginModelsTests
{
    [Fact]
    public void DiscoveredPlugin_Constructors_SetProperties()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        PluginVersion version = new(1, 0, 0);

        DiscoveredPlugin parameterized = new("Calculators", version, "/storage/Calculators.v1.0.0.dll", now);
        Assert.Equal("Calculators", parameterized.Name);
        Assert.Equal(version, parameterized.Version);
        Assert.Equal("/storage/Calculators.v1.0.0.dll", parameterized.FullPath);
        Assert.Equal(now, parameterized.DiscoveredAt);

        DiscoveredPlugin parameterless = new();
        Assert.Equal(string.Empty, parameterless.Name);
        Assert.Equal(PluginVersion.Default, parameterless.Version);
        Assert.Equal(string.Empty, parameterless.FullPath);
        Assert.Equal(default, parameterless.DiscoveredAt);
    }

    [Fact]
    public void LoadedPlugin_ConstructorsAndUnloadAssembly_CleanupShadowDirectory()
    {
        string tempShadowDir = Path.Combine(Path.GetTempPath(), "mst-loaded-plugin-test", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempShadowDir);
        File.WriteAllText(Path.Combine(tempShadowDir, "dummy.txt"), "x");

        DateTimeOffset now = DateTimeOffset.UtcNow;
        PluginVersion version = new(2, 0, 0);
        DiscoveredPlugin discovered = new("Mechanics", version, "/storage/Mechanics.v2.0.0.dll", now);
        Type[] types = [typeof(string), typeof(int)];
        AssemblyLoadContext context = new("TestContext", isCollectible: true);

        LoadedPlugin loaded = new(discovered, types, context, tempShadowDir);
        Assert.Equal("Mechanics", loaded.Name);
        Assert.Equal(version, loaded.Version);
        Assert.Equal(types, loaded.ProcessableTypes);

        loaded.UnloadAssembly();
        Assert.False(Directory.Exists(tempShadowDir));

        LoadedPlugin empty = new();
        Assert.Empty(empty.ProcessableTypes);
        empty.UnloadAssembly();
    }

    [Fact]
    public void RegisteredPlugin_MarkTypeLoaded_TransitionsToFullyLoadedWhenAllTypesRegistered()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateTimeOffset t1 = new(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
        DateTimeOffset t2 = t1.AddSeconds(5);
        DiscoveredPlugin discovered = new("TestPlugin", new PluginVersion(1, 2, 0), "/storage/TestPlugin.v1.2.0.dll", now);
        AssemblyLoadContext context = new("TestContext2", isCollectible: true);
        LoadedPlugin loaded = new(discovered, [typeof(string), typeof(int)], context);
        context.Unload();

        RegisteredPlugin registered = new(loaded);
        Assert.Equal(2, registered.TypesLoadedStatus.Count);
        Assert.False(registered.IsFullyLoaded);
        Assert.Null(registered.FullyLoadedAt);

        registered.MarkTypeLoaded(typeof(string), t1);
        Assert.False(registered.IsFullyLoaded);
        Assert.Null(registered.FullyLoadedAt);

        registered.MarkTypeLoaded(typeof(int), t2);
        Assert.True(registered.IsFullyLoaded);
        Assert.Equal(t2, registered.FullyLoadedAt);

        RegisteredPlugin empty = new();
        Assert.Empty(empty.TypesLoadedStatus);
        Assert.False(empty.IsFullyLoaded);
    }

    [Fact]
    public void PluginRegistrationContext_FactoryMethods_SetExpectedTargets()
    {
        ServiceCollection services = new();
        PluginRegistrationContext startup = PluginRegistrationContext.ForStartup(services);
        Assert.True(startup.IsStartup);
        Assert.Same(services, startup.Services);
        Assert.Null(startup.DynamicProvider);

        Mock<IDynamicServiceProvider> dynamicMock = new();
        PluginRegistrationContext runtime = PluginRegistrationContext.ForRuntime(dynamicMock.Object);
        Assert.False(runtime.IsStartup);
        Assert.Null(runtime.Services);
        Assert.Same(dynamicMock.Object, runtime.DynamicProvider);
    }

    [Fact]
    public void PluginVersionJsonConverter_HandlesObjectFieldsDefaultsEmptyStringAndOtherTokens()
    {
        JsonSerializerOptions options = new() { Converters = { new PluginVersionJsonConverter() } };

        Assert.Equal(PluginVersion.Default, JsonSerializer.Deserialize<PluginVersion>("\"\"", options));
        Assert.Equal(new PluginVersion(2, 3, 4), JsonSerializer.Deserialize<PluginVersion>("{\"Major\":2,\"Minor\":3,\"Patch\":4}", options));
        Assert.Equal(new PluginVersion(0, 0, 0), JsonSerializer.Deserialize<PluginVersion>("{}", options));
        Assert.Equal(PluginVersion.Default, JsonSerializer.Deserialize<PluginVersion>("42", options));
    }

    [Fact]
    public void PluginLoadContext_UnresolvedManagedAndUnmanaged_ReturnFallbackDefaults()
    {
        PluginLoadContext alc = new(typeof(PluginLoadContext).Assembly.Location, isCollectible: true);
        try
        {
            MethodInfo loadMethod = typeof(PluginLoadContext).GetMethod("Load", BindingFlags.Instance | BindingFlags.NonPublic)!;
            MethodInfo loadUnmanagedMethod = typeof(PluginLoadContext).GetMethod("LoadUnmanagedDll", BindingFlags.Instance | BindingFlags.NonPublic)!;

            Assert.Null(loadMethod.Invoke(alc, [new AssemblyName("NonExistent.Plugin.Dep")]));
            Assert.Equal(IntPtr.Zero, (IntPtr)loadUnmanagedMethod.Invoke(alc, ["nonexistent_native_lib"])!);
        }
        finally
        {
            alc.Unload();
        }
    }
}
