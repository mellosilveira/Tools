using MelloSilveiraTools.Plugins.Infrastructure.Models;
using Microsoft.Extensions.DependencyInjection;

namespace MelloSilveiraTools.Plugins.Infrastructure.Services;

/// <summary>
/// High-level orchestrator for plugin discovery, loading, reload, and cache management.
/// </summary>
public interface IPluginService
{
    /// <summary>
    /// Discovers plugins and registers their services at application startup, using the root service collection.
    /// </summary>
    /// <param name="services">The service collection to register plugin-contributed services into during application startup.</param>
    /// <param name="name">Optional plugin name filter. When empty, all plugins are considered.</param>
    /// <param name="version">Optional version filter. When <c>null</c>, all versions are considered.</param>
    /// <example>
    /// <code>
    /// // In Program.cs / Startup.cs:
    /// services
    ///     .AddToolsServices(configuration)
    ///     .AddPluginServices(configuration);
    ///
    /// var app = builder.Build();
    /// app.Services.GetRequiredService&lt;IPluginService&gt;().LoadPluginsOnStartup();
    /// </code>
    /// </example>
    void LoadPluginsOnStartup(IServiceCollection services, string? name = null, PluginVersion? version = null);

    /// <summary>
    /// Discovers plugins and registers their services at runtime through the dynamic service provider.
    /// </summary>
    /// <param name="name">Optional plugin name filter. When empty, all plugins are considered.</param>
    /// <param name="version">Optional version filter. When <c>null</c>, all versions are considered.</param>
    void LoadPluginsOnRuntime(string? name = null, PluginVersion? version = null);

    /// <summary>
    /// Reloads plugins previously moved to the loaded folder, optionally forcing a fresh load, at startup time.
    /// </summary>
    /// <param name="services">The service collection to register plugin-contributed services into during application startup.</param>
    /// <param name="forceLoad">When <c>true</c>, clears the cache for the matching plugins and loads them again.</param>
    /// <param name="name">Optional plugin name filter. When empty, all plugins are considered.</param>
    /// <param name="version">Optional version filter. When <c>null</c>, all versions are considered.</param>
    void ReloadPluginsOnStartup(IServiceCollection services, bool forceLoad, string? name = null, PluginVersion? version = null);

    /// <summary>
    /// Reloads plugins previously moved to the loaded folder, optionally forcing a fresh load, at runtime.
    /// </summary>
    /// <param name="forceLoad">When <c>true</c>, clears the cache for the matching plugins and loads them again.</param>
    /// <param name="name">Optional plugin name filter. When empty, all plugins are considered.</param>
    /// <param name="version">Optional version filter. When <c>null</c>, all versions are considered.</param>
    void ReloadPluginsOnRuntime(bool forceLoad, string? name = null, PluginVersion? version = null);

    /// <summary>
    /// Clears the plugin cache entirely.
    /// </summary>
    Task ClearAsync();

    /// <summary>
    /// Lists plugins available on disk that match the provided filters, returning registry information for each one.
    /// </summary>
    /// <param name="name">Optional plugin name filter. When empty, all plugins are considered.</param>
    /// <param name="version">Optional version filter. When <c>null</c>, all versions are considered.</param>
    /// <returns>The registered plugins discovered on disk.</returns>
    IEnumerable<RegisteredPlugin> GetPlugins(string? name, PluginVersion? version);

    /// <summary>
    /// Persists the current plugin cache entries matching the provided filters through the configured persistence.
    /// </summary>
    /// <param name="name">Optional plugin name filter. When empty, all plugins are considered.</param>
    /// <param name="version">Optional version filter. When <c>null</c>, all versions are considered.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task PersistCacheAsync(string? name = null, PluginVersion? version = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores plugin cache entries matching the provided filters from the configured persistence.
    /// </summary>
    /// <param name="name">Optional plugin name filter. When empty, all plugins are considered.</param>
    /// <param name="version">Optional version filter. When <c>null</c>, all versions are considered.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task RestoreCacheAsync(string? name = null, PluginVersion? version = null, CancellationToken cancellationToken = default);
}
