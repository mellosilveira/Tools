using MelloSilveiraTools.Core;
using MelloSilveiraTools.Core.Logging;
using MelloSilveiraTools.Core.ResiliencePipelines;
using MelloSilveiraTools.Core.Services.Email;
using MelloSilveiraTools.Core.Services.Encryption;
using MelloSilveiraTools.Database;
using MelloSilveiraTools.Database.RelationalDatabase.Settings;
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
    /// <param name="emailSettings">Settings used to send e-mails through an SMTP server.</param>
    /// <param name="loggerSettings">Settings used by logger service.</param>
    /// <param name="useDefaultLogger">Indicates if should use the default logger.</param>
    /// <param name="addMechanicalModels"></param>
    /// <returns>The same <paramref name="services"/> instance to allow call chaining.</returns>
    public static IServiceCollection AddToolsServices(this IServiceCollection services,
        DatabaseSettings databaseSettings,
        EncryptionSettings encryptionSettings,
        ResiliencePipelineSettings resiliencePipelineSettings,
        PluginSettings pluginSettings,
        EmailSettings? emailSettings = null,
        LoggerSettings? loggerSettings = null,
        bool useDefaultLogger = true,
        bool addMechanicalModels = false)
        => services
            .AddCoreServices(encryptionSettings, resiliencePipelineSettings, emailSettings, loggerSettings, useDefaultLogger)
            .AddDatabaseServices(databaseSettings, resiliencePipelineSettings)
            .AddMathematicsServices()
            .AddMechanicsOfMaterialsServices(addMechanicalModels)
            .AddPluginServices(pluginSettings)
            .AddWebApiServices(resiliencePipelineSettings);
}
