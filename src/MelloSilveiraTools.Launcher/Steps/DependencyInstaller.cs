using MelloSilveiraTools.Launcher.Infrastructure;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace MelloSilveiraTools.Launcher.Steps;

/// <summary>
/// Metadata describing an existing PostgreSQL container discovered in Docker.
/// </summary>
/// <param name="Name">Docker container name.</param>
/// <param name="Image">Container image name.</param>
/// <param name="IsRunning">Whether the container is currently running.</param>
/// <param name="HostPort">Published host TCP port mapped to container port <c>5432</c>, if any.</param>
public readonly record struct DockerPostgresContainerInfo(string Name, string Image, bool IsRunning, int? HostPort);

/// <summary>
/// Resolves and installs system prerequisites (<c>PostgreSQL</c>, <c>Docker</c>, <c>.NET 10 SDK/Runtime</c>, and <c>git</c>)
/// following the launcher precedence:
/// native PostgreSQL -&gt; installed Docker (reusing existing Docker PostgreSQL when present) -&gt; prompt Docker install -&gt; prompt local PostgreSQL + .NET 10 install -&gt; fail clearly if both are declined.
/// </summary>
/// <param name="logger">Logger for diagnostics and interactive setup messages.</param>
/// <param name="connectionString">Target PostgreSQL connection string.</param>
/// <param name="allowDockerInstall">Optional CLI override for Docker installation.</param>
/// <param name="allowPostgresInstall">Optional CLI override for local PostgreSQL installation.</param>
/// <param name="nonInteractive">When <see langword="true"/>, skips interactive stdin prompts.</param>
/// <param name="timeProvider">Clock provider used for bounded retry delays.</param>
public sealed class DependencyInstaller(
    ILogger<DependencyInstaller> logger,
    string connectionString,
    bool? allowDockerInstall,
    bool? allowPostgresInstall,
    bool nonInteractive,
    TimeProvider timeProvider)
{
    private readonly string _host = ExtractHost(connectionString);
    private readonly int _port = ExtractPort(connectionString);

    /// <summary>
    /// Executes steps 1 through 7 of infrastructure detection and dependency installation, returning the resolved <see cref="ExecutionMode"/>.
    /// </summary>
    /// <param name="requestedMode">Optional explicit mode override from CLI (<c>--mode</c> / <c>--docker</c>).</param>
    /// <param name="modeExplicitlySet">Whether <paramref name="requestedMode"/> was explicitly forced via CLI.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing success status and the resolved <see cref="ExecutionMode"/>.</returns>
    public async Task<(bool Success, ExecutionMode Mode)> ResolveExecutionInfrastructureAsync(
        ExecutionMode requestedMode,
        bool modeExplicitlySet,
        CancellationToken cancellationToken = default)
    {
        if (modeExplicitlySet && requestedMode == ExecutionMode.Docker)
        {
            bool dockerReady = await EnsureOrInstallDockerAsync(cancellationToken).ConfigureAwait(false);
            return (dockerReady, ExecutionMode.Docker);
        }

        // 1. Validate if PostgreSQL is installed / running natively on this machine (excluding Docker Postgres containers).
        logger.LogInformation("[Setup 1/7] Checking for native PostgreSQL installation on {Host}:{Port}...", _host, _port);
        DockerPostgresContainerInfo? dockerPostgres = await DetectDockerPostgresAsync(_port, cancellationToken).ConfigureAwait(false);
        bool dockerPostgresOnTargetPort = dockerPostgres is { IsRunning: true } info && (info.HostPort is null || info.HostPort == _port);
        bool nativePostgresInstalled = await IsNativePostgresInstalledAsync(dockerPostgresOnTargetPort, cancellationToken).ConfigureAwait(false);

        if (nativePostgresInstalled)
        {
            logger.LogInformation("Native PostgreSQL detected.");
            if (!await EnsureLocalPostgresRunningAsync(cancellationToken).ConfigureAwait(false))
                return (false, ExecutionMode.Local);

            logger.LogInformation("Applications and database will run locally using native PostgreSQL.");
            bool dotnetReady = await EnsureDotNet10InstalledAsync(cancellationToken).ConfigureAwait(false);
            return (dotnetReady, ExecutionMode.Local);
        }

        if (modeExplicitlySet && requestedMode == ExecutionMode.Local)
        {
            logger.LogInformation("Local mode explicitly requested, but native PostgreSQL was not found.");
            bool localAllowed = ConfirmInstall(
                allowPostgresInstall,
                "Native PostgreSQL is not installed. Do you allow installing PostgreSQL and .NET 10 SDK/Runtime locally? [Y/n]: ");
            if (!localAllowed)
            {
                logger.LogError("Cannot continue in Local mode without PostgreSQL installed.");
                return (false, ExecutionMode.Local);
            }

            bool localReady = await InstallLocalStackAsync(cancellationToken).ConfigureAwait(false);
            return (localReady, ExecutionMode.Local);
        }

        // 2. If native PostgreSQL is not installed, check whether Docker is installed.
        logger.LogInformation("[Setup 2/7] Native PostgreSQL not detected. Checking whether Docker is installed...");
        if (await IsDockerInstalledAsync(cancellationToken).ConfigureAwait(false))
        {
            logger.LogInformation("Docker detected. Applications and PostgreSQL will run inside Docker containers.");
            bool daemonReady = await EnsureDockerDaemonRunningAsync(logger, timeProvider, cancellationToken).ConfigureAwait(false);
            if (!daemonReady)
            {
                logger.LogError("Docker is installed, but the Docker daemon could not be started.");
                return (false, ExecutionMode.Docker);
            }

            return (true, ExecutionMode.Docker);
        }

        // 3. Ask user permission to install Docker.
        logger.LogWarning("[Setup 3/7] Neither native PostgreSQL nor Docker was detected on this machine.");
        bool installDocker = ConfirmInstall(
            allowDockerInstall,
            "Docker is not installed. Do you allow downloading and installing Docker now? [Y/n]: ");

        if (installDocker)
        {
            // 6. Install Docker and use Docker for both apps and PostgreSQL.
            logger.LogInformation("[Setup 6/7] Installing Docker to run applications and PostgreSQL in containers...");
            bool dockerInstalled = await TryInstallDockerAsync(cancellationToken).ConfigureAwait(false);
            if (!dockerInstalled)
            {
                logger.LogError("Docker installation did not complete automatically. Please finish installing Docker and re-run the launcher.");
                return (false, ExecutionMode.Docker);
            }

            if (!await EnsureDockerDaemonRunningAsync(logger, timeProvider, cancellationToken).ConfigureAwait(false))
            {
                logger.LogError("Docker was installed, but Docker Desktop/daemon did not start yet. Start Docker (or log out/reboot if required) and re-run the launcher.");
                return (false, ExecutionMode.Docker);
            }

            logger.LogInformation("Docker is ready. Applications and PostgreSQL will run inside Docker containers.");
            return (true, ExecutionMode.Docker);
        }

        // 4. Docker installation declined -> ask permission to install PostgreSQL locally.
        logger.LogInformation("[Setup 4/7] Docker installation was declined.");
        bool installPostgres = ConfirmInstall(
            allowPostgresInstall,
            "Docker will not be installed. Do you allow installing PostgreSQL locally (along with .NET 10 SDK & Runtime)? [Y/n]: ");

        // 5. Both declined -> return explicit error and stop.
        if (!installPostgres)
        {
            logger.LogError("[Setup 5/7] Cannot continue: neither Docker nor PostgreSQL is installed, and both installations were declined.");
            return (false, ExecutionMode.Local);
        }

        // 7. Inform local execution + install PostgreSQL and .NET 10 SDK/Runtime dependencies.
        logger.LogInformation("[Setup 7/7] Applications and the PostgreSQL database will be installed and executed locally with required dependencies (.NET 10 SDK and Runtime).");
        bool localInstalled = await InstallLocalStackAsync(cancellationToken).ConfigureAwait(false);
        return (localInstalled, ExecutionMode.Local);
    }

    /// <summary>
    /// Inspects Docker containers to find an existing PostgreSQL container (<c>tools_postgres</c> or any <c>postgres</c> image bound to <paramref name="targetPort"/>).
    /// </summary>
    /// <param name="targetPort">Target PostgreSQL host port.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching <see cref="DockerPostgresContainerInfo"/>, or <see langword="null"/> if none was found.</returns>
    public async Task<DockerPostgresContainerInfo?> DetectDockerPostgresAsync(int targetPort, CancellationToken cancellationToken = default)
    {
        (int exitCode, string output, _) = await ProcessRunner.RunCommandAsync(
            "docker",
            "ps -a --format \"{{.Names}}|{{.Image}}|{{.State}}|{{.Ports}}\"",
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (exitCode != 0 || string.IsNullOrWhiteSpace(output))
            return null;

        return ParseDockerPostgresContainer(output, targetPort);
    }

    /// <summary>
    /// Parses <c>docker ps -a</c> pipe-delimited output (<c>Names|Image|State|Ports</c>) to locate a reusable PostgreSQL container.
    /// </summary>
    /// <param name="dockerPsOutput">Standard output from <c>docker ps -a --format "{{.Names}}|{{.Image}}|{{.State}}|{{.Ports}}"</c>.</param>
    /// <param name="targetPort">Target PostgreSQL host port.</param>
    /// <returns>The prioritized matching container, or <see langword="null"/> when none matches.</returns>
    public static DockerPostgresContainerInfo? ParseDockerPostgresContainer(string dockerPsOutput, int targetPort)
    {
        if (string.IsNullOrWhiteSpace(dockerPsOutput))
            return null;

        List<DockerPostgresContainerInfo> candidates = [];
        string[] lines = dockerPsOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (string line in lines)
        {
            string[] parts = line.Split('|');
            if (parts.Length < 3)
                continue;

            string name = parts[0].Trim();
            string image = parts[1].Trim();
            string state = parts[2].Trim();
            string ports = parts.Length >= 4 ? parts[3].Trim() : string.Empty;

            bool isToolsPostgres = string.Equals(name, "tools_postgres", StringComparison.OrdinalIgnoreCase);
            bool isPostgresImage = image.Contains("postgres", StringComparison.OrdinalIgnoreCase);
            if (!isToolsPostgres && !isPostgresImage)
                continue;

            bool isRunning = string.Equals(state, "running", StringComparison.OrdinalIgnoreCase);
            int? mappedPort = TryExtractMappedHostPort(ports, 5432);
            if (!isToolsPostgres && mappedPort.HasValue && mappedPort.Value != targetPort)
                continue;

            candidates.Add(new DockerPostgresContainerInfo(name, image, isRunning, mappedPort));
        }

        if (candidates.Count == 0)
            return null;

        foreach (DockerPostgresContainerInfo candidate in candidates)
        {
            if (string.Equals(candidate.Name, "tools_postgres", StringComparison.OrdinalIgnoreCase) && candidate.IsRunning)
                return candidate;
        }

        foreach (DockerPostgresContainerInfo candidate in candidates)
        {
            if (candidate.IsRunning && candidate.HostPort == targetPort)
                return candidate;
        }

        foreach (DockerPostgresContainerInfo candidate in candidates)
        {
            if (string.Equals(candidate.Name, "tools_postgres", StringComparison.OrdinalIgnoreCase))
                return candidate;
        }

        foreach (DockerPostgresContainerInfo candidate in candidates)
        {
            if (candidate.IsRunning)
                return candidate;
        }

        return candidates[0];
    }

    /// <summary>
    /// Ensures <c>git</c> CLI is available when the repository needs to be cloned from GitHub.
    /// </summary>
    public async Task<bool> EnsureGitInstalledAsync(CancellationToken cancellationToken = default)
    {
        (int exitCode, _, _) = await ProcessRunner.RunCommandAsync("git", "--version", cancellationToken: cancellationToken).ConfigureAwait(false);
        if (exitCode == 0)
            return true;

        logger.LogWarning("Git CLI was not found. Attempting to install Git...");
        int installCode;
        if (OperatingSystem.IsWindows())
            installCode = await ProcessRunner.RunInteractiveAsync("winget", "install --id Git.Git -e --accept-source-agreements --accept-package-agreements", cancellationToken).ConfigureAwait(false);
        else if (OperatingSystem.IsMacOS())
            installCode = await ProcessRunner.RunInteractiveAsync("brew", "install git", cancellationToken).ConfigureAwait(false);
        else if (OperatingSystem.IsLinux())
            installCode = await RunLinuxPackageInstallAsync("git", "git", "git", cancellationToken).ConfigureAwait(false);
        else
            installCode = -1;

        if (installCode != 0)
        {
            logger.LogError("Git installation failed. Please install Git manually and re-run the launcher.");
            return false;
        }

        (int verifyExit, _, _) = await ProcessRunner.RunCommandAsync("git", "--version", cancellationToken: cancellationToken).ConfigureAwait(false);
        return verifyExit == 0;
    }

    /// <summary>
    /// Ensures the Docker daemon is running, starting Docker Desktop / systemd service when possible.
    /// </summary>
    public static async Task<bool> EnsureDockerDaemonRunningAsync(ILogger logger, TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        if (await IsDockerDaemonRunningAsync(cancellationToken).ConfigureAwait(false))
        {
            logger.LogInformation("Docker daemon is already running.");
            return true;
        }

        logger.LogInformation("Starting Docker daemon...");
        if (OperatingSystem.IsWindows())
        {
            string dockerDesktopPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Docker", "Docker", "Docker Desktop.exe");
            if (File.Exists(dockerDesktopPath))
                Process.Start(new ProcessStartInfo { FileName = dockerDesktopPath, UseShellExecute = true });
        }
        else if (OperatingSystem.IsMacOS())
        {
            (int exitCode, _, string err) = await ProcessRunner.RunCommandAsync("open", "-a Docker", cancellationToken: cancellationToken).ConfigureAwait(false);
            if (exitCode != 0)
                logger.LogWarning("Could not launch Docker Desktop automatically: {Error}", err);
        }
        else if (OperatingSystem.IsLinux())
        {
            (int exitCode, _, string err) = await ProcessRunner.RunCommandAsync("systemctl", "start docker", cancellationToken: cancellationToken).ConfigureAwait(false);
            if (exitCode != 0)
                logger.LogWarning("Could not start docker service via systemctl: {Error}", err);
        }

        for (int attempt = 0; attempt < 30; attempt++)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), timeProvider, cancellationToken).ConfigureAwait(false);
            if (await IsDockerDaemonRunningAsync(cancellationToken).ConfigureAwait(false))
            {
                logger.LogInformation("Docker daemon is ready.");
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Probes whether PostgreSQL is accepting protocol queries using the configured connection string.
    /// </summary>
    public Task<bool> CanQueryPostgresAsync(CancellationToken cancellationToken) =>
        CanQueryPostgresAsync(connectionString, cancellationToken);

    /// <summary>
    /// Probes whether PostgreSQL is accepting protocol queries using <paramref name="targetConnectionString"/>.
    /// </summary>
    /// <param name="targetConnectionString">PostgreSQL connection string to test.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task<bool> CanQueryPostgresAsync(string targetConnectionString, CancellationToken cancellationToken)
    {
        try
        {
            NpgsqlConnectionStringBuilder builder = new(targetConnectionString)
            {
                Database = "postgres",
                Pooling = false,
                Timeout = 10,
                CommandTimeout = 10
            };
            await using NpgsqlConnection connection = new(builder.ConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using NpgsqlCommand command = new("SELECT 1;", connection);
            await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Polls PostgreSQL until it accepts protocol queries or times out.
    /// </summary>
    public Task<bool> WaitForPostgresReadyAsync(CancellationToken cancellationToken) =>
        WaitForPostgresReadyAsync(connectionString, cancellationToken);

    /// <summary>
    /// Polls PostgreSQL at <paramref name="targetConnectionString"/> until it accepts protocol queries or times out.
    /// </summary>
    /// <param name="targetConnectionString">PostgreSQL connection string to probe.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<bool> WaitForPostgresReadyAsync(string targetConnectionString, CancellationToken cancellationToken)
    {
        string host = ExtractHost(targetConnectionString);
        int port = ExtractPort(targetConnectionString);
        const int maxAttempts = 30;
        TimeSpan retryDelay = TimeSpan.FromSeconds(5);
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            if (await CanQueryPostgresAsync(targetConnectionString, cancellationToken).ConfigureAwait(false))
            {
                logger.LogInformation("PostgreSQL is ready and accepting protocol queries at {Host}:{Port}.", host, port);
                return true;
            }

            logger.LogInformation("({CurrentAttempt}/{MaxAttempt}) PostgreSQL is still initializing at {Host}:{Port}. Waiting {DelayTimeSpan} for next retry.", attempt, maxAttempts, host, port, retryDelay);
            await Task.Delay(retryDelay, timeProvider, cancellationToken).ConfigureAwait(false);
        }

        return false;
    }

    private async Task<bool> EnsureOrInstallDockerAsync(CancellationToken cancellationToken)
    {
        if (await IsDockerInstalledAsync(cancellationToken).ConfigureAwait(false))
            return await EnsureDockerDaemonRunningAsync(logger, timeProvider, cancellationToken).ConfigureAwait(false);

        bool installDocker = ConfirmInstall(
            allowDockerInstall,
            "Docker mode was requested, but Docker is not installed. Install Docker now? [Y/n]: ");
        if (!installDocker)
            return false;

        bool installed = await TryInstallDockerAsync(cancellationToken).ConfigureAwait(false);
        if (!installed)
            return false;

        return await EnsureDockerDaemonRunningAsync(logger, timeProvider, cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> InstallLocalStackAsync(CancellationToken cancellationToken)
    {
        bool postgresInstalled = await TryInstallLocalPostgresAsync(cancellationToken).ConfigureAwait(false);
        if (!postgresInstalled)
        {
            logger.LogError("Local PostgreSQL installation failed. Please install PostgreSQL manually and re-run the launcher.");
            return false;
        }

        if (!await EnsureLocalPostgresRunningAsync(cancellationToken).ConfigureAwait(false))
            return false;

        return await EnsureDotNet10InstalledAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> IsNativePostgresInstalledAsync(bool dockerPostgresOnTargetPort, CancellationToken cancellationToken)
    {
        if (dockerPostgresOnTargetPort)
            return false;

        if (await CanQueryPostgresAsync(cancellationToken).ConfigureAwait(false))
            return true;

        if (OperatingSystem.IsWindows())
        {
            (int svcExit, string svcOut, _) = await ProcessRunner.RunCommandAsync("powershell", "-NoProfile -Command \"Get-Service postgresql* -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty Name\"", cancellationToken: cancellationToken).ConfigureAwait(false);
            return svcExit == 0 && !string.IsNullOrWhiteSpace(svcOut);
        }

        (int serverExit, _, _) = await ProcessRunner.RunCommandAsync("postgres", "--version", cancellationToken: cancellationToken).ConfigureAwait(false);
        if (serverExit == 0)
            return true;

        if (OperatingSystem.IsLinux())
        {
            (int sysExit, string sysOut, _) = await ProcessRunner.RunCommandAsync("systemctl", "list-unit-files postgresql*", cancellationToken: cancellationToken).ConfigureAwait(false);
            if (sysExit != 0)
                return false;

            bool hasLinuxService = sysOut.Contains("postgresql", StringComparison.OrdinalIgnoreCase);
            return hasLinuxService;
        }

        if (OperatingSystem.IsMacOS())
        {
            (int brewExit, string brewOut, _) = await ProcessRunner.RunCommandAsync("brew", "services list", cancellationToken: cancellationToken).ConfigureAwait(false);
            if (brewExit != 0)
                return false;

            bool hasBrewService = brewOut.Contains("postgresql", StringComparison.OrdinalIgnoreCase);
            return hasBrewService;
        }

        return false;
    }

    private async Task<bool> EnsureLocalPostgresRunningAsync(CancellationToken cancellationToken)
    {
        if (await CanQueryPostgresAsync(cancellationToken).ConfigureAwait(false))
        {
            logger.LogInformation("Local PostgreSQL is already accepting connections at {Host}:{Port}.", _host, _port);
            return true;
        }

        logger.LogInformation("Attempting to start local PostgreSQL service...");
        (int startExit, _, string startErr) = OperatingSystem.IsWindows()
            ? await ProcessRunner.RunCommandAsync("powershell", "-NoProfile -Command \"Get-Service postgresql* | Start-Service\"", cancellationToken: cancellationToken).ConfigureAwait(false)
            : OperatingSystem.IsLinux()
                ? await ProcessRunner.RunCommandAsync("sh", "-c \"sudo systemctl start postgresql || systemctl start postgresql\"", cancellationToken: cancellationToken).ConfigureAwait(false)
                : await ProcessRunner.RunCommandAsync("brew", "services start postgresql@17 || brew services start postgresql", cancellationToken: cancellationToken).ConfigureAwait(false);

        if (startExit != 0)
            logger.LogWarning("Could not start local PostgreSQL service automatically: {Error}", startErr);

        bool ready = await WaitForPostgresReadyAsync(cancellationToken).ConfigureAwait(false);
        if (!ready)
            logger.LogError("Could not connect to local PostgreSQL on {Host}:{Port}. Verify credentials in --connection-string if PostgreSQL uses a custom password.", _host, _port);

        return ready;
    }

    private async Task<bool> TryInstallLocalPostgresAsync(CancellationToken cancellationToken)
    {
        if (OperatingSystem.IsWindows())
        {
            logger.LogInformation("Installing PostgreSQL 17 locally via winget...");
            int code = await ProcessRunner.RunInteractiveAsync("winget", "install --id PostgreSQL.PostgreSQL.17 -e --accept-source-agreements --accept-package-agreements", cancellationToken).ConfigureAwait(false);
            return code == 0;
        }

        if (OperatingSystem.IsMacOS())
        {
            logger.LogInformation("Installing PostgreSQL 17 locally via Homebrew...");
            int code = await ProcessRunner.RunInteractiveAsync("brew", "install postgresql@17", cancellationToken).ConfigureAwait(false);
            if (code == 0)
                await ProcessRunner.RunInteractiveAsync("brew", "services start postgresql@17", cancellationToken).ConfigureAwait(false);
            return code == 0;
        }

        if (OperatingSystem.IsLinux())
        {
            logger.LogInformation("Installing PostgreSQL locally via system package manager...");
            int code = await RunLinuxPackageInstallAsync("postgresql postgresql-contrib", "postgresql-server postgresql-contrib", "postgresql", cancellationToken).ConfigureAwait(false);
            return code == 0;
        }

        return false;
    }

    private async Task<bool> EnsureDotNet10InstalledAsync(CancellationToken cancellationToken)
    {
        if (await HasDotNet10SdkAndRuntimeAsync(cancellationToken).ConfigureAwait(false))
        {
            logger.LogInformation(".NET 10 SDK and Runtime detected.");
            return true;
        }

        logger.LogInformation("Installing .NET 10 SDK & Runtime locally...");
        int code;
        if (OperatingSystem.IsWindows())
        {
            code = await ProcessRunner.RunInteractiveAsync("winget", "install --id Microsoft.DotNet.SDK.10 -e --accept-source-agreements --accept-package-agreements", cancellationToken).ConfigureAwait(false);
        }
        else if (OperatingSystem.IsMacOS())
        {
            code = await ProcessRunner.RunInteractiveAsync("brew", "install --cask dotnet-sdk", cancellationToken).ConfigureAwait(false);
        }
        else if (OperatingSystem.IsLinux())
        {
            code = await ProcessRunner.RunInteractiveAsync("sh", "-c \"curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 10.0\"", cancellationToken).ConfigureAwait(false);
            string dotnetHome = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet");
            string currentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            if (Directory.Exists(dotnetHome) && !currentPath.Contains(dotnetHome, StringComparison.Ordinal))
                Environment.SetEnvironmentVariable("PATH", $"{dotnetHome}{Path.PathSeparator}{currentPath}");
        }
        else
        {
            code = -1;
        }

        if (code != 0)
        {
            logger.LogError(".NET 10 SDK installation did not complete automatically. Install .NET 10 SDK from https://dotnet.microsoft.com/download/dotnet/10.0 and re-run.");
            return false;
        }

        bool verified = await HasDotNet10SdkAndRuntimeAsync(cancellationToken).ConfigureAwait(false);
        if (!verified)
            logger.LogWarning(".NET 10 SDK was installed; if 'dotnet' is not on PATH yet, open a new terminal session and re-run the launcher.");

        return verified;
    }

    private static async Task<bool> HasDotNet10SdkAndRuntimeAsync(CancellationToken cancellationToken)
    {
        (int sdkExit, string sdkOut, _) = await ProcessRunner.RunCommandAsync("dotnet", "--list-sdks", cancellationToken: cancellationToken).ConfigureAwait(false);
        (int rtExit, string rtOut, _) = await ProcessRunner.RunCommandAsync("dotnet", "--list-runtimes", cancellationToken: cancellationToken).ConfigureAwait(false);
        if (sdkExit != 0 || rtExit != 0)
            return false;

        bool hasSdk10 = sdkOut.Contains("10.", StringComparison.Ordinal);
        bool hasAspNet10 = rtOut.Contains("Microsoft.AspNetCore.App 10.", StringComparison.OrdinalIgnoreCase);
        return hasSdk10 && hasAspNet10;
    }

    private static async Task<bool> IsDockerInstalledAsync(CancellationToken cancellationToken)
    {
        (int exitCode, _, _) = await ProcessRunner.RunCommandAsync("docker", "--version", cancellationToken: cancellationToken).ConfigureAwait(false);
        return exitCode == 0;
    }

    private static async Task<bool> IsDockerDaemonRunningAsync(CancellationToken cancellationToken)
    {
        (int exitCode, _, _) = await ProcessRunner.RunCommandAsync("docker", "info", cancellationToken: cancellationToken).ConfigureAwait(false);
        return exitCode == 0;
    }

    private async Task<bool> TryInstallDockerAsync(CancellationToken cancellationToken)
    {
        if (OperatingSystem.IsWindows())
        {
            logger.LogInformation("Installing Docker Desktop via winget ({Arch})...", RuntimeInformation.ProcessArchitecture);
            int code = await ProcessRunner.RunInteractiveAsync("winget", "install --id Docker.DockerDesktop -e --accept-source-agreements --accept-package-agreements", cancellationToken).ConfigureAwait(false);
            return code == 0;
        }

        if (OperatingSystem.IsMacOS())
        {
            logger.LogInformation("Installing Docker Desktop via Homebrew...");
            int code = await ProcessRunner.RunInteractiveAsync("brew", "install --cask docker", cancellationToken).ConfigureAwait(false);
            return code == 0;
        }

        if (OperatingSystem.IsLinux())
        {
            logger.LogInformation("Installing Docker Engine via official get.docker.com script...");
            int code = await ProcessRunner.RunInteractiveAsync("sh", "-c \"curl -fsSL https://get.docker.com | sh\"", cancellationToken).ConfigureAwait(false);
            return code == 0;
        }

        return false;
    }

    private static int? TryExtractMappedHostPort(string portsOutput, int containerPort)
    {
        if (string.IsNullOrWhiteSpace(portsOutput))
            return null;

        string targetSuffix = $"->{containerPort}/";
        string[] mappings = portsOutput.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (string mapping in mappings)
        {
            int arrowIndex = mapping.IndexOf(targetSuffix, StringComparison.OrdinalIgnoreCase);
            if (arrowIndex <= 0)
                continue;

            string hostPart = mapping[..arrowIndex];
            int colonIndex = hostPart.LastIndexOf(':');
            string portSegment = colonIndex >= 0 ? hostPart[(colonIndex + 1)..] : hostPart;
            if (int.TryParse(portSegment, NumberStyles.Integer, CultureInfo.InvariantCulture, out int port) && port is > 0 and <= 65535)
                return port;
        }

        return null;
    }

    private static Task<int> RunLinuxPackageInstallAsync(string aptPackages, string dnfPackages, string pacmanPackages, CancellationToken cancellationToken)
    {
        string script = $"if command -v apt-get >/dev/null 2>&1; then sudo apt-get update && sudo apt-get install -y {aptPackages}; " +
                        $"elif command -v dnf >/dev/null 2>&1; then sudo dnf install -y {dnfPackages}; " +
                        $"elif command -v pacman >/dev/null 2>&1; then sudo pacman -S --noconfirm {pacmanPackages}; " +
                        "else exit 1; fi";
        return ProcessRunner.RunInteractiveAsync("sh", $"-c \"{script}\"", cancellationToken);
    }

    private bool ConfirmInstall(bool? overrideFlag, string promptMessage)
    {
        if (overrideFlag.HasValue)
            return overrideFlag.Value;

        if (nonInteractive || Console.IsInputRedirected)
            return false;

        Console.Write($"  {promptMessage}");
        string? answer = Console.ReadLine()?.Trim();
        return answer is not null
            && (answer.Length == 0
                || answer.Equals("S", StringComparison.OrdinalIgnoreCase)
                || answer.Equals("Y", StringComparison.OrdinalIgnoreCase));
    }

    private static string ExtractHost(string connStr)
    {
        NpgsqlConnectionStringBuilder builder = new(connStr);
        return string.IsNullOrWhiteSpace(builder.Host) ? "localhost" : builder.Host;
    }

    private static int ExtractPort(string connStr)
    {
        NpgsqlConnectionStringBuilder builder = new(connStr);
        return builder.Port is > 0 and <= 65535 ? builder.Port : 5432;
    }
}
