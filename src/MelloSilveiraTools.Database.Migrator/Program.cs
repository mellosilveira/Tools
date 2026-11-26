using MelloSilveiraTools.Core.Models;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace MelloSilveiraTools.Database.Migrator;

/// <summary>
/// Command-line entry point for executing contextual PostgreSQL database schema migrations.
/// </summary>
public static class Program
{
    private const string DefaultConnectionString = "Host=localhost;Port=5432;Database=tools_db;Username=postgres;Password=postgres;";

    /// <summary>
    /// Application main method.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>Exit code: 0 for success, 1 for failure.</returns>
    public static int Main(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].Equals("--help", StringComparison.OrdinalIgnoreCase)
                || args[i].Equals("-h", StringComparison.OrdinalIgnoreCase))
            {
                PrintHelp();
                return 0;
            }
        }

        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
        ILogger logger = loggerFactory.CreateLogger("DatabaseMigrator");

        logger.LogInformation("================================================================================");
        logger.LogInformation(" MelloSilveiraTools - Contextual Database Migrator (DbUp)");
        logger.LogInformation(" NOTICE: Today this application exclusively supports PostgreSQL.");
        logger.LogInformation("================================================================================");

        string connectionString = DefaultConnectionString;
        MigrationContext context = MigrationContext.All;
        int timeoutSeconds = 60;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];

            if (arg.Equals("--context", StringComparison.OrdinalIgnoreCase) || arg.Equals("-c", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length)
                {
                    logger.LogError("Missing value for {Option}. Valid options are: All, Common, Optimizations, Plugins.", arg);
                    return 1;
                }

                string contextValue = args[++i];
                if (!Enum.TryParse(contextValue, ignoreCase: true, out MigrationContext parsedContext))
                {
                    logger.LogError("Invalid context '{Context}'. Valid options are: All, Common, Optimizations, Plugins.", contextValue);
                    return 1;
                }

                context = parsedContext;
            }
            else if (arg.Equals("--connection-string", StringComparison.OrdinalIgnoreCase) || arg.Equals("-s", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[i + 1]))
                {
                    logger.LogError("Missing or empty value for {Option}.", arg);
                    return 1;
                }

                connectionString = args[++i];
            }
            else if (arg.Equals("--timeout", StringComparison.OrdinalIgnoreCase) || arg.Equals("-t", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length || !int.TryParse(args[i + 1], out int parsedTimeout) || parsedTimeout <= 0)
                {
                    logger.LogError("Invalid or missing value for {Option}. Expected a positive integer in seconds.", arg);
                    return 1;
                }

                timeoutSeconds = parsedTimeout;
                i++;
            }
            else
            {
                logger.LogError("Unknown argument '{Argument}'. Run with --help to see supported options.", arg);
                return 1;
            }
        }

        logger.LogInformation("Target Database Engine: {Engine}", MigratorEngine.SupportedDatabaseEngine);
        logger.LogInformation("Target Context        : {Context}", context);
        logger.LogInformation("Connection String     : {ConnectionString}", MaskConnectionString(connectionString));
        logger.LogInformation("Execution Timeout     : {TimeoutSeconds}s", timeoutSeconds);
        logger.LogInformation("--------------------------------------------------------------------------------");

        MigratorOptions options = new(connectionString, context, timeoutSeconds);
        Result result = MigratorEngine.UpgradeDatabase(logger, options);

        if (result.Success)
        {
            logger.LogInformation("All schema migrations for the selected context completed successfully.");
            return 0;
        }

        logger.LogError("Migration failed: {Result}", result);
        return 1;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Usage: dotnet run --project src/MelloSilveiraTools.Database.Migrator -- [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -c, --context <Context>            Target migration context: All (default), Common, Optimizations, Plugins.");
        Console.WriteLine("  -s, --connection-string <String>   PostgreSQL connection string (default: Host=localhost;Port=5432;Database=tools_db;Username=postgres;Password=********;).");
        Console.WriteLine("  -t, --timeout <Seconds>            Command timeout in seconds (default: 60).");
        Console.WriteLine("  -h, --help                         Show this help message.");
        Console.WriteLine();
        Console.WriteLine("Note: MelloSilveiraTools currently supports PostgreSQL as its exclusive database engine.");
    }

    private static string MaskConnectionString(string connectionString)
    {
        try
        {
            NpgsqlConnectionStringBuilder builder = new(connectionString);
            if (!string.IsNullOrEmpty(builder.Password))
                builder.Password = "********";

            return builder.ConnectionString;
        }
        catch
        {
            return "********";
        }
    }
}
