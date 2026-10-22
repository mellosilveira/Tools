using MelloSilveiraTools.Core.Providers.Dynamics;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Runtime.Loader;

namespace UnitTests.Plugins.Infrastructure.Models;

public sealed class PluginModelsTests
{
    [Fact]
    public void DiscoveredPlugin_ParameterizedConstructor_SetsAllProperties()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        PluginVersion version = new(1, 0, 0);

        DiscoveredPlugin plugin = new("Calculators", version, "/storage/Calculators.v1.0.0.dll", now);

        Assert.Equal("Calculators", plugin.Name);
        Assert.Equal(version, plugin.Version);
        Assert.Equal("/storage/Calculators.v1.0.0.dll", plugin.FullPath);
        Assert.Equal(now, plugin.DiscoveredAt);
    }

    [Fact]
    public void DiscoveredPlugin_ParameterlessConstructor_InitializesDefaults()
    {
        DiscoveredPlugin plugin = new();

        Assert.Equal(string.Empty, plugin.Name);
        Assert.Equal(PluginVersion.Default, plugin.Version);
        Assert.Equal(string.Empty, plugin.FullPath);
        Assert.Equal(default, plugin.DiscoveredAt);
    }

    [Fact]
    public void LoadedPlugin_Constructor_WrapsDiscoveredPluginAndSetsProcessableTypes()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        PluginVersion version = new(2, 0, 0);
        DiscoveredPlugin discovered = new("Mechanics", version, "/storage/Mechanics.v2.0.0.dll", now);
        Type[] types = [typeof(string), typeof(int)];
        AssemblyLoadContext context = new("TestContext", isCollectible: true);

        LoadedPlugin loaded = new(discovered, types, context);

        Assert.Equal("Mechanics", loaded.Name);
        Assert.Equal(version, loaded.Version);
        Assert.Equal("/storage/Mechanics.v2.0.0.dll", loaded.FullPath);
        Assert.Equal(now, loaded.DiscoveredAt);
        Assert.Equal(types, loaded.ProcessableTypes);

        // Test UnloadAssembly does not throw
        loaded.UnloadAssembly();
    }

    [Fact]
    public void LoadedPlugin_ParameterlessConstructor_InitializesDefaults()
    {
        LoadedPlugin loaded = new();

        Assert.Empty(loaded.ProcessableTypes);
        loaded.UnloadAssembly();
    }

    [Fact]
    public void RegisteredPlugin_ConstructorFromLoaded_InitializesStatusAsFalse()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        PluginVersion version = new(1, 2, 0);
        DiscoveredPlugin discovered = new("TestPlugin", version, "/storage/TestPlugin.v1.2.0.dll", now);
        Type[] types = [typeof(string), typeof(int)];
        AssemblyLoadContext context = new("TestContext2", isCollectible: true);
        LoadedPlugin loaded = new(discovered, types, context);

        RegisteredPlugin registered = new(loaded);

        Assert.Equal("TestPlugin", registered.Name);
        Assert.Equal(version, registered.Version);
        Assert.Equal(2, registered.TypesLoadedStatus.Count);
        Assert.False(registered.TypesLoadedStatus[typeof(string)]);
        Assert.False(registered.TypesLoadedStatus[typeof(int)]);
        Assert.False(registered.IsFullyLoaded);
        Assert.Null(registered.FullyLoadedAt);

        // Mark first type loaded
        registered.MarkTypeLoaded(typeof(string), TimeProvider.System);
        Assert.True(registered.TypesLoadedStatus[typeof(string)]);
        Assert.False(registered.IsFullyLoaded);
        Assert.Null(registered.FullyLoadedAt);

        // Mark second type loaded
        registered.MarkTypeLoaded(typeof(int), TimeProvider.System);
        Assert.True(registered.TypesLoadedStatus[typeof(int)]);
        Assert.True(registered.IsFullyLoaded);
        Assert.NotNull(registered.FullyLoadedAt);
    }

    [Fact]
    public void RegisteredPlugin_ParameterlessConstructor_InitializesEmpty()
    {
        RegisteredPlugin registered = new();

        Assert.Empty(registered.TypesLoadedStatus);
        Assert.False(registered.IsFullyLoaded);
        Assert.Null(registered.FullyLoadedAt);
    }

    [Fact]
    public void PluginRegistrationContext_ForStartup_SetsServicesAndFlagsIsStartupTrue()
    {
        ServiceCollection services = new();

        PluginRegistrationContext context = PluginRegistrationContext.ForStartup(services);

        Assert.Same(services, context.Services);
        Assert.Null(context.DynamicProvider);
        Assert.True(context.IsStartup);
    }

    [Fact]
    public void PluginRegistrationContext_ForRuntime_SetsDynamicProviderAndFlagsIsStartupFalse()
    {
        Mock<IDynamicServiceProvider> dynamicMock = new();

        PluginRegistrationContext context = PluginRegistrationContext.ForRuntime(dynamicMock.Object);

        Assert.Null(context.Services);
        Assert.Same(dynamicMock.Object, context.DynamicProvider);
        Assert.False(context.IsStartup);
    }
}
