using MelloSilveiraTools.Core;
using MelloSilveiraTools.Core.Logging;
using MelloSilveiraTools.Core.Managers.File.Providers;
using MelloSilveiraTools.Core.Pipelines.Notifiers;
using MelloSilveiraTools.Core.ResiliencePipelines;
using MelloSilveiraTools.Core.Services.Email;
using MelloSilveiraTools.Core.Services.Encryption;
using MelloSilveiraTools.Database;
using MelloSilveiraTools.Database.RelationalDatabase.Settings;
using MelloSilveiraTools.Mathematics;
using MelloSilveiraTools.MechanicsOfMaterials;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep;
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
    /// All configuration settings are optional.
    /// </summary>
    /// <param name="services">Service collection that receives the registrations.</param>
    /// <param name="defaultResiliencePipelineSettings">Optional settings that parameterize the resilience pipelines.</param>
    /// <param name="databaseSettings">Optional database connection and behavior settings.</param>
    /// <param name="encryptionSettings">Optional settings used by the encryption service.</param>
    /// <param name="pluginSettings">Optional settings that describe plugin folders and behavior.</param>
    /// <param name="emailSettings">Optional settings used to send e-mails through an SMTP server.</param>
    /// <param name="fileStorageSettings">Optional settings used to configure root directories and storage policies.</param>
    /// <param name="loggerSettings">Optional settings used by logger service.</param>
    /// <param name="curveFitterOptions">Optional curve fitter configuration options controlling deterministic seeding and default fitter engine.</param>
    /// <param name="experimentalDataSettings">Optional settings used by experimental data processing and optimization pipelines.</param>
    /// <param name="parametricSweepSettings">Optional settings used by parametric sweep sensitivity analysis pipelines.</param>
    /// <param name="pipelineProgressNotifierSettings">Optional configuration settings for real-time progress notifications.</param>
    /// <param name="addMechanicalModels">Indicates whether to register mechanical model calculators. Defaults to true.</param>
    /// <returns>The same <paramref name="services"/> instance to allow call chaining.</returns>
    public static IServiceCollection AddToolsServices(this IServiceCollection services,
        ResiliencePipelineSettings defaultResiliencePipelineSettings,
        DatabaseSettings? databaseSettings = null,
        EncryptionSettings? encryptionSettings = null,
        PluginSettings? pluginSettings = null,
        EmailSettings? emailSettings = null,
        FileStorageSettings? fileStorageSettings = null,
        LoggerSettings? loggerSettings = null,
        CurveFitterSettings? curveFitterOptions = null,
        ExperimentalDataSettings? experimentalDataSettings = null,
        ParametricSweepSettings? parametricSweepSettings = null,
        PipelineProgressNotifierSettings? pipelineProgressNotifierSettings = null,
        bool addMechanicalModels = true)
    {
        if (databaseSettings is not null)
            services.AddDatabaseServices(defaultResiliencePipelineSettings, databaseSettings);

        if (pluginSettings is not null)
            services.AddPluginServices(pluginSettings);

        return services
            .AddSingleton(defaultResiliencePipelineSettings)
            .AddCoreServices(defaultResiliencePipelineSettings, encryptionSettings, emailSettings, fileStorageSettings, loggerSettings, pipelineProgressNotifierSettings)
            .AddMathematicsServices()
            .AddMechanicsOfMaterialsServices(addMechanicalModels)
            .AddOptimizationsServices(curveFitterOptions, experimentalDataSettings, parametricSweepSettings)
            .AddWebApiServices(defaultResiliencePipelineSettings);
    }
}
