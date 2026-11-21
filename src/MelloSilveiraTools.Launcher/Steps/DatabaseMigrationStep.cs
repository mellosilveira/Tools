using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Database.Migrator;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.Launcher.Steps;

/// <summary>
/// Executes idempotent DbUp PostgreSQL migrations for the configured domain context.
/// </summary>
/// <param name="loggerFactory">Logger factory used to create loggers for migration execution.</param>
/// <param name="currentStepNumber">Current 1-based step index.</param>
/// <param name="numberOfSteps">Total number of launcher steps.</param>
/// <param name="connectionString">PostgreSQL connection string.</param>
/// <param name="context">Target migration context.</param>
/// <param name="commandTimeoutInSeconds">Command timeout in seconds.</param>
public sealed class DatabaseMigrationStep(
    ILoggerFactory loggerFactory,
    int currentStepNumber,
    int numberOfSteps,
    string connectionString,
    MigrationContext context = MigrationContext.All,
    int commandTimeoutInSeconds = 60)
{
    /// <summary>
    /// Applies schema migrations for the configured <see cref="MigrationContext"/>.
    /// </summary>
    public bool Execute()
    {
        ILogger<DatabaseMigrationStep> logger = loggerFactory.CreateLogger<DatabaseMigrationStep>();
        logger.LogInformation("[{CurrentStepNumber}/{NumberOfSteps}] Running database schema migrations (DbUp) for context '{Context}'...", currentStepNumber, numberOfSteps, context);

        MigratorOptions options = new(connectionString, context, commandTimeoutInSeconds);
        Result result = MigratorEngine.UpgradeDatabase(logger, options);
        if (!result.Success)
        {
            logger.LogError("Migration failed: {Result}", result);
            return false;
        }

        logger.LogInformation("Database migrations for context '{Context}' migrated/verified.", context);
        return true;
    }
}
