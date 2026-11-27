using MelloSilveiraTools.Launcher.Infrastructure;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Diagnostics;
using System.Globalization;

namespace MelloSilveiraTools.Launcher.Steps;

/// <summary>
/// Executes idempotent DbUp PostgreSQL migrations via <c>MelloSilveiraTools.Database.Migrator</c>,
/// verifies that all expected schema tables exist in PostgreSQL, and tears down the temporary migration runner process.
/// </summary>
/// <param name="loggerFactory">Logger factory used to create loggers for migration execution.</param>
/// <param name="currentStepNumber">Current 1-based step index.</param>
/// <param name="numberOfSteps">Total number of launcher steps.</param>
/// <param name="repositoryRoot">Root directory of the cloned or local repository.</param>
/// <param name="connectionString">PostgreSQL connection string.</param>
/// <param name="context">Target migration context.</param>
/// <param name="commandTimeoutInSeconds">Command timeout in seconds.</param>
public sealed class DatabaseMigrationStep(
    ILoggerFactory loggerFactory,
    int currentStepNumber,
    int numberOfSteps,
    string repositoryRoot,
    string connectionString,
    LauncherMigrationContext context = LauncherMigrationContext.All,
    int commandTimeoutInSeconds = 60)
{
    /// <summary>
    /// Starts the local migration project, runs migrations, validates created tables in PostgreSQL, and terminates/cleans up the migration process.
    /// </summary>
    public async Task<bool> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        ILogger<DatabaseMigrationStep> logger = loggerFactory.CreateLogger<DatabaseMigrationStep>();
        logger.LogInformation("[{CurrentStepNumber}/{NumberOfSteps}] Running database schema migrations (DbUp) for context '{Context}'...", currentStepNumber, numberOfSteps, context);

        string migratorProject = Path.Combine(repositoryRoot, "src", "MelloSilveiraTools.Database.Migrator", "MelloSilveiraTools.Database.Migrator.csproj");
        if (!File.Exists(migratorProject))
        {
            logger.LogError("Could not locate Database.Migrator project at '{MigratorProject}'.", migratorProject);
            return false;
        }

        bool processSucceeded = await RunLocalMigratorProcessAndTearDownAsync(logger, migratorProject, cancellationToken).ConfigureAwait(false);
        if (!processSucceeded)
            return false;

        bool tablesValid = await ValidateTablesCreatedAsync(logger, connectionString, context, cancellationToken).ConfigureAwait(false);
        if (!tablesValid)
            return false;

        logger.LogInformation("Database migrations for context '{Context}' validated and temporary migrator process stopped.", context);
        return true;
    }

    /// <summary>
    /// Verifies in PostgreSQL <c>information_schema.tables</c> that <c>schemaversions</c> and all expected domain tables for <paramref name="context"/> exist.
    /// </summary>
    /// <param name="logger">Logger for validation output.</param>
    /// <param name="connectionString">PostgreSQL connection string.</param>
    /// <param name="context">Migration context whose tables must exist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> when all expected tables exist; otherwise <see langword="false"/>.</returns>
    public static async Task<bool> ValidateTablesCreatedAsync(
        ILogger logger,
        string connectionString,
        LauncherMigrationContext context,
        CancellationToken cancellationToken = default)
    {
        string[] expectedTables = GetExpectedTables(context);
        try
        {
            await using NpgsqlConnection connection = new(connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            HashSet<string> existingTables = new(StringComparer.OrdinalIgnoreCase);
            const string sql = "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE';";
            await using NpgsqlCommand command = new(sql, connection);
            await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                string tableName = reader.GetString(0);
                existingTables.Add(tableName);
            }

            List<string> missingTables = [];
            foreach (string expected in expectedTables)
            {
                if (!existingTables.Contains(expected))
                    missingTables.Add(expected);
            }

            if (missingTables.Count > 0)
            {
                string missingJoined = string.Join(", ", missingTables);
                logger.LogError("Database table validation failed for context '{Context}'. Missing tables: {MissingTables}", context, missingJoined);
                return false;
            }

            logger.LogInformation("Verified {TableCount} expected PostgreSQL table(s) for context '{Context}'.", expectedTables.Length, context);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to validate created PostgreSQL tables for context '{Context}'.", context);
            return false;
        }
    }

    /// <summary>
    /// Returns the expected PostgreSQL table names created for the given <see cref="LauncherMigrationContext"/>.
    /// </summary>
    public static string[] GetExpectedTables(LauncherMigrationContext context) => context switch
    {
        LauncherMigrationContext.Common => ["schemaversions", "users", "storage_entry_metadata"],
        LauncherMigrationContext.Optimizations => ["schemaversions", "users", "storage_entry_metadata", "mechanical_model_curve_fit_outputs", "mechanical_model_simulations", "parametric_sweeps"],
        LauncherMigrationContext.Plugins => ["schemaversions", "plugin_cache", "plugin_submissions"],
        LauncherMigrationContext.All => ["schemaversions", "users", "storage_entry_metadata", "mechanical_model_curve_fit_outputs", "mechanical_model_simulations", "parametric_sweeps", "plugin_cache", "plugin_submissions"],
        _ => ["schemaversions"]
    };

    private async Task<bool> RunLocalMigratorProcessAndTearDownAsync(
        ILogger logger,
        string migratorProject,
        CancellationToken cancellationToken)
    {
        string timeoutArg = commandTimeoutInSeconds.ToString(CultureInfo.InvariantCulture);
        ProcessStartInfo startInfo = new()
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{migratorProject}\" --configuration Release -- --connection-string \"{connectionString}\" --context {context} --timeout {timeoutArg}",
            WorkingDirectory = repositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using Process? migratorProcess = Process.Start(startInfo);
        if (migratorProcess is null)
        {
            logger.LogError("Failed to start local Database.Migrator process.");
            return false;
        }

        try
        {
            Task<string> stdOutTask = migratorProcess.StandardOutput.ReadToEndAsync(cancellationToken);
            Task<string> stdErrTask = migratorProcess.StandardError.ReadToEndAsync(cancellationToken);
            await migratorProcess.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            string stdOut = await stdOutTask.ConfigureAwait(false);
            string stdErr = await stdErrTask.ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(stdOut))
                logger.LogInformation("{MigratorOutput}", stdOut.TrimEnd());

            if (migratorProcess.ExitCode != 0)
            {
                logger.LogError("Local Database.Migrator exited with code {ExitCode}: {Error}", migratorProcess.ExitCode, stdErr);
                return false;
            }

            return true;
        }
        finally
        {
            if (!migratorProcess.HasExited)
            {
                migratorProcess.Kill(entireProcessTree: true);
                await migratorProcess.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }
    }
}
