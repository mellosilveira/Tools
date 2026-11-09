using DbUp;
using DbUp.Builder;
using DbUp.Engine;
using MelloSilveiraTools.Core.Models;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace MelloSilveiraTools.Database.Migrator;

/// <summary>
/// Executes contextual DDL database schema migrations against PostgreSQL using DbUp.
/// </summary>
/// <remarks>
/// <para>
/// <b>Notice:</b> The application exclusively supports PostgreSQL. All migration scripts, connection builders,
/// and journal tables are explicitly designed for and limited to PostgreSQL.
/// </para>
/// </remarks>
public static class MigratorEngine
{
    /// <summary>
    /// Supported database engine name for diagnostic logs and documentation.
    /// </summary>
    public const string SupportedDatabaseEngine = "PostgreSQL";

    /// <summary>
    /// Executes schema migrations according to the provided options.
    /// </summary>
    /// <param name="options">The contextual migration options containing the connection string and target context.</param>
    /// <param name="logger">Optional logger for telemetry and execution output.</param>
    /// <returns>A <see cref="Result"/> indicating success or containing details of failure.</returns>
    public static Result UpgradeDatabase(ILogger logger, MigratorOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            logger.LogError("Connection string must be specified. Target database engine: {Engine}", SupportedDatabaseEngine);
            return Result.CreateBadRequest("The PostgreSQL connection string cannot be empty.");
        }

        logger.LogInformation("Starting database migration for context '{Context}'. Engine: {Engine}", options.Context, SupportedDatabaseEngine);

        try
        {
            EnsureDatabase.For.PostgresqlDatabase(options.ConnectionString);

            Assembly assembly = Assembly.GetExecutingAssembly();

            UpgradeEngineBuilder builder = DeployChanges.To
                .PostgresqlDatabase(options.ConnectionString)
                .WithScriptsEmbeddedInAssembly(assembly, scriptName => FilterScriptByContext(scriptName, options.Context))
                .WithTransactionPerScript()
                .WithExecutionTimeout(TimeSpan.FromSeconds(options.CommandTimeoutInSeconds))
                .JournalToPostgresqlTable("public", "schemaversions");

            UpgradeEngine upgradeEngine = builder.Build();

            DatabaseUpgradeResult upgradeResult = upgradeEngine.PerformUpgrade();

            if (!upgradeResult.Successful)
            {
                logger.LogError(upgradeResult.Error, "Database migration failed: {Message}", upgradeResult.Error?.Message);
                return Result.CreateError(StatusCode.UnknownError, $"Database migration failed: {upgradeResult.Error?.Message}");
            }

            int executedCount = upgradeResult.Scripts.Count();
            logger.LogInformation("Database migration completed successfully. {Count} scripts executed for context '{Context}'.", executedCount, options.Context);

            return Result.CreateSuccessOk();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unexpected error occurred during database migration against {Engine}.", SupportedDatabaseEngine);
            return Result.CreateError(StatusCode.UnknownError, $"Migration exception against {SupportedDatabaseEngine}: {ex.Message}");
        }
    }

    /// <summary>
    /// Filters embedded SQL migration scripts based on the target migration context.
    /// </summary>
    /// <param name="scriptName">The fully qualified resource name of the embedded script.</param>
    /// <param name="context">The target context.</param>
    /// <returns><c>true</c> if the script belongs to the target context; otherwise, <c>false</c>.</returns>
    public static bool FilterScriptByContext(string scriptName, MigrationContext context) => context switch
    {
        MigrationContext.All => true,
        MigrationContext.Common => scriptName.Contains(".Common.", StringComparison.OrdinalIgnoreCase),
        MigrationContext.Optimizations => scriptName.Contains(".Common.", StringComparison.OrdinalIgnoreCase)
                                       || scriptName.Contains(".Optimizations.", StringComparison.OrdinalIgnoreCase),
        MigrationContext.Plugins => scriptName.Contains(".Plugins.", StringComparison.OrdinalIgnoreCase),
        _ => false
    };
}
