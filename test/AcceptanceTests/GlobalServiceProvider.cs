using MelloSilveiraTools;
using MelloSilveiraTools.Core.Managers.File.Models;
using MelloSilveiraTools.Core.ResiliencePipelines;
using MelloSilveiraTools.Core.Services.Encryption;
using MelloSilveiraTools.Database.RelationalDatabase.Settings;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Factories;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter.Fung;
using MelloSilveiraTools.Plugins.Infrastructure;
using MelloSilveiraTools.Plugins.Infrastructure.Persistences;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace AcceptanceTests;

/// <summary>
/// Central dependency injection container for all acceptance tests. 
/// Created exactly once per application domain (eliminates duplication across generic types).
/// </summary>
public static class GlobalServiceProvider
{
    public static ServiceProvider Provider { get; }

    static GlobalServiceProvider()
    {
        string dbConnectionString = Environment.GetEnvironmentVariable("TEST_DB_CONNECTION_STRING") ?? "Host=localhost;Database=tests;Username=postgres;Password=postgres";
        string pluginsDir = Environment.GetEnvironmentVariable("TEST_PLUGINS_DIRECTORY") ?? Path.Combine(AppContext.BaseDirectory, "plugins");

        if (!Directory.Exists(pluginsDir))
        {
            Directory.CreateDirectory(pluginsDir);
        }

        IServiceCollection services = new ServiceCollection();
        services.AddLogging();
        services.AddToolsServices(
            databaseSettings: new DatabaseSettings
            {
                ConnectionString = dbConnectionString,
                UnitOperationTimeoutInMilliseconds = 30000,
                BulkOperationTimeoutInMilliseconds = 120000
            },
            encryptionSettings: new EncryptionSettings
            {
                SaltSize = 16,
                HashSize = 32,
                Iterations = 100_000
            },
            defaultResiliencePipelineSettings: new ResiliencePipelineSettings
            {
                BackoffType = DelayBackoffType.Constant,
                DelayInMilliseconds = 100,
                MaxRetryAttempts = 3,
                UseJitter = false
            },
            pluginSettings: new PluginSettings
            {
                StorageProvider = StorageProvider.Local,
                Directory = pluginsDir,
                DefaultCacheTarget = PluginCacheTargets.File,
                PollInterval = TimeSpan.FromSeconds(30),
                PreviousVersionRetention = TimeSpan.FromHours(24)
            },
            addMechanicalModels: true);
        services.AddTransient(sp => sp.GetRequiredKeyedService<ICurveFitter>(CurveFitterType.Alglib));
        services.AddTransient(sp => (FungRelaxationOnlyCurveFitterStep)sp.GetRequiredService<IMechanicalModelStepFactory>().Create(nameof(MechanicalModel.Fung), [SegmentType.Relaxation], CurveFitterType.Alglib, 0.90));
        services.AddTransient(sp => (SimplifiedFungRelaxationOnlyCurveFitterStep)sp.GetRequiredService<IMechanicalModelStepFactory>().Create(nameof(MechanicalModel.SimplifiedFung), [SegmentType.Relaxation], CurveFitterType.Alglib, 0.90));
        services.AddTransient(sp => (SchaperyRelaxationOnlyCurveFitterStep)sp.GetRequiredService<IMechanicalModelStepFactory>().Create(nameof(MechanicalModel.Schapery), [SegmentType.Relaxation], CurveFitterType.Alglib, 0.90));

        Provider = services.BuildServiceProvider();
    }
}
