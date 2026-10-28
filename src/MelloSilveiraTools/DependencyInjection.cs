using MelloSilveiraTools.Core;
using MelloSilveiraTools.Core.Logging;
using MelloSilveiraTools.Core.Managers.File.Providers;
using MelloSilveiraTools.Core.Pipelines.Telemetry;
using MelloSilveiraTools.Core.ResiliencePipelines;
using MelloSilveiraTools.Core.Services.Email;
using MelloSilveiraTools.Core.Services.Encryption;
using MelloSilveiraTools.Database;
using MelloSilveiraTools.Database.RelationalDatabase.Settings;
using MelloSilveiraTools.Mathematics;
using MelloSilveiraTools.MechanicsOfMaterials;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData;
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
    /// (Core, Database, WebApi, Plugins, Mathematics, MechanicsOfMaterials, and Optimizations).
    /// </summary>
    /// <param name="services">Service collection that receives the registrations.</param>
    /// <param name="defaultResiliencePipelineSettings">Settings that parameterize the resilience pipelines.</param>
    /// <param name="databaseSettings">Database connection and behavior settings.</param>
    /// <param name="encryptionSettings">Settings used by the encryption service.</param>
    /// <param name="pluginSettings">Settings that describe plugin folders and behavior.</param>
    /// <param name="emailSettings">Settings used to send e-mails through an SMTP server.</param>
    /// <param name="loggerSettings">Settings used by logger service.</param>
    /// <param name="fileStorageSettings">Settings used to configure root directories and storage policies.</param>
    /// <param name="experimentalDataSettings">Settings used by experimental data processing and optimization pipelines.</param>
    /// <param name="addMechanicalModels">Indicates whether to register mechanical model calculators.</param>
    /// <param name="enablePipelineProgressNotifications">Indicates whether to enable real-time pipeline progress notifications via <see cref="MelloSilveiraTools.Core.Pipelines.Notifiers.ChannelPipelineProgressNotifier"/>.</param>
    /// <returns>The same <paramref name="services"/> instance to allow call chaining.</returns>
    public static IServiceCollection AddToolsServices(this IServiceCollection services,
        ResiliencePipelineSettings defaultResiliencePipelineSettings,
        DatabaseSettings databaseSettings,
        EncryptionSettings encryptionSettings,
        PluginSettings pluginSettings,
        EmailSettings? emailSettings = null,
        FileStorageSettings? fileStorageSettings = null,
        ExperimentalDataSettings? experimentalDataSettings = null,
        LoggerSettings? loggerSettings = null,
        bool addMechanicalModels = false,
        bool enablePipelineProgressNotifications = false)
        => services
            .AddSingleton(defaultResiliencePipelineSettings)
            .AddCoreServices(defaultResiliencePipelineSettings, encryptionSettings, emailSettings, fileStorageSettings, loggerSettings, enablePipelineProgressNotifications)
            .AddDatabaseServices(defaultResiliencePipelineSettings, databaseSettings)
            .AddMathematicsServices()
            .AddMechanicsOfMaterialsServices(addMechanicalModels)
            .AddOptimizationsServices(experimentalDataSettings)
            .AddPluginServices(pluginSettings)
            .AddWebApiServices(defaultResiliencePipelineSettings);
}
