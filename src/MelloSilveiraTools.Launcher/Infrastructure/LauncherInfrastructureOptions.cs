using MelloSilveiraTools.Database.Migrator;
using Microsoft.Extensions.Logging;
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
    public string
        BuildDockerConnectionString()
    {
        string[] parts = ConnectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        List<string> updatedParts = new(parts.Length);

        foreach (string part in parts)
        {
            int separatorIndex = part.IndexOf('=');
            if (separatorIndex > 0)
            {
                string key = part[..separatorIndex].Trim();
                string value = part[(separatorIndex + 1)..].Trim();
                if ((key.Equals("Host", StringComparison.OrdinalIgnoreCase) || key.Equals("Server", StringComparison.OrdinalIgnoreCase))
                    && (value.Equals("localhost", StringComparison.OrdinalIgnoreCase) || value.Equals("127.0.0.1", StringComparison.Ordinal)))
                {
                    updatedParts.Add($"{key}=postgres");
                    continue;
                }
            }

            updatedParts.Add(part);
        }

        return string.Join(";", updatedParts) + ";";
    }

    /// <summary>
    /// Extracts the PostgreSQL host and port from <see cref="ConnectionString"/>, falling back to <c>localhost:5432</c>.
    /// </summary>
    public (string Host, int Port) ExtractHostAndPort()
    {
        string host = "localhost";
        int port = 5432;

        foreach (string part in ConnectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            int separatorIndex = part.IndexOf('=');
            if (separatorIndex <= 0)
                continue;

            string key = part[..separatorIndex].Trim();
            string value = part[(separatorIndex + 1)..].Trim();
            if (string.IsNullOrEmpty(value))
                continue;

            if (key.Equals("Host", StringComparison.OrdinalIgnoreCase) || key.Equals("Server", StringComparison.OrdinalIgnoreCase))
                host = value;
            else if (key.Equals("Port", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedPort) && parsedPort is > 0 and <= 65535)
                port = parsedPort;
        }

        return (host, port);
    }

    /// <summary>
    /// Builds a log-safe PostgreSQL connection string with any password segment masked.
    /// </summary>
    public string BuildMaskedConnectionString() => MaskConnectionString(ConnectionString);

    /// <summary>
    /// Masks <c>Password</c> and <c>Pwd</c> segments in a PostgreSQL connection string.
    /// </summary>
    public static string MaskConnectionString(string connectionString)
    {
        string[] parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        List<string> maskedParts = new(parts.Length);

        foreach (string part in parts)
        {
            maskedParts.Add(part.StartsWith("password=", StringComparison.OrdinalIgnoreCase)
                            || part.StartsWith("pwd=", StringComparison.OrdinalIgnoreCase)
                ? "Password=********"
                : part);
        }

        return string.Join(";", maskedParts);
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
    public static bool IsHelpRequested(string[] args)
    {
        foreach (string arg in args)
        {
            if (arg.Equals("--help", StringComparison.OrdinalIgnoreCase)
                || arg.Equals("-h", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

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

            if (arg.Equals("--non-interactive", StringComparison.OrdinalIgnoreCase)
                || arg.Equals("--yes", StringComparison.OrdinalIgnoreCase)
                || arg.Equals("-y", StringComparison.OrdinalIgnoreCase))
            {
                nonInteractive = true;
            }
            else if (arg.Equals("--docker", StringComparison.OrdinalIgnoreCase))
            {
                mode = ExecutionMode.Docker;
                modeSet = true;
            }
            else if (arg.Equals("--mode", StringComparison.OrdinalIgnoreCase) || arg.Equals("-m", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length || !Enum.TryParse(args[i + 1], ignoreCase: true, out mode))
                {
                    logger.LogError("Invalid or missing value for {Option}. Valid options are: Local, Docker.", arg);
                    return null;
                }

                modeSet = true;
                i++;
            }
            else if (arg.Equals("--install-docker", StringComparison.OrdinalIgnoreCase))
            {
                allowDockerInstall = true;
            }
            else if (arg.Equals("--no-docker-install", StringComparison.OrdinalIgnoreCase))
            {
                allowDockerInstall = false;
            }
            else if (arg.Equals("--no-browser", StringComparison.OrdinalIgnoreCase))
            {
                openBrowser = false;
            }
            else if (arg.Equals("--connection-string", StringComparison.OrdinalIgnoreCase) || arg.Equals("-s", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[i + 1]))
                {
                    logger.LogError("Invalid or missing value for {Option}: cannot be empty.", arg);
                    return null;
                }

                connectionString = args[++i];
                connectionStringSet = true;
            }
            else if (arg.Equals("--context", StringComparison.OrdinalIgnoreCase) || arg.Equals("-c", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length)
                {
                    logger.LogError("Missing value for {Option}. Valid options are: All, Common, Optimizations, Plugins.", arg);
                    return null;
                }

                string contextValue = args[++i];
                if (!Enum.TryParse(contextValue, ignoreCase: true, out migrationContext))
                {
                    logger.LogError("Invalid context '{Context}'. Valid options are: All, Common, Optimizations, Plugins.", contextValue);
                    return null;
                }
            }
            else if (arg.Equals("--timeout", StringComparison.OrdinalIgnoreCase) || arg.Equals("-t", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length
                    || !int.TryParse(args[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out migrationTimeoutSeconds)
                    || migrationTimeoutSeconds <= 0)
                {
                    logger.LogError("Invalid or missing value for {Option}. Expected a positive integer in seconds.", arg);
                    return null;
                }

                i++;
            }
            else if (arg.Equals("--port", StringComparison.OrdinalIgnoreCase) || arg.Equals("-p", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length || !TryParseValidPort(args[i + 1], out webApiPort))
                {
                    logger.LogError("Invalid or missing value for {Option}. Expected an integer between 1 and 65535.", arg);
                    return null;
                }

                webApiPortSet = true;
                i++;
            }
            else if (arg.Equals("--repo-root", StringComparison.OrdinalIgnoreCase) || arg.Equals("-r", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[i + 1]))
                {
                    logger.LogError("Missing or empty value for {Option}.", arg);
                    return null;
                }

                repositoryRoot = args[++i];
            }
            else
            {
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
            cancellationToken).ConfigureAwait(false);

        if (exitCode != 0 || !File.Exists(Path.Combine(targetDirectory, "MelloSilveiraTools.sln")))
        {
            logger.LogError("Failed to clone repository from {RepoUrl}: {Error}", GitHubRepositoryUrl, stdErr);
            return null;
        }

        logger.LogInformation("Repository cloned successfully at '{TargetDirectory}'.", targetDirectory);
        return targetDirectory;
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
