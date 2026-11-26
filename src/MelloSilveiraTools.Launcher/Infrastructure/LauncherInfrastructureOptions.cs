using MelloSilveiraTools.Database.Migrator;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Globalization;

namespace MelloSilveiraTools.Launcher.Infrastructure;

/// <summary>
/// Defines where the application backend and database services are executed.
/// </summary>
public enum ExecutionMode
{
    /// <summary>
    /// Runs <c>WebApi.Host</c> locally with ReadyToRun AOT enabled (using Docker or local PostgreSQL for the database).
    /// </summary>
    Local = 0,

    /// <summary>
    /// Runs PostgreSQL, <c>Database.Migrator</c>, and self-contained <c>WebApi.Host</c> inside Docker Compose containers.
    /// </summary>
    Docker = 1
}

/// <summary>
/// Resolved infrastructure configuration for execution mode, PostgreSQL, migrations, repository root, and the Web API host.
/// </summary>
/// <param name="Mode">Target execution mode (<see cref="ExecutionMode.Local"/> or <see cref="ExecutionMode.Docker"/>).</param>
/// <param name="ConnectionString">Complete PostgreSQL connection string.</param>
/// <param name="MigrationContext">Target DbUp migration context.</param>
/// <param name="MigrationTimeoutSeconds">Migration command timeout in seconds.</param>
/// <param name="WebApiPort">Local TCP port where <c>MelloSilveiraTools.WebApi.Host</c> listens.</param>
/// <param name="RepositoryRoot">Optional explicit repository root path.</param>
/// <param name="AllowDockerInstall">Optional override for automatic Docker installation (<c>true</c>/<c>false</c>), or <c>null</c> to prompt interactively.</param>
/// <param name="OpenBrowser">Whether to open the default browser once <c>WebApi.Host</c> is healthy.</param>
public sealed record LauncherInfrastructureOptions(
    ExecutionMode Mode,
    string ConnectionString,
    MigrationContext MigrationContext,
    int MigrationTimeoutSeconds,
    int WebApiPort,
    string? RepositoryRoot,
    bool? AllowDockerInstall,
    bool OpenBrowser)
{
    /// <summary>
    /// Builds the local <c>http://localhost:&lt;port&gt;</c> base URL for <c>MelloSilveiraTools.WebApi.Host</c>.
    /// </summary>
    public string BuildWebApiBaseUrl() => $"http://localhost:{WebApiPort}";

    /// <summary>
    /// Builds a Docker-network-safe PostgreSQL connection string, mapping <c>localhost</c>/<c>127.0.0.1</c> to the <c>postgres</c> service.
    /// </summary>
    public string BuildDockerConnectionString()
    {
        NpgsqlConnectionStringBuilder builder = new(ConnectionString);
        if (string.Equals(builder.Host, "localhost", StringComparison.OrdinalIgnoreCase) || string.Equals(builder.Host, "127.0.0.1", StringComparison.Ordinal))
        {
            builder.Host = "postgres";
            builder.Port = 5432;
        }

        return builder.ConnectionString;
    }

    /// <summary>
    /// Builds the application-level PostgreSQL connection string used by <c>MelloSilveiraTools.WebApi.Host</c>.
    /// When the configured user is the <c>postgres</c> superuser (which bypasses PostgreSQL Row-Level Security),
    /// swaps credentials to the non-superuser <c>tools_app</c> role provisioned by migrations so RLS is enforced.
    /// </summary>
    /// <param name="forDocker">When <see langword="true"/>, maps <c>localhost</c>/<c>127.0.0.1</c> to the Docker <c>postgres</c> service.</param>
    /// <returns>The resolved application connection string.</returns>
    public string BuildApplicationConnectionString(bool forDocker = false)
    {
        NpgsqlConnectionStringBuilder builder = new(forDocker ? BuildDockerConnectionString() : ConnectionString);
        if (string.Equals(builder.Username, "postgres", StringComparison.OrdinalIgnoreCase))
        {
            builder.Username = "tools_app";
            builder.Password = "tools_app_pass";
        }

        return builder.ConnectionString;
    }

    /// <summary>
    /// Extracts the PostgreSQL host and port from <see cref="ConnectionString"/>, falling back to <c>localhost:5432</c>.
    /// </summary>
    public (string Host, int Port) ExtractHostAndPort()
    {
        NpgsqlConnectionStringBuilder builder = new(ConnectionString);
        string host = string.IsNullOrWhiteSpace(builder.Host) ? "localhost" : builder.Host;
        int port = builder.Port is > 0 and <= 65535 ? builder.Port : 5432;
        return (host, port);
    }

    /// <summary>
    /// Builds a log-safe PostgreSQL connection string with any password segment masked.
    /// </summary>
    public string BuildMaskedConnectionString() => MaskConnectionString(ConnectionString);

    /// <summary>
    /// Masks password segments in a PostgreSQL connection string.
    /// </summary>
    public static string MaskConnectionString(string connectionString)
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

/// <summary>
/// Parses CLI arguments, defaults, repository resolution, and optional interactive prompts for <see cref="LauncherInfrastructureOptions"/>.
/// </summary>
public static class LauncherInfrastructureResolver
{
    private const string DefaultConnectionString = "Host=localhost;Port=5432;Database=tools_db;Username=postgres;Password=postgres";
    private const int DefaultWebApiPort = 24263;
    private const MigrationContext DefaultMigrationContext = MigrationContext.All;
    private const int DefaultMigrationTimeoutSeconds = 60;
    private const string GitHubRepositoryUrl = "https://github.com/mellosilveira/Tools.git";

    /// <summary>
    /// Checks whether help was requested in the command-line arguments.
    /// </summary>
    public static bool IsHelpRequested(string[] args) =>
        args.Any(arg => arg.Equals("--help", StringComparison.OrdinalIgnoreCase) || arg.Equals("-h", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Prints CLI usage instructions to standard output.
    /// </summary>
    public static void PrintHelp()
    {
        Console.WriteLine("Usage: dotnet run --project src/MelloSilveiraTools.Launcher -- [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -m, --mode <Local|Docker>          Execution mode: Local (default, ReadyToRun AOT) or Docker (self-contained containers).");
        Console.WriteLine("  --docker                           Shorthand for --mode Docker.");
        Console.WriteLine("  -s, --connection-string <String>   PostgreSQL connection string (default: Host=localhost;Port=5432;Database=tools_db;Username=postgres;Password=********).");
        Console.WriteLine("  -c, --context <Context>            Target migration context: All (default), Common, Optimizations, Plugins.");
        Console.WriteLine($"  -t, --timeout <Seconds>            Migration command timeout in seconds (default: {DefaultMigrationTimeoutSeconds}).");
        Console.WriteLine($"  -p, --port <Port>                  Local TCP port for WebApi.Host (default: {DefaultWebApiPort}).");
        Console.WriteLine("  -r, --repo-root <Path>             Repository root path containing MelloSilveiraTools.sln (if omitted and not found locally, clones GitHub main branch).");
        Console.WriteLine("  --install-docker                   Automatically allow installing Docker if neither Docker nor local PostgreSQL is found.");
        Console.WriteLine("  --no-docker-install                Never prompt or install Docker automatically.");
        Console.WriteLine("  --no-browser                       Do not open the default browser when WebApi.Host is ready.");
        Console.WriteLine("  -y, --non-interactive              Skip interactive prompts and accept CLI/default values.");
        Console.WriteLine("  -h, --help                         Show this help message.");
    }

    /// <summary>
    /// Resolves infrastructure options from CLI arguments, defaults, and optional interactive prompts.
    /// </summary>
    /// <param name="logger">Logger for configuration diagnostics.</param>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>The resolved <see cref="LauncherInfrastructureOptions"/>, or <c>null</c> if CLI arguments are invalid.</returns>
    public static LauncherInfrastructureOptions? Resolve(ILogger logger, string[] args)
    {
        ExecutionMode mode = ExecutionMode.Local;
        string connectionString = DefaultConnectionString;
        MigrationContext migrationContext = DefaultMigrationContext;
        int migrationTimeoutSeconds = DefaultMigrationTimeoutSeconds;
        int webApiPort = DefaultWebApiPort;
        string? repositoryRoot = null;
        bool? allowDockerInstall = null;
        bool openBrowser = true;

        bool modeSet = false;
        bool connectionStringSet = false;
        bool webApiPortSet = false;
        bool nonInteractive = false;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            switch (arg.ToLowerInvariant())
            {
                case "--non-interactive":
                case "--yes":
                case "-y":
                    nonInteractive = true;
                    break;
                case "--docker":
                    mode = ExecutionMode.Docker;
                    modeSet = true;
                    break;
                case "--mode":
                case "-m":
                    if (!TryReadValue(args, ref i, out string modeRaw) || !Enum.TryParse(modeRaw, ignoreCase: true, out mode))
                    {
                        logger.LogError("Invalid or missing value for {Option}. Valid options are: Local, Docker.", arg);
                        return null;
                    }

                    modeSet = true;
                    break;
                case "--install-docker":
                    allowDockerInstall = true;
                    break;
                case "--no-docker-install":
                    allowDockerInstall = false;
                    break;
                case "--no-browser":
                    openBrowser = false;
                    break;
                case "--connection-string":
                case "-s":
                    if (!TryReadValue(args, ref i, out connectionString))
                    {
                        logger.LogError("Invalid or missing value for {Option}: cannot be empty.", arg);
                        return null;
                    }

                    connectionStringSet = true;
                    break;
                case "--context":
                case "-c":
                    if (!TryReadValue(args, ref i, out string contextRaw) || !Enum.TryParse(contextRaw, ignoreCase: true, out migrationContext))
                    {
                        logger.LogError("Invalid or missing value for {Option}. Valid options are: All, Common, Optimizations, Plugins.", arg);
                        return null;
                    }

                    break;
                case "--timeout":
                case "-t":
                    if (!TryReadValue(args, ref i, out string timeoutRaw)
                        || !int.TryParse(timeoutRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out migrationTimeoutSeconds)
                        || migrationTimeoutSeconds <= 0)
                    {
                        logger.LogError("Invalid or missing value for {Option}. Expected a positive integer in seconds.", arg);
                        return null;
                    }

                    break;
                case "--port":
                case "-p":
                    if (!TryReadValue(args, ref i, out string portRaw) || !TryParseValidPort(portRaw, out webApiPort))
                    {
                        logger.LogError("Invalid or missing value for {Option}. Expected an integer between 1 and 65535.", arg);
                        return null;
                    }

                    webApiPortSet = true;
                    break;
                case "--repo-root":
                case "-r":
                    if (!TryReadValue(args, ref i, out string rootRaw))
                    {
                        logger.LogError("Missing or empty value for {Option}.", arg);
                        return null;
                    }

                    repositoryRoot = rootRaw;
                    break;
                default:
                    logger.LogError("Unknown argument '{Argument}'. Run with --help to see supported options.", arg);
                    return null;
            }
        }

        if (!nonInteractive && !Console.IsInputRedirected && (!modeSet || !connectionStringSet || !webApiPortSet))
        {
            Console.WriteLine();
            Console.WriteLine("Configure infrastructure parameters (press Enter to accept the default in brackets):");

            if (!modeSet)
                mode = PromptExecutionMode("  Execution Mode (Local/Docker)", mode);
            if (!connectionStringSet)
                connectionString = PromptConnectionString("  PostgreSQL Connection String", connectionString);
            if (!webApiPortSet)
                webApiPort = PromptPort("  WebApi.Host Port", webApiPort);

            Console.WriteLine();
        }

        if (nonInteractive && allowDockerInstall is null)
            allowDockerInstall = false;

        return new LauncherInfrastructureOptions(
            mode,
            connectionString,
            migrationContext,
            migrationTimeoutSeconds,
            webApiPort,
            repositoryRoot,
            allowDockerInstall,
            openBrowser);
    }

    /// <summary>
    /// Resolves the repository root from <paramref name="explicitRepoRoot"/>, local parent directories, or by cloning the GitHub <c>main</c> branch.
    /// </summary>
    /// <param name="logger">Logger for repository resolution diagnostics.</param>
    /// <param name="explicitRepoRoot">Optional repository root passed via <c>--repo-root</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resolved repository root path, or <c>null</c> on failure.</returns>
    public static async Task<string?> ResolveOrCloneRepositoryRootAsync(
        ILogger logger,
        string? explicitRepoRoot,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(explicitRepoRoot))
        {
            string fullPath = Path.GetFullPath(explicitRepoRoot);
            if (File.Exists(Path.Combine(fullPath, "MelloSilveiraTools.sln")))
                return fullPath;

            logger.LogError("Specified --repo-root '{RepoRoot}' does not contain MelloSilveiraTools.sln.", fullPath);
            return null;
        }

        string? localRoot = FindRepositoryRoot(Environment.CurrentDirectory)
            ?? FindRepositoryRoot(AppContext.BaseDirectory);
        if (localRoot is not null)
            return localRoot;

        string targetDirectory = Path.Combine(Environment.CurrentDirectory, "MelloSilveiraTools");
        logger.LogInformation("Repository not found locally. Cloning main branch from {RepoUrl} into '{TargetDirectory}'...", GitHubRepositoryUrl, targetDirectory);

        (int exitCode, _, string stdErr) = await ProcessRunner.RunCommandAsync(
            "git",
            $"clone --branch main --single-branch {GitHubRepositoryUrl} \"{targetDirectory}\"",
            Environment.CurrentDirectory,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (exitCode != 0 || !File.Exists(Path.Combine(targetDirectory, "MelloSilveiraTools.sln")))
        {
            logger.LogError("Failed to clone repository from {RepoUrl}: {Error}", GitHubRepositoryUrl, stdErr);
            return null;
        }

        logger.LogInformation("Repository cloned successfully at '{TargetDirectory}'.", targetDirectory);
        return targetDirectory;
    }

    private static bool TryReadValue(string[] args, ref int index, out string value)
    {
        if (index + 1 < args.Length && !string.IsNullOrWhiteSpace(args[index + 1]))
        {
            value = args[++index];
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static ExecutionMode PromptExecutionMode(string label, ExecutionMode defaultValue)
    {
        while (true)
        {
            Console.Write($"{label} [{defaultValue}]: ");
            string? input = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(input))
                return defaultValue;

            if (Enum.TryParse(input, ignoreCase: true, out ExecutionMode parsed))
                return parsed;

            Console.WriteLine("  Invalid mode. Enter 'Local' or 'Docker'.");
        }
    }

    private static string PromptConnectionString(string label, string defaultValue)
    {
        Console.Write($"{label} [{LauncherInfrastructureOptions.MaskConnectionString(defaultValue)}]: ");
        string? input = Console.ReadLine()?.Trim();
        return string.IsNullOrEmpty(input) ? defaultValue : input;
    }

    private static int PromptPort(string label, int defaultValue)
    {
        while (true)
        {
            Console.Write($"{label} [{defaultValue}]: ");
            string? input = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(input))
                return defaultValue;

            if (TryParseValidPort(input, out int parsed))
                return parsed;

            Console.WriteLine("  Invalid port. Enter a number between 1 and 65535.");
        }
    }

    private static string? FindRepositoryRoot(string startDirectory)
    {
        DirectoryInfo? current = new(startDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "MelloSilveiraTools.sln")))
                return current.FullName;

            current = current.Parent;
        }

        return null;
    }

    private static bool TryParseValidPort(string value, out int port) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out port)
        && port is > 0 and <= 65535;
}
