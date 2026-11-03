using MelloSilveiraTools.Core.Managers.File.Providers;
using MelloSilveiraTools.Plugins.Application.Commands.Cache;
using MelloSilveiraTools.Plugins.Application.Commands.Get;
using MelloSilveiraTools.Plugins.Application.Commands.Load;
using MelloSilveiraTools.Plugins.Application.Commands.Reload;
using MelloSilveiraTools.Plugins.Application.Commands.Upload;
using MelloSilveiraTools.Plugins.Application.Validators;
using MelloSilveiraTools.Plugins.Infrastructure;
using MelloSilveiraTools.Plugins.Infrastructure.Persistences;
using MelloSilveiraTools.Plugins.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.Plugins;

/// <summary>
/// Provides extension methods to register services from the MelloSilveiraTools.Plugins package.
/// </summary>
public static class PluginsDependencyInjection
{
    /// <summary>
    /// Registers plugin infrastructure, caching, providers, and plugin operations.
    /// </summary>
    /// <param name="services">Service collection that receives the registrations.</param>
    /// <param name="pluginSettings">Settings that describe plugin folders and behavior.</param>
    /// <returns>The same <paramref name="services"/> instance to allow call chaining.</returns>
    public static IServiceCollection AddPluginServices(this IServiceCollection services, PluginSettings pluginSettings) => services
        // Register settings.
        .AddSingleton(pluginSettings)
        // Required so plugin cache operations can read the {target} route value.
        .AddHttpContextAccessor()
        // Register plugin processors and cache configured with the specified storage provider.
        .AddSingleton(serviceProvider => new PluginFileProcessor(
            serviceProvider.GetRequiredService<ILogger<PluginFileProcessor>>(),
            serviceProvider.GetRequiredKeyedService<IFileStorageService>(pluginSettings.StorageProvider),
            serviceProvider.GetRequiredService<PluginCache>(),
            serviceProvider.GetRequiredService<PluginSettings>()))
        .AddSingleton(serviceProvider => new PluginAssemblyProcessor(
            serviceProvider.GetServices<IPluginTypeProcessor>(),
            serviceProvider.GetRequiredService<TimeProvider>(),
            serviceProvider.GetRequiredService<PluginFileProcessor>(),
            serviceProvider.GetRequiredService<PluginCache>()))
        .AddSingleton<PluginCache>()
        // Register validators.
        .AddSingleton<PluginValidator<GetPluginsRequest>>()
        .AddSingleton<PluginValidator<ReloadPluginsRequest>>()
        .AddSingleton<PluginValidator>()
        .AddSingleton<UploadPluginValidator>()
        // Register plugin cache persistences as keyed services. The key matches
        // the {target} route segment on cache-persist/restore endpoints. Consumers
        // of this package can register additional implementations under their own
        // keys (e.g. services.AddKeyedSingleton<IPluginCachePersistence, RedisPluginCachePersistence>("redis");)
        // and the same endpoints will route to them without further changes.
        .AddKeyedSingleton<IPluginCachePersistence>(
            PluginCacheTargets.File,
            (serviceProvider, key) => new JsonFilePluginCachePersistence(
                serviceProvider.GetRequiredKeyedService<IFileStorageService>(pluginSettings.StorageProvider),
                serviceProvider.GetRequiredService<PluginSettings>()))
        .AddKeyedSingleton<IPluginCachePersistence, DatabasePluginCachePersistence>(PluginCacheTargets.Database)
        // Non-keyed IPluginCachePersistence resolves the correct keyed implementation
        // per request, based on the {target} segment of the current route. This is
        // what PluginService and any other consumer gets through normal constructor
        // injection.
        .AddScoped((serviceProvider) =>
        {
            IHttpContextAccessor accessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
            PluginSettings settings = serviceProvider.GetRequiredService<PluginSettings>();
            // Outside an HTTP request (e.g. inside the plugin orchestrator background service)
            // there is no {target} route value; fall back to the configured default so that
            // IPluginService can still be resolved and call LoadPluginsOnRuntime.
            object target = accessor.HttpContext?.Request.RouteValues["target"] ?? settings.DefaultCacheTarget;
            return serviceProvider.GetRequiredKeyedService<IPluginCachePersistence>(target);
        })
        // Plugin cache operations run inside an HTTP request, so the scoped
        // persistence factory above resolves to the right implementation.
        // PluginService must be scoped as well to avoid capturing a scoped
        // dependency from a wider lifetime.
        .AddScoped<IPluginService, PluginService>()
        // Register plugin operations.
        .AddScoped<GetPlugins>()
        .AddScoped<LoadPlugins>()
        .AddScoped<ReloadPlugins>()
        .AddScoped<ClearPluginCache>()
        .AddScoped<PersistPluginCache>()
        .AddScoped<RestorePluginCache>()
        .AddScoped<UploadPlugin>((IServiceProvider serviceProvider) => new UploadPlugin(
            serviceProvider.GetRequiredService<ILogger<UploadPlugin>>(),
            serviceProvider.GetRequiredKeyedService<IFileStorageService>(pluginSettings.StorageProvider),
            serviceProvider.GetRequiredService<UploadPluginValidator>(),
            serviceProvider.GetRequiredService<PluginSettings>()))
        // Background orchestrator: polls the plugin folder, promotes newer versions at runtime
        // and evicts obsolete cached versions according to the configured retention policy.
        .AddHostedService<PluginOrchestratorBackgroundService>();

    /// <summary>
    /// Registers plugin management services into <paramref name="services"/> and asynchronously eagerly loads discovered plugins at startup.
    /// </summary>
    /// <param name="services">Service collection that receives the registrations.</param>
    /// <param name="pluginSettings">Settings that describe plugin folders and behavior.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the registration and startup loading operation.</returns>
    public static async Task<IServiceCollection> AddPluginServicesAsync(
        this IServiceCollection services,
        PluginSettings pluginSettings,
        CancellationToken cancellationToken = default)
    {
        services.AddPluginServices(pluginSettings);

        using ServiceProvider bootstrapProvider = services.BuildServiceProvider();
        using IServiceScope bootstrapScope = bootstrapProvider.CreateScope();
        await bootstrapScope.ServiceProvider
            .GetRequiredService<IPluginService>()
            .LoadPluginsOnStartupAsync(services, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return services;
    }
}
