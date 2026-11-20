using MelloSilveiraTools.Plugins.Infrastructure.Models;
using Microsoft.Extensions.DependencyInjection;

namespace MelloSilveiraTools.Plugins.Infrastructure.Services;

/// <summary>
/// High-level orchestrator for plugin discovery, loading, reload, and cache management.
/// </summary>
public interface IPluginService
{
    /// <summary>
    /// Asynchronously discovers plugins in storage and registers their services at application startup, using the root service collection.
    /// </summary>
    /// <param name="services">The service collection to register plugin-contributed services into during application startup.</param>
    /// <param name="name">Optional plugin name filter. When empty, all plugins are considered.</param>
    /// <param name="version">Optional version filter. When <c>null</c>, all versions are considered.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous startup load operation.</returns>
    Task LoadPluginsOnStartupAsync(IServiceCollection services, string? name = null, PluginVersion? version = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously discovers plugins in storage and registers their services at runtime through the dynamic service provider.
    /// </summary>
    /// <param name="name">Optional plugin name filter. When empty, all plugins are considered.</param>
    /// <param name="version">Optional version filter. When <c>null</c>, all versions are considered.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous runtime load operation.</returns>
    Task LoadPluginsOnRuntimeAsync(string? name = null, PluginVersion? version = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously reloads plugins previously moved to the loaded folder, optionally forcing a fresh load, at startup time.
    /// </summary>
    /// <param name="services">The service collection to register plugin-contributed services into during application startup.</param>
    /// <param name="forceLoad">When <c>true</c>, clears the cache for the matching plugins and loads them again.</param>
    /// <param name="name">Optional plugin name filter. When empty, all plugins are considered.</param>
    /// <param name="version">Optional version filter. When <c>null</c>, all versions are considered.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous startup reload operation.</returns>
    Task ReloadPluginsOnStartupAsync(IServiceCollection services, bool forceLoad, string? name = null, PluginVersion? version = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously reloads plugins previously moved to the loaded folder, optionally forcing a fresh load, at runtime.
    /// </summary>
    /// <param name="forceLoad">When <c>true</c>, clears the cache for the matching plugins and loads them again.</param>
    /// <param name="name">Optional plugin name filter. When empty, all plugins are considered.</param>
    /// <param name="version">Optional version filter. When <c>null</c>, all versions are considered.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous runtime reload operation.</returns>
    Task ReloadPluginsOnRuntimeAsync(bool forceLoad, string? name = null, PluginVersion? version = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously streams plugins available in storage that match the provided filters, returning registry information for each one.
    /// </summary>
    /// <param name="name">Optional plugin name filter. When empty, all plugins are considered.</param>
    /// <param name="version">Optional version filter. When <c>null</c>, all versions are considered.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An asynchronous stream of registered plugins discovered in storage.</returns>
    IAsyncEnumerable<RegisteredPlugin> GetPluginsAsync(string? name = null, PluginVersion? version = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the plugin cache entirely.
    /// </summary>
    Task ClearAsync();

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
