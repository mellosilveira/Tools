using MelloSilveiraTools.Core.Logger;
using MelloSilveiraTools.Core.ResiliencePipelines;
using MelloSilveiraTools.Database.RelationalDatabase.Repositories;
using MelloSilveiraTools.Database.RelationalDatabase.Settings;
using MelloSilveiraTools.Database.RelationalDatabase.Sql.Provider;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Database.ResiliencePipelines;
using Microsoft.Extensions.DependencyInjection;

namespace MelloSilveiraTools.Database;

/// <summary>
/// Provides extension methods to register services from the MelloSilveiraTools.Database package.
/// </summary>
public static class DatabaseDependencyInjection
{
    /// <summary>
    /// Registers database services: settings, Postgres resilience pipeline, SQL provider and repository.
    /// </summary>
    /// <param name="services">Service collection that receives the registrations.</param>
    /// <param name="databaseSettings">Database connection and behavior settings.</param>
    /// <param name="resiliencePipelineSettings">Settings that parameterize the resilience pipelines.</param>
    /// <returns>The same <paramref name="services"/> instance to allow call chaining.</returns>
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services,
        DatabaseSettings databaseSettings, ResiliencePipelineSettings resiliencePipelineSettings)
        => services
            // Register settings.
            .AddSingleton(databaseSettings)
            .AddSingleton(resiliencePipelineSettings)
            // Register resilience pipelines.
            .AddSingleton(provider => new PostgresResiliencePipeline(provider.GetRequiredService<ILogger>(), resiliencePipelineSettings))
            // Register SQL providers.
            .AddSingleton<ISqlProvider, PostgresSqlProvider>()
            // Register repositories.
            .AddSingleton<IRepository, PostgresRepository>();
}
