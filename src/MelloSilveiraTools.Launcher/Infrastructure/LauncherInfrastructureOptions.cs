using MelloSilveiraTools.Launcher.Steps;
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
    /// Runs <c>WebApi.Host</c> locally with ReadyToRun AOT enabled (using local PostgreSQL).
    /// </summary>
    Local = 0,

    /// <summary>
    /// Runs PostgreSQL, <c>Database.Migrator</c>, and self-contained <c>WebApi.Host</c> inside Docker Compose containers.
    /// </summary>
    Docker = 1
}

/// <summary>
/// Resolved infrastructure configuration for execution mode, PostgreSQL, migrations, repository root/branch, and the Web API host.
/// </summary>
/// <param name="Mode">Resolved or requested execution mode (<see cref="ExecutionMode.Local"/> or <see cref="ExecutionMode.Docker"/>).</param>
/// <param name="ModeExplicitlySet">Whether <paramref name="Mode"/> was explicitly forced via CLI arguments.</param>
/// <param name="ConnectionString">Complete PostgreSQL connection string.</param>
/// <param name="MigrationContext">Target DbUp migration context.</param>
/// <param name="MigrationTimeoutSeconds">Migration command timeout in seconds.</param>
/// <param name="WebApiPort">Local HTTP TCP port where <c>MelloSilveiraTools.WebApi.Host</c> listens (default: <c>26324</c>).</param>
/// <param name="WebApiHttpsPort">External HTTPS TCP port where <c>MelloSilveiraTools.WebApi.Host</c> listens (default: <c>24263</c>).</param>
/// <param name="RepositoryRoot">Optional explicit repository root path.</param>
/// <param name="Branch">GitHub branch to clone when no local repository copy exists (default: <c>main</c>).</param>
/// <param name="BranchExplicitlySet">Whether <paramref name="Branch"/> was explicitly supplied via CLI arguments.</param>
/// <param name="AllowDockerInstall">Optional override for automatic Docker installation (<c>true</c>/<c>false</c>), or <c>null</c> to prompt interactively.</param>
/// <param name="AllowPostgresInstall">Optional override for automatic local PostgreSQL installation (<c>true</c>/<c>false</c>), or <c>null</c> to prompt interactively.</param>
/// <param name="OpenBrowser">Optional override for browser opening (<c>true</c>/<c>false</c>), or <c>null</c> to prompt interactively once ready.</param>
/// <param name="NonInteractive">Whether interactive stdin prompts are disabled.</param>
/// <param name="HttpsCertificatePath">Optional path to a custom HTTPS PKCS#12 (<c>.pfx</c>) certificate file.</param>
/// <param name="HttpsCertificatePassword">Optional password for <paramref name="HttpsCertificatePath"/>.</param>
public sealed record LauncherInfrastructureOptions(
    ExecutionMode Mode,
    bool ModeExplicitlySet,
    string ConnectionString,
    LauncherMigrationContext MigrationContext,
    int MigrationTimeoutSeconds,
    int WebApiPort,
    int WebApiHttpsPort,
    string? RepositoryRoot,
    string Branch,
    bool BranchExplicitlySet,
    bool? AllowDockerInstall,
    bool? AllowPostgresInstall,
    bool? OpenBrowser,
    bool NonInteractive,
    string? HttpsCertificatePath = null,
    string? HttpsCertificatePassword = null)
{
    /// <summary>
    /// Builds the local <c>http://localhost:&lt;port&gt;</c> base URL for <c>MelloSilveiraTools.WebApi.Host</c>.
    /// </summary>
    public string BuildWebApiBaseUrl() => $"http://localhost:{WebApiPort}";

    /// <summary>
    /// Builds the <c>https://localhost:&lt;httpsPort&gt;</c> URL for <c>MelloSilveiraTools.WebApi.Host</c>.
    /// </summary>
    public string BuildWebApiHttpsUrl() => $"https://localhost:{WebApiHttpsPort}";

    /// <summary>
    /// Builds a Docker-network-safe PostgreSQL connection string, mapping <c>localhost</c>/<c>127.0.0.1</c>
    /// either to the Compose <c>postgres</c> service or to <c>host.docker.internal</c> when reusing an external container.
    /// </summary>
    /// <param name="useHostGateway">When <see langword="true"/>, maps loopback hosts to <c>host.docker.internal</c> preserving the host port.</param>
    public string BuildDockerConnectionString(bool useHostGateway = false)
    {
        NpgsqlConnectionStringBuilder builder = new(ConnectionString);
        if (string.Equals(builder.Host, "localhost", StringComparison.OrdinalIgnoreCase) || string.Equals(builder.Host, "127.0.0.1", StringComparison.Ordinal))
        {
            if (useHostGateway)
            {
                builder.Host = "host.docker.internal";
                if (builder.Port <= 0)
                    builder.Port = 5432;
            }
            else
            {
                builder.Host = "postgres";
                builder.Port = 5432;
            }
        }

        return builder.ConnectionString;
    }

    /// <summary>
    /// Returns a copy of this options record with the PostgreSQL host port updated in <see cref="ConnectionString"/>.
    /// </summary>
    /// <param name="newPort">New PostgreSQL TCP port.</param>
    public LauncherInfrastructureOptions WithPostgresPort(int newPort)
    {
        NpgsqlConnectionStringBuilder builder = new(ConnectionString) { Port = newPort };
        return this with { ConnectionString = builder.ConnectionString };
    }

    /// <summary>
    /// Returns a copy of this options record with <see cref="WebApiPort"/> updated.
    /// </summary>
    /// <param name="newPort">New Web API / Frontend local HTTP TCP port.</param>
    public LauncherInfrastructureOptions WithWebApiPort(int newPort) => this with { WebApiPort = newPort };

    /// <summary>
    /// Returns a copy of this options record with <see cref="WebApiHttpsPort"/> updated.
    /// </summary>
    /// <param name="newPort">New Web API / Frontend HTTPS TCP port.</param>
    public LauncherInfrastructureOptions WithWebApiHttpsPort(int newPort) => this with { WebApiHttpsPort = newPort };

    /// <summary>
    /// Builds the application-level PostgreSQL connection string used by <c>MelloSilveiraTools.WebApi.Host</c>.
    /// When the configured user is the <c>postgres</c> superuser (which bypasses PostgreSQL Row-Level Security),
    /// swaps credentials to the non-superuser <c>tools_app</c> role provisioned by migrations so RLS is enforced.
    /// </summary>
    /// <param name="forDocker">When <see langword="true"/>, maps <c>localhost</c>/<c>127.0.0.1</c> to the Docker <c>postgres</c> service (or <c>host.docker.internal</c>).</param>
    /// <param name="useHostGateway">When <see langword="true"/> and <paramref name="forDocker"/> is <see langword="true"/>, uses <c>host.docker.internal</c>.</param>
    /// <returns>The resolved application connection string.</returns>
    public string BuildApplicationConnectionString(bool forDocker = false, bool useHostGateway = false)
    {
        NpgsqlConnectionStringBuilder builder = new(forDocker ? BuildDockerConnectionString(useHostGateway) : ConnectionString);
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
    private const int DefaultWebApiPort = 26324;
    private const int DefaultWebApiHttpsPort = 24263;
    private const LauncherMigrationContext DefaultMigrationContext = LauncherMigrationContext.All;
    private const int DefaultMigrationTimeoutSeconds = 60;
    private const string DefaultBranch = "main";
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
        Console.WriteLine("Usage: MelloSilveiraTools.Launcher [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -m, --mode <Local|Docker>          Force execution mode: Local or Docker (default: auto-detect native PostgreSQL -> Docker -> install).");
        Console.WriteLine("  --docker                           Shorthand for --mode Docker.");
        Console.WriteLine("  -s, --connection-string <String>   PostgreSQL connection string (default: Host=localhost;Port=5432;Database=tools_db;Username=postgres;Password=********).");
        Console.WriteLine("  -c, --context <Context>            Target migration context: All (default), Common, Optimizations, Plugins.");
        Console.WriteLine($"  -t, --timeout <Seconds>            Migration command timeout in seconds (default: {DefaultMigrationTimeoutSeconds}).");
        Console.WriteLine($"  -p, --port <Port>                  Local HTTP TCP port for WebApi.Host & WebUi (default: {DefaultWebApiPort}).");
        Console.WriteLine($"  --https-port <Port>                External HTTPS TCP port for WebApi.Host & WebUi (default: {DefaultWebApiHttpsPort}).");
        Console.WriteLine("  --cert-path <Path>                 Path to a custom HTTPS (.pfx) certificate file (default: prompt or generate local cert).");
        Console.WriteLine("  --cert-password <Password>         Optional password for the custom HTTPS certificate.");
        Console.WriteLine("  -r, --repo-root <Path>             Repository root path containing MelloSilveiraTools.sln.");
        Console.WriteLine($"  -b, --branch <Branch>              GitHub branch to clone when repository is absent locally (default: {DefaultBranch}).");
        Console.WriteLine("  --install-docker                   Automatically allow installing Docker if neither PostgreSQL nor Docker is found.");
        Console.WriteLine("  --no-docker-install                Decline automatic Docker installation.");
        Console.WriteLine("  --install-postgres                 Automatically allow installing PostgreSQL + .NET 10 locally if Docker is declined.");
        Console.WriteLine("  --no-postgres-install              Decline automatic PostgreSQL installation.");
        Console.WriteLine("  --open-browser                     Automatically open the default browser on the frontend Home page when ready.");
        Console.WriteLine("  --no-browser                       Do not open the default browser when WebApi.Host is ready.");
        Console.WriteLine("  -y, --non-interactive              Skip interactive prompts and accept CLI/default values.");
        Console.WriteLine("  -h, --help                         Show this help message.");
    }

    /// <summary>
    /// Resolves infrastructure options from CLI arguments and defaults.
    /// </summary>
    /// <param name="logger">Logger for configuration diagnostics.</param>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>The resolved <see cref="LauncherInfrastructureOptions"/>, or <c>null</c> if CLI arguments are invalid.</returns>
    public static LauncherInfrastructureOptions? Resolve(ILogger logger, string[] args)
    {
        ExecutionMode mode = ExecutionMode.Local;
        string connectionString = DefaultConnectionString;
        LauncherMigrationContext migrationContext = DefaultMigrationContext;
        int migrationTimeoutSeconds = DefaultMigrationTimeoutSeconds;
        int webApiPort = DefaultWebApiPort;
        int webApiHttpsPort = DefaultWebApiHttpsPort;
        string? repositoryRoot = null;
        string branch = DefaultBranch;
        bool branchSet = false;
        bool? allowDockerInstall = null;
        bool? allowPostgresInstall = null;
        bool? openBrowser = null;
        string? httpsCertificatePath = null;
        string? httpsCertificatePassword = null;

        bool modeSet = false;
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
                case "--install-postgres":
                    allowPostgresInstall = true;
                    break;
                case "--no-postgres-install":
                    allowPostgresInstall = false;
                    break;
                case "--open-browser":
                    openBrowser = true;
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

                    break;
                case "--https-port":
                    if (!TryReadValue(args, ref i, out string httpsPortRaw) || !TryParseValidPort(httpsPortRaw, out webApiHttpsPort))
                    {
                        logger.LogError("Invalid or missing value for {Option}. Expected an integer between 1 and 65535.", arg);
                        return null;
                    }

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
                case "--branch":
                case "-b":
                    if (!TryReadValue(args, ref i, out string branchRaw))
                    {
                        logger.LogError("Missing or empty value for {Option}.", arg);
                        return null;
                    }

                    branch = branchRaw.Trim();
                    branchSet = true;
                    break;
                case "--cert-path":
                    if (!TryReadValue(args, ref i, out string certPathRaw))
                    {
                        logger.LogError("Missing or empty value for {Option}.", arg);
                        return null;
                    }

                    httpsCertificatePath = certPathRaw;
                    break;
                case "--cert-password":
                    if (i + 1 >= args.Length)
                    {
                        logger.LogError("Missing value for {Option}.", arg);
                        return null;
                    }

                    httpsCertificatePassword = args[++i];
                    break;
                default:
                    logger.LogError("Unknown argument '{Argument}'. Run with --help to see supported options.", arg);
                    return null;
            }
        }

        if (nonInteractive)
        {
            allowDockerInstall ??= false;
            allowPostgresInstall ??= false;
        }

        return new LauncherInfrastructureOptions(
            mode,
            modeSet,
            connectionString,
            migrationContext,
            migrationTimeoutSeconds,
            webApiPort,
            webApiHttpsPort,
            repositoryRoot,
            branch,
            branchSet,
            allowDockerInstall,
            allowPostgresInstall,
            openBrowser,
            nonInteractive,
            httpsCertificatePath,
            httpsCertificatePassword);
    }

    /// <summary>
    /// Resolves the repository root from <see cref="LauncherInfrastructureOptions.RepositoryRoot"/>, local directories, or by cloning from GitHub
    /// after asking the user which branch to use (defaulting to <c>main</c>).
    /// </summary>
    /// <param name="logger">Logger for repository resolution diagnostics.</param>
    /// <param name="dependencyInstaller">Dependency installer used to ensure <c>git</c> is present before cloning.</param>
    /// <param name="options">Resolved launcher infrastructure options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resolved repository root path, or <c>null</c> on failure.</returns>
    public static async Task<string?> ResolveOrCloneRepositoryRootAsync(
        ILogger logger,
        DependencyInstaller dependencyInstaller,
        LauncherInfrastructureOptions options,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(options.RepositoryRoot))
        {
            string fullPath = Path.GetFullPath(options.RepositoryRoot);
            if (File.Exists(Path.Combine(fullPath, "MelloSilveiraTools.sln")))
                return fullPath;

            logger.LogError("Specified --repo-root '{RepoRoot}' does not contain MelloSilveiraTools.sln.", fullPath);
            return null;
        }

        string targetDirectory = Path.Combine(Environment.CurrentDirectory, "MelloSilveiraTools");
        string? localRoot = FindRepositoryRoot(Environment.CurrentDirectory)
            ?? FindRepositoryRoot(targetDirectory)
            ?? FindRepositoryRoot(AppContext.BaseDirectory);
        if (localRoot is not null)
        {
            logger.LogInformation("Local copy of MelloSilveiraTools found at '{RepoRoot}'.", localRoot);
            return localRoot;
        }

        if (!await dependencyInstaller.EnsureGitInstalledAsync(cancellationToken).ConfigureAwait(false))
            return null;

        string targetBranch = options.Branch;
        if (!options.BranchExplicitlySet && !options.NonInteractive && !Console.IsInputRedirected)
        {
            Console.Write($"  MelloSilveiraTools copy not found locally. Enter GitHub branch to clone [{DefaultBranch}]: ");
            string? input = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(input))
                targetBranch = input;
        }

        logger.LogInformation("Cloning branch '{Branch}' from {RepoUrl} into '{TargetDirectory}'...", targetBranch, GitHubRepositoryUrl, targetDirectory);

        (int exitCode, _, string stdErr) = await ProcessRunner.RunCommandAsync(
            "git",
            $"clone --branch \"{targetBranch}\" --single-branch {GitHubRepositoryUrl} \"{targetDirectory}\"",
            Environment.CurrentDirectory,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (exitCode != 0 || !File.Exists(Path.Combine(targetDirectory, "MelloSilveiraTools.sln")))
        {
            logger.LogError("Failed to clone branch '{Branch}' from {RepoUrl}: {Error}", targetBranch, GitHubRepositoryUrl, stdErr);
            return null;
        }

        logger.LogInformation("Repository cloned successfully at '{TargetDirectory}' (branch: {Branch}).", targetDirectory, targetBranch);
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

    private static string? FindRepositoryRoot(string startDirectory)
    {
        if (!Directory.Exists(startDirectory))
            return null;

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
