using MelloSilveiraTools.Core.ResiliencePipelines;
using MelloSilveiraTools.Database.RelationalDatabase.Repositories;
using MelloSilveiraTools.Database.RelationalDatabase.Settings;
using MelloSilveiraTools.Database.RelationalDatabase.Sql.Provider;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Database.ResiliencePipelines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace MelloSilveiraTools.Database;

/// <summary>
/// Provides extension methods to register services from the MelloSilveiraTools.Database package.
/// </summary>
public static class DatabaseDependencyInjection
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers database services as singletons: <see cref="DatabaseSettings"/>, an AOT-friendly
        /// <see cref="NpgsqlDataSource"/> built via <see cref="NpgsqlSlimDataSourceBuilder"/>,
        /// <see cref="PostgresResiliencePipeline"/>, <see cref="ISqlProvider"/>, and <see cref="IRepository"/>.
        /// The DI container owns the <see cref="NpgsqlDataSource"/> lifetime and disposes its connection pool on shutdown.
        /// </summary>
        /// <remarks>
        /// <see cref="NpgsqlDataSource"/> is registered explicitly via <see cref="CreateDataSource"/> instead of
        /// <c>AddNpgsqlDataSource</c> for four architectural reasons:
        /// <list type="number">
        /// <item><description><b>Minimal dependencies:</b> on Npgsql 10.x, <c>AddNpgsqlDataSource</c> requires the separate <c>Npgsql.DependencyInjection</c> NuGet package.</description></item>
        /// <item><description><b>Native AOT &amp; trimming:</b> <c>AddNpgsqlDataSource</c> instantiates the full <see cref="NpgsqlDataSourceBuilder"/>, pulling default codecs (geometry, network, full-text search, etc.), whereas <see cref="CreateDataSource"/> uses the pay-per-play <see cref="NpgsqlSlimDataSourceBuilder"/>.</description></item>
        /// <item><description><b>RLS &amp; resilience boundary:</b> <c>AddNpgsqlDataSource</c> also registers <see cref="NpgsqlConnection"/> and <c>DbConnection</c> directly in DI, which could encourage bypassing <see cref="PostgresRepository"/> and its transactional Row-Level Security (<c>SET LOCAL</c>) + Polly v8 execution pipeline.</description></item>
        /// <item><description><b>Same DI lifetime &amp; test reuse:</b> registering the factory as a singleton gives the DI container full ownership of disposal on shutdown while allowing tests to call <see cref="CreateDataSource"/> directly without building a service provider.</description></item>
        /// </list>
        /// </remarks>
        /// <param name="defaultResiliencePipelineSettings">Settings that parameterize the default resilience pipelines.</param>
        /// <param name="databaseSettings">Database connection and behavior settings.</param>
        /// <returns>The same <paramref name="services"/> instance to allow call chaining.</returns>
        public IServiceCollection AddDatabaseServices(ResiliencePipelineSettings defaultResiliencePipelineSettings, DatabaseSettings databaseSettings) => services
            // Register settings.
            .AddSingleton(databaseSettings)
            // Register NpgsqlDataSource directly via NpgsqlSlimDataSourceBuilder instead of AddNpgsqlDataSource:
            // avoids the extra Npgsql.DependencyInjection package (Npgsql 10.x), stays slim/AOT-friendly,
            // prevents exposing raw NpgsqlConnection/DbConnection in DI outside PostgresRepository's RLS/Polly scope,
            // and lets DI own disposal while reusing CreateDataSource in tests.
            .AddSingleton(provider => CreateDataSource(databaseSettings, provider.GetService<ILoggerFactory>()))
            // Register resilience pipelines.
            .AddSingleton(provider => new PostgresResiliencePipeline(provider.GetRequiredService<ILogger<PostgresResiliencePipeline>>(), databaseSettings.ResiliencePipelineSettings ?? defaultResiliencePipelineSettings))
            // Register SQL providers.
            .AddSingleton<ISqlProvider, PostgresSqlProvider>()
            // Register repositories.
            .AddSingleton<IRepository, PostgresRepository>();
    }

    /// <summary>
    /// Creates an AOT-compatible <see cref="NpgsqlDataSource"/> using <see cref="NpgsqlSlimDataSourceBuilder"/>
    /// with TLS/SSL transport security and PostgreSQL array support enabled, wired into the supplied
    /// <see cref="ILoggerFactory"/> while keeping parameter logging disabled for security.
    /// </summary>
    /// <param name="databaseSettings">Database settings containing the PostgreSQL connection string.</param>
    /// <param name="loggerFactory">Optional logger factory for Npgsql connection and pool diagnostics.</param>
    /// <returns>A configured <see cref="NpgsqlDataSource"/> instance.</returns>
    public static NpgsqlDataSource CreateDataSource(DatabaseSettings databaseSettings, ILoggerFactory? loggerFactory = null)
    {
        NpgsqlSlimDataSourceBuilder dataSourceBuilder = new NpgsqlSlimDataSourceBuilder(databaseSettings.ConnectionString)
            .EnableTransportSecurity()
            .EnableArrays();

        if (loggerFactory is not null)
            dataSourceBuilder.UseLoggerFactory(loggerFactory);

        return dataSourceBuilder.Build();
    }
}
