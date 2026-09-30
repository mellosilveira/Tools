using MelloSilveiraTools.Core;
using MelloSilveiraTools.Core.Logging;
using MelloSilveiraTools.Core.ResiliencePipelines;
using MelloSilveiraTools.Database.Logging;
using MelloSilveiraTools.Database.RelationalDatabase.Repositories;
using MelloSilveiraTools.Database.RelationalDatabase.Settings;
using MelloSilveiraTools.Database.RelationalDatabase.Sql.Provider;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Database.ResiliencePipelines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.Database;

/// <summary>
/// Provides extension methods to register services from the MelloSilveiraTools.Database package.
/// </summary>
public static class DatabaseDependencyInjection
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers database services: settings, Postgres resilience pipeline, SQL provider and repository.
        /// </summary>
        /// <param name="databaseSettings">Database connection and behavior settings.</param>
        /// <param name="resiliencePipelineSettings">Settings that parameterize the resilience pipelines.</param>
        /// <param name="loggerSettings">Settings used by logger service.</param>
        /// <returns>The same <paramref name="services"/> instance to allow call chaining.</returns>
        public IServiceCollection AddDatabaseServices(DatabaseSettings databaseSettings,
            ResiliencePipelineSettings resiliencePipelineSettings,
            LoggerSettings? loggerSettings = null)
            => services
                // Register logger.
                .AddDatabaseLogging(loggerSettings ?? new LoggerSettings())
                // Register settings.
                .AddSingleton(databaseSettings)
                .AddSingleton(resiliencePipelineSettings)
                // Register resilience pipelines.
                .AddSingleton(provider => new PostgresResiliencePipeline(provider.GetRequiredService<ILogger<PostgresResiliencePipeline>>(), resiliencePipelineSettings))
                // Register SQL providers.
                .AddSingleton<ISqlProvider, PostgresSqlProvider>()
                // Register repositories.
                .AddSingleton<IRepository, PostgresRepository>();

        public IServiceCollection AddDatabaseLogging(LoggerSettings loggerSettings)
        {
            var loggerConfiguration = LoggerConfigurationExtensions.Create().WriteToPostgres(loggerSettings);
            return services.AddCoreLogging(loggerConfiguration, loggerSettings);
        }
    }
}
