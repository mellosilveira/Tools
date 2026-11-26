using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Security;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Plugins.Application.Commands.Cache;
using MelloSilveiraTools.Plugins.Application.Commands.Get;
using MelloSilveiraTools.Plugins.Application.Commands.Load;
using MelloSilveiraTools.Plugins.Application.Commands.Reload;
using MelloSilveiraTools.Plugins.Application.Commands.Submissions;
using MelloSilveiraTools.Plugins.Application.Commands.Submissions.Cancel;
using MelloSilveiraTools.Plugins.Application.Commands.Submissions.Get;
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
    /// Registers plugin infrastructure, caching, providers, community submission/moderation commands, and plugin operations.
    /// </summary>
    /// <param name="services">Service collection that receives the registrations.</param>
    /// <param name="pluginSettings">Settings that describe plugin folders, limits, and behavior.</param>
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
            serviceProvider.GetRequiredService<PluginCache>(),
            serviceProvider.GetRequiredService<PluginSettings>()))
        .AddSingleton<PluginCache>()
        .AddSingleton(serviceProvider => new PluginSubmissionPersistence(
            serviceProvider.GetRequiredService<ILogger<PluginSubmissionPersistence>>(),
            serviceProvider.GetRequiredKeyedService<IFileStorageService>(pluginSettings.StorageProvider),
            serviceProvider.GetRequiredService<IRepository>(),
            serviceProvider.GetRequiredService<PluginSettings>()))
        // Register validators.
        .AddSingleton<PluginValidator<GetPluginsRequest>>()
        .AddSingleton<PluginValidator<InspectPluginsRequest>>()
        .AddSingleton<PluginValidator<ReloadPluginsRequest>>()
        .AddSingleton<PluginValidator>()
        .AddSingleton<UploadPluginValidator>()
        // Register plugin cache persistences as keyed services.
        .AddKeyedSingleton<IPluginCachePersistence>(
            PluginCacheTargets.File,
            (serviceProvider, key) => new JsonFilePluginCachePersistence(
                serviceProvider.GetRequiredKeyedService<IFileStorageService>(pluginSettings.StorageProvider),
                serviceProvider.GetRequiredService<PluginSettings>()))
        .AddKeyedSingleton<IPluginCachePersistence, DatabasePluginCachePersistence>(PluginCacheTargets.Database)
        // Non-keyed IPluginCachePersistence resolves the correct keyed implementation
        // per request, based on the {target} segment of the current route.
        .AddScoped((serviceProvider) =>
        {
            IHttpContextAccessor accessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
            PluginSettings settings = serviceProvider.GetRequiredService<PluginSettings>();
            object target = accessor.HttpContext?.Request.RouteValues["target"] ?? settings.DefaultCacheTarget;
            return serviceProvider.GetRequiredKeyedService<IPluginCachePersistence>(target);
        })
        .AddScoped<IPluginService, PluginService>()
        // Register plugin operations and community submission moderation commands.
        .AddScoped<GetPlugins>()
        .AddScoped<InspectPlugins>()
        .AddScoped<LoadPlugins>()
        .AddScoped<ReloadPlugins>()
        .AddScoped<ClearPluginCache>()
        .AddScoped<PersistPluginCache>()
        .AddScoped<RestorePluginCache>()
        .AddScoped<GetPluginSubmissions>()
        .AddScoped<SubmitPlugin>()
        .AddScoped<UploadPlugin>()
        .AddScoped(serviceProvider => new ApprovePluginSubmission(
            serviceProvider.GetRequiredService<ILogger<ApprovePluginSubmission>>(),
            serviceProvider.GetRequiredKeyedService<IFileStorageService>(pluginSettings.StorageProvider),
            serviceProvider.GetRequiredService<ICurrentUserProvider>(),
            serviceProvider.GetRequiredService<IRepository>(),
            serviceProvider.GetRequiredService<IPluginService>(),
            serviceProvider.GetRequiredService<TimeProvider>(),
            serviceProvider.GetRequiredService<PluginFileProcessor>(),
            serviceProvider.GetRequiredService<PluginSubmissionPersistence>()))
        .AddScoped(serviceProvider => new RejectPluginSubmission(
            serviceProvider.GetRequiredService<ILogger<RejectPluginSubmission>>(),
            serviceProvider.GetRequiredKeyedService<IFileStorageService>(pluginSettings.StorageProvider),
            serviceProvider.GetRequiredService<ICurrentUserProvider>(),
            serviceProvider.GetRequiredService<IRepository>(),
            serviceProvider.GetRequiredService<TimeProvider>(),
            serviceProvider.GetRequiredService<PluginFileProcessor>()))
        .AddScoped(serviceProvider => new CancelPluginSubmission(
            serviceProvider.GetRequiredService<ILogger<CancelPluginSubmission>>(),
            serviceProvider.GetRequiredKeyedService<IFileStorageService>(pluginSettings.StorageProvider),
            serviceProvider.GetRequiredService<ICurrentUserProvider>(),
            serviceProvider.GetRequiredService<IRepository>()))
        // Background orchestrator: polls the incoming/ folder (never pending/), promotes newer versions at runtime,
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
