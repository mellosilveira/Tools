using MelloSilveiraTools.Core.Caching;
using MelloSilveiraTools.Core.Providers.Dynamics;
using MelloSilveiraTools.Plugins.Application.Commands.Cache;
using MelloSilveiraTools.Plugins.Application.Commands.Get;
using MelloSilveiraTools.Plugins.Application.Commands.Load;
using MelloSilveiraTools.Plugins.Application.Commands.Reload;
using MelloSilveiraTools.Plugins.Infrastructure;
using MelloSilveiraTools.Plugins.Infrastructure.Persistences;
using MelloSilveiraTools.Plugins.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

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
    public static IServiceCollection AddPluginServices(this IServiceCollection services, PluginSettings pluginSettings)
    {
        services
            // Register settings.
            .AddSingleton(pluginSettings)
            // Required so plugin cache operations can read the {target} route value.
            .AddHttpContextAccessor()
            // Register caching.
            .AddSingleton<ISingleLevelCache, InMemorySingleLevelCache>()
            .AddSingleton<ITwoLevelCache, InMemoryTwoLevelCache>()
            // Register dynamic service provider (runtime plugin service registration).
            .AddSingleton(services)
            .AddSingleton<IDynamicServiceProvider, InMemoryDynamicServiceProvider>()
            // Register plugin processors and cache.
            .AddSingleton<PluginCache>()
            .AddSingleton<PluginFileProcessor>()
            .AddSingleton<PluginAssemblyProcessor>()
            // Register plugin cache persistences as keyed services. The key matches
            // the {target} route segment on cache-persist/restore endpoints. Consumers
            // of this package can register additional implementations under their own
            // keys (e.g. services.AddKeyedSingleton<IPluginCachePersistence, RedisPluginCachePersistence>("redis");)
            // and the same endpoints will route to them without further changes.
            .AddKeyedSingleton<IPluginCachePersistence, JsonFilePluginCachePersistence>(PluginCacheTargets.File)
            .AddKeyedSingleton<IPluginCachePersistence, DatabasePluginCachePersistence>(PluginCacheTargets.Database)
            // Non-keyed IPluginCachePersistence resolves the correct keyed implementation
            // per request, based on the {target} segment of the current route. This is
            // what PluginService and any other consumer gets through normal constructor
            // injection.
            .AddScoped(serviceProvider =>
            {
                var accessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
                var settings = serviceProvider.GetRequiredService<PluginSettings>();
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
            // Background orchestrator: polls the plugin folder, promotes newer versions at runtime
            // and evicts obsolete cached versions according to the configured retention policy.
            .AddHostedService<PluginOrchestratorBackgroundService>();

        // Eagerly load plugins discovered on disk into the same IServiceCollection that the host
        // is being built with. This must happen here — not from an IApplicationBuilder hook —
        // because LoadPluginsOnStartup registers plugin types via PluginRegistrationContext.ForStartup
        // which mutates IServiceCollection. Once the host is built (services.BuildServiceProvider()
        // is called by the framework), the collection is sealed and any later registration is a no-op.
        //
        // We build a temporary ServiceProvider from the current registrations, resolve the services
        // needed by PluginService, run the startup load (which writes back into `services`) and
        // dispose the temporary provider. The runtime container the host eventually builds will
        // observe every registration the plugins added.
        using ServiceProvider bootstrapProvider = services.BuildServiceProvider();
        using IServiceScope bootstrapScope = bootstrapProvider.CreateScope();
        bootstrapScope.ServiceProvider.GetRequiredService<IPluginService>().LoadPluginsOnStartup();

        return services;
    }
}
