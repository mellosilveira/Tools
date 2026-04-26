using MelloSilveiraTools.Core;
using MelloSilveiraTools.Core.Infrastructure.Logger;
using MelloSilveiraTools.Core.Infrastructure.ResiliencePipelines;
using MelloSilveiraTools.Core.Infrastructure.Services.Encryption;
using MelloSilveiraTools.Database;
using MelloSilveiraTools.Database.Infrastructure.Database.Settings;
using MelloSilveiraTools.Mathematics;
using MelloSilveiraTools.MechanicsOfMaterials;
using MelloSilveiraTools.Plugins;
using MelloSilveiraTools.Plugins.Infrastructure;
using MelloSilveiraTools.WebApi;
using Microsoft.Extensions.DependencyInjection;

namespace MelloSilveiraTools;

/// <summary>
/// Provides extension methods to dependency injection of Tools project (meta-package).
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the services of every contextual MelloSilveiraTools package
    /// (Core, Database, WebApi and Plugins).
    /// </summary>
    /// <param name="services">Service collection that receives the registrations.</param>
    /// <param name="databaseSettings">Database connection and behavior settings.</param>
    /// <param name="encryptionSettings">Settings used by the encryption service.</param>
    /// <param name="resiliencePipelineSettings">Settings that parameterize the resilience pipelines.</param>
    /// <param name="pluginSettings">Settings that describe plugin folders and behavior.</param>
    /// <param name="loggerSettings">Settings used by logger service.</param>
    /// <returns>The same <paramref name="services"/> instance to allow call chaining.</returns>
    public static IServiceCollection AddToolsServices(this IServiceCollection services, DatabaseSettings databaseSettings,
        EncryptionSettings encryptionSettings, ResiliencePipelineSettings resiliencePipelineSettings, PluginSettings pluginSettings, LoggerSettings? loggerSettings = null)
        => services
            .AddCoreServices(encryptionSettings, resiliencePipelineSettings, loggerSettings)
            .AddDatabaseServices(databaseSettings, resiliencePipelineSettings)
            .AddMathematicsServices()
            .AddMechanicsOfMaterialsServices()
            .AddPluginServices(pluginSettings)
            .AddWebApiServices(resiliencePipelineSettings);
}
