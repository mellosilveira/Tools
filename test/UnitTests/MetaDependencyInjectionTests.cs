using MelloSilveiraTools;
using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Files.Providers.Local;
using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Pipelines.Notifiers;
using MelloSilveiraTools.Core.ResiliencePipelines;
using MelloSilveiraTools.Database.RelationalDatabase.Settings;
using MelloSilveiraTools.Database.RelationalDatabase.Sql.Provider;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Mathematics.NumericalMethods.Integrals;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Elasticity;
using MelloSilveiraTools.Plugins.Infrastructure;
using MelloSilveiraTools.WebApi.Infrastructure.ResiliencePipelines;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace UnitTests;

/// <summary>
/// Unit tests for <see cref="DependencyInjection.AddToolsServices"/> verifying meta-package registration
/// across Core, Database, WebApi, Plugins, Mathematics, MechanicsOfMaterials, and Optimizations.
/// </summary>
public sealed class MetaDependencyInjectionTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), $"MetaDiTests_{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            try
            {
                Directory.Delete(_tempDirectory, true);
            }
            catch
            {
                // Best effort cleanup of temporary test directory
            }
        }
    }

    private static ResiliencePipelineSettings CreateResilienceSettings() => new()
    {
        BackoffType = DelayBackoffType.Constant,
        MaxRetryAttempts = 1,
        DelayInMilliseconds = 1,
        UseJitter = false
    };

    /// <summary>
    /// Verifies that AddToolsServices with minimal required settings registers Core, Mathematics,
    /// MechanicsOfMaterials, Optimizations, and WebApi services without optional Database or Plugin registrations.
    /// </summary>
    [Fact]
    public void AddToolsServices_WithMinimalSettings_RegistersExpectedPackageServices()
    {
        // Arrange
        ServiceCollection services = new();
        ResiliencePipelineSettings resilienceSettings = CreateResilienceSettings();

        // Act
        IServiceCollection returned = services.AddToolsServices(resilienceSettings);
        ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        Assert.Same(services, returned);
        Assert.Same(resilienceSettings, provider.GetService<ResiliencePipelineSettings>());
        Assert.NotNull(provider.GetService<PipelineFactory>());
        Assert.NotNull(provider.GetService<IPipelineProgressNotifier>());
        Assert.NotNull(provider.GetService<IIntegration>());
        Assert.NotNull(provider.GetService<IMechanicalModelCalculatorFactory>());
        Assert.NotNull(provider.GetService<IElasticModelCalculator>());
        Assert.NotNull(provider.GetService<ApiServiceAgentResiliencePipeline>());
        Assert.Null(provider.GetService<IRepository>());
        Assert.Null(provider.GetService<PluginSettings>());
    }

    /// <summary>
    /// Verifies that AddToolsServices registers Database and Plugin services when optional settings are supplied,
    /// and skips mechanical model calculators when addMechanicalModels is false.
    /// </summary>
    [Fact]
    public void AddToolsServices_WithDatabaseAndPluginSettings_RegistersAllContextualServices()
    {
        // Arrange
        Directory.CreateDirectory(_tempDirectory);
        ServiceCollection services = new();
        ResiliencePipelineSettings resilienceSettings = CreateResilienceSettings();
        DatabaseSettings databaseSettings = new()
        {
            ConnectionString = "Host=localhost;Database=tools_test;Username=postgres;Password=postgres;",
            UnitOperationTimeoutInMilliseconds = 5000,
            BulkOperationTimeoutInMilliseconds = 30000
        };
        PluginSettings pluginSettings = new()
        {
            StorageProvider = StorageProvider.Local,
            Directory = _tempDirectory,
            PollInterval = TimeSpan.FromMinutes(1),
            PreviousVersionRetention = TimeSpan.FromMinutes(5),
            DefaultCacheTarget = "file"
        };
        FileStorageSettings fileStorageSettings = new()
        {
            Local = new LocalFileStorageSettings { BaseDirectory = _tempDirectory }
        };

        // Act
        services.AddToolsServices(
            defaultResiliencePipelineSettings: resilienceSettings,
            databaseSettings: databaseSettings,
            pluginSettings: pluginSettings,
            fileStorageSettings: fileStorageSettings,
            addMechanicalModels: false);
        ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        Assert.Same(databaseSettings, provider.GetService<DatabaseSettings>());
        Assert.NotNull(provider.GetService<ISqlProvider>());
        Assert.NotNull(provider.GetService<IRepository>());
        Assert.Same(pluginSettings, provider.GetService<PluginSettings>());
        Assert.NotNull(provider.GetService<PluginCache>());
        Assert.NotNull(provider.GetService<IMechanicalModelCalculatorFactory>());
        Assert.Null(provider.GetService<IElasticModelCalculator>());
    }
}
