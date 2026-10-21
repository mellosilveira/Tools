using MelloSilveiraTools.Database.RelationalDatabase.Attributes;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Persistences;
using System.Reflection;

namespace UnitTests.Plugins.Infrastructure.Persistences;

public sealed class PluginCacheEntitiesTests
{
    [Fact]
    public void PluginCacheEntity_PropertiesAndAttributes_AreConfiguredCorrectly()
    {
        PluginCacheEntity entity = new()
        {
            PluginName = "MyPlugin",
            PluginVersion = "v1.0.0",
            StateType = "LoadedPlugin",
            StateJson = "{}"
        };

        Assert.Equal("MyPlugin", entity.PluginName);
        Assert.Equal("v1.0.0", entity.PluginVersion);
        Assert.Equal("LoadedPlugin", entity.StateType);
        Assert.Equal("{}", entity.StateJson);

        TableAttribute? tableAttr = typeof(PluginCacheEntity).GetCustomAttribute<TableAttribute>();
        Assert.NotNull(tableAttr);
        Assert.Equal("plugin_cache", tableAttr.Name);

        PropertyInfo? nameProp = typeof(PluginCacheEntity).GetProperty(nameof(PluginCacheEntity.PluginName));
        Assert.NotNull(nameProp?.GetCustomAttribute<UniqueColumnAttribute>());

        PropertyInfo? versionProp = typeof(PluginCacheEntity).GetProperty(nameof(PluginCacheEntity.PluginVersion));
        Assert.NotNull(versionProp?.GetCustomAttribute<UniqueColumnAttribute>());

        PropertyInfo? stateTypeProp = typeof(PluginCacheEntity).GetProperty(nameof(PluginCacheEntity.StateType));
        Assert.NotNull(stateTypeProp?.GetCustomAttribute<ColumnAttribute>());

        PropertyInfo? stateJsonProp = typeof(PluginCacheEntity).GetProperty(nameof(PluginCacheEntity.StateJson));
        Assert.NotNull(stateJsonProp?.GetCustomAttribute<ColumnAttribute>());
    }

    [Fact]
    public void PluginCacheFilter_PropertiesAndAttributes_AreConfiguredCorrectly()
    {
        PluginCacheFilter filter = new()
        {
            PluginName = "Contoso",
            PluginVersion = "v2.0.0"
        };

        Assert.Equal("Contoso", filter.PluginName);
        Assert.Equal("v2.0.0", filter.PluginVersion);

        PropertyInfo? nameProp = typeof(PluginCacheFilter).GetProperty(nameof(PluginCacheFilter.PluginName));
        FilterColumnAttribute? nameAttr = nameProp?.GetCustomAttribute<FilterColumnAttribute>();
        Assert.NotNull(nameAttr);
        Assert.Equal("=", nameAttr.FilterClause);

        PropertyInfo? versionProp = typeof(PluginCacheFilter).GetProperty(nameof(PluginCacheFilter.PluginVersion));
        FilterColumnAttribute? versionAttr = versionProp?.GetCustomAttribute<FilterColumnAttribute>();
        Assert.NotNull(versionAttr);
        Assert.Equal("=", versionAttr.FilterClause);
    }

    [Fact]
    public void PluginCacheEntry_ConstructorAndProperties_AreInitialized()
    {
        DiscoveredPlugin plugin = new("CorePlugin", new PluginVersion(1, 0, 0), "/path", DateTimeOffset.UtcNow);
        PluginCacheEntry entry = new("CorePlugin", "v1.0.0", plugin);

        Assert.Equal("CorePlugin", entry.Name);
        Assert.Equal("v1.0.0", entry.Version);
        Assert.Same(plugin, entry.State);
    }

    [Fact]
    public void PluginCacheTargets_Constants_AreDefined()
    {
        Assert.Equal("file", PluginCacheTargets.File);
        Assert.Equal("database", PluginCacheTargets.Database);
    }
}
