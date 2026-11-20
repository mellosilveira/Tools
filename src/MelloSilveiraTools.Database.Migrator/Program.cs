using MelloSilveiraTools.Core.Models;
using Microsoft.Extensions.Logging;

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
        Console.WriteLine("================================================================================");
        Console.WriteLine(" MelloSilveiraTools - Contextual Database Migrator (DbUp)");
        Console.WriteLine(" NOTICE: Today this application exclusively supports PostgreSQL.");
        Console.WriteLine("================================================================================");

        string connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") ?? DefaultConnectionString;
        MigrationContext context = MigrationContext.All;
        int timeoutSeconds = 60;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];

            if ((arg.Equals("--help", StringComparison.OrdinalIgnoreCase) || arg.Equals("-h", StringComparison.OrdinalIgnoreCase)))
            {
                PrintHelp();
                return 0;
            }

            if ((arg.Equals("--context", StringComparison.OrdinalIgnoreCase) || arg.Equals("-c", StringComparison.OrdinalIgnoreCase)) && i + 1 < args.Length)
            {
                string contextValue = args[++i];
                if (Enum.TryParse(contextValue, ignoreCase: true, out MigrationContext parsedContext))
                {
                    context = parsedContext;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Invalid context '{contextValue}'. Valid options are: All, Common, Optimizations, Plugins.");
                    Console.ResetColor();
                    return 1;
                }
            }
            else if ((arg.Equals("--connection-string", StringComparison.OrdinalIgnoreCase) || arg.Equals("-s", StringComparison.OrdinalIgnoreCase)) && i + 1 < args.Length)
            {
                connectionString = args[++i];
            }
            else if ((arg.Equals("--timeout", StringComparison.OrdinalIgnoreCase) || arg.Equals("-t", StringComparison.OrdinalIgnoreCase)) && i + 1 < args.Length)
            {
                if (int.TryParse(args[++i], out int parsedTimeout) && parsedTimeout > 0)
                    timeoutSeconds = parsedTimeout;
            }
        }

        Console.WriteLine($"Target Database Engine: {MigratorEngine.SupportedDatabaseEngine}");
        Console.WriteLine($"Target Context        : {context}");
        Console.WriteLine($"Connection String     : {MaskConnectionString(connectionString)}");
        Console.WriteLine($"Execution Timeout     : {timeoutSeconds}s");
        Console.WriteLine("--------------------------------------------------------------------------------");

        ILogger logger = LoggerFactory
            .Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            })
            .CreateLogger("DatabaseMigrator");

        MigratorOptions options = new(connectionString, context, timeoutSeconds);
        Result result = MigratorEngine.UpgradeDatabase(logger, options);

        if (result.Success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n[SUCCESS] All schema migrations for the selected context completed successfully.");
            Console.ResetColor();
            return 0;
        }

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\n[ERROR] Migration failed: {result.JoinedMessages}");
        Console.ResetColor();
        return 1;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Usage: dotnet run --project src/MelloSilveiraTools.Database.Migrator -- [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -c, --context <Context>            Target migration context: All (default), Common, Optimizations, Plugins.");
        Console.WriteLine("  -s, --connection-string <String>   PostgreSQL connection string (defaults to CONNECTION_STRING env var or localhost).");
        Console.WriteLine("  -t, --timeout <Seconds>            Command timeout in seconds (default: 60).");
        Console.WriteLine("  -h, --help                         Show this help message.");
        Console.WriteLine();
        Console.WriteLine("Note: MelloSilveiraTools currently supports PostgreSQL as its exclusive database engine.");
    }

    private static string MaskConnectionString(string connectionString)
    {
        string[] parts = connectionString.Split(';');
        List<string> maskedParts = new(parts.Length);

        foreach (string part in parts)
        {
            string trimmed = part.Trim();
            if (string.IsNullOrEmpty(trimmed))
                continue;

            maskedParts.Add(trimmed.StartsWith("password", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("pwd", StringComparison.OrdinalIgnoreCase)
                ? "Password=********"
                : trimmed);
        }

        return string.Join(";", maskedParts);
    }
}
