using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Database.Migrator;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.Launcher.Steps;

/// <summary>
/// Executes idempotent DbUp PostgreSQL migrations across all required domain contexts.
/// </summary>
public sealed class DatabaseMigrationStep(string connectionString, ILoggerFactory loggerFactory)
{
    /// <summary>
    /// Applies schema migrations across all contexts (<c>Common</c>, <c>Optimizations</c>, and <c>Plugins</c>).
    /// </summary>
    public bool Execute()
    {
        Console.WriteLine("[2/3] Running database schema migrations (DbUp)...");
        ILogger logger = loggerFactory.CreateLogger<DatabaseMigrationStep>();

        MigratorOptions options = new(connectionString, MigrationContext.All, 60);
        Result result = MigratorEngine.UpgradeDatabase(logger, options);
        if (!result.Success)
        {
            Console.WriteLine($"  [ERROR] Migration failed: {result.JoinedMessages}");
            return false;
        }

        Console.WriteLine("  -> All database contexts (Common, Optimizations, Plugins) migrated/verified.");
        return true;
    }
}
