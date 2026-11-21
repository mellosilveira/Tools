using System.Diagnostics;
using System.Net.Sockets;
using MelloSilveiraTools.Launcher.Infrastructure;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.Launcher.Steps;

/// <summary>
/// Validates and bootstraps the database infrastructure following the exact precedence:
/// Docker (start daemon + ensure containers) -&gt; local PostgreSQL (start + validate) -&gt; interactive or configured Docker installation.
/// </summary>
/// <param name="logger">Logger for execution diagnostics.</param>
/// <param name="currentStepNumber">Current 1-based step index.</param>
/// <param name="numberOfSteps">Total number of launcher steps.</param>
/// <param name="repositoryRoot">Root directory of the repository.</param>
/// <param name="host">PostgreSQL target host.</param>
/// <param name="port">PostgreSQL target port.</param>
/// <param name="allowDockerInstall">Optional override for automatic Docker installation (<c>true</c>/<c>false</c>), or <c>null</c> to prompt interactively.</param>
public sealed class DatabaseInfrastructureStep(
    ILogger<DatabaseInfrastructureStep> logger,
    int currentStepNumber,
    int numberOfSteps,
    string repositoryRoot,
    string host,
    int port,
    bool? allowDockerInstall)
{
    /// <summary>
    /// Ensures PostgreSQL is running and accepting connections on the configured host and port.
    /// </summary>
    public async Task<bool> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("[{CurrentStepNumber}/{NumberOfSteps}] Checking container & database infrastructure...", currentStepNumber, numberOfSteps);

        if (await IsDockerInstalledAsync(cancellationToken).ConfigureAwait(false))
        {
            logger.LogInformation("Docker detected.");
            if (!await EnsureDockerDaemonRunningAsync(cancellationToken).ConfigureAwait(false))
            {
                logger.LogError("Docker is installed, but the Docker daemon could not be started.");
                return false;
            }

            return await EnsureDockerComposeDependenciesAsync(cancellationToken).ConfigureAwait(false);
        }

        logger.LogInformation("Docker is not installed. Checking for local PostgreSQL installation...");
        if (await IsLocalPostgresInstalledAsync(cancellationToken).ConfigureAwait(false))
        {
            logger.LogInformation("Local PostgreSQL detected.");
            return await EnsureLocalPostgresRunningAsync(cancellationToken).ConfigureAwait(false);
        }

        logger.LogWarning("Neither Docker nor local PostgreSQL was found on this machine.");

        bool allowInstall;
        if (allowDockerInstall.HasValue)
        {
            allowInstall = allowDockerInstall.Value;
        }
        else if (Console.IsInputRedirected)
        {
            allowInstall = false;
        }
        else
        {
            Console.Write("  Do you allow downloading and installing Docker now? [Y/n]: ");
            string? answer = Console.ReadLine()?.Trim();
            allowInstall = answer is not null
                && (answer.Length == 0
                    || answer.Equals("S", StringComparison.OrdinalIgnoreCase)
                    || answer.Equals("Y", StringComparison.OrdinalIgnoreCase));
        }

        if (allowInstall)
        {
            bool installed = await TryInstallDockerAsync(cancellationToken).ConfigureAwait(false);
            if (!installed)
            {
                logger.LogError("Docker installation did not complete automatically. Please finish installing Docker and re-run the launcher.");
                return false;
            }

            if (!await EnsureDockerDaemonRunningAsync(cancellationToken).ConfigureAwait(false))
            {
                logger.LogInformation("Docker was installed, but may require starting Docker Desktop or rebooting/re-logging in before continuing.");
                return false;
            }

            return await EnsureDockerComposeDependenciesAsync(cancellationToken).ConfigureAwait(false);
        }

        logger.LogError("Docker (or a local PostgreSQL instance) is mandatory to run MelloSilveiraTools locally. Exiting.");
        return false;
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

    private async Task<bool> EnsureDockerDaemonRunningAsync(CancellationToken cancellationToken)
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
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
            if (await IsDockerDaemonRunningAsync(cancellationToken).ConfigureAwait(false))
            {
                logger.LogInformation("Docker daemon is ready.");
                return true;
            }
        }

        return false;
    }

    private async Task<bool> EnsureDockerComposeDependenciesAsync(CancellationToken cancellationToken)
    {
        (int psExit, string psOut, _) = await ProcessRunner.RunCommandAsync(
            "docker",
            "ps --filter \"name=tools_postgres\" --filter \"status=running\" --format \"{{.Names}}\"",
            repositoryRoot,
            cancellationToken).ConfigureAwait(false);

        if (psExit == 0 && psOut.Contains("tools_postgres", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("Container 'tools_postgres' is already running.");
            return await WaitForTcpPortAsync(cancellationToken).ConfigureAwait(false);
        }

        logger.LogInformation("Starting PostgreSQL container via docker compose...");
        (int composeExit, string stdOut, string stdErr) = await ProcessRunner.RunCommandAsync(
            "docker",
            "compose up -d postgres",
            repositoryRoot,
            cancellationToken).ConfigureAwait(false);

        if (composeExit != 0)
        {
            logger.LogError("'docker compose up -d postgres' failed: {StdErr} {StdOut}", stdErr, stdOut);
            return false;
        }

        return await WaitForTcpPortAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<bool> IsLocalPostgresInstalledAsync(CancellationToken cancellationToken)
    {
        (int psqlExit, _, _) = await ProcessRunner.RunCommandAsync("psql", "--version", cancellationToken: cancellationToken).ConfigureAwait(false);
        if (psqlExit == 0)
            return true;

        (int readyExit, _, _) = await ProcessRunner.RunCommandAsync("pg_isready", "--version", cancellationToken: cancellationToken).ConfigureAwait(false);
        return readyExit == 0;
    }

    private async Task<bool> EnsureLocalPostgresRunningAsync(CancellationToken cancellationToken)
    {
        if (await IsTcpPortOpenAsync(cancellationToken).ConfigureAwait(false))
        {
            logger.LogInformation("Local PostgreSQL is already listening on {Host}:{Port}.", host, port);
            return true;
        }

        logger.LogInformation("Attempting to start local PostgreSQL service...");
        (int startExit, _, string startErr) = OperatingSystem.IsWindows()
            ? await ProcessRunner.RunCommandAsync("powershell", "-Command \"Get-Service postgresql* | Start-Service\"", cancellationToken: cancellationToken).ConfigureAwait(false)
            : OperatingSystem.IsLinux()
                ? await ProcessRunner.RunCommandAsync("systemctl", "start postgresql", cancellationToken: cancellationToken).ConfigureAwait(false)
                : await ProcessRunner.RunCommandAsync("brew", "services start postgresql", cancellationToken: cancellationToken).ConfigureAwait(false);

        if (startExit != 0)
            logger.LogWarning("Failed to start local PostgreSQL service automatically: {Error}", startErr);

        bool open = await WaitForTcpPortAsync(cancellationToken).ConfigureAwait(false);
        if (!open)
            logger.LogError("Could not connect to local PostgreSQL on {Host}:{Port}.", host, port);

        return open;
    }

    private async Task<bool> TryInstallDockerAsync(CancellationToken cancellationToken)
    {
        if (OperatingSystem.IsWindows())
        {
            logger.LogInformation("Installing Docker Desktop via winget...");
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

    private async Task<bool> WaitForTcpPortAsync(CancellationToken cancellationToken)
    {
        int maxAttempt = 30;
        for (int attempt = 0; attempt < maxAttempt; attempt++)
        {
            if (await IsTcpPortOpenAsync(cancellationToken).ConfigureAwait(false))
            {
                logger.LogInformation("PostgreSQL is accepting connections at {Host}:{Port}.", host, port);
                return true;
            }

            TimeSpan delayTimeSpan = TimeSpan.FromSeconds(5);
            logger.LogInformation("({CurrentAttemp}/{MaxAttempt}) PostgreSQL is still not accepting connections at {Host}:{Port}. Waiting {DailayTimeSpan} for next retry.", attempt + 1, maxAttempt, host, port, delayTimeSpan);
            await Task.Delay(delayTimeSpan, cancellationToken).ConfigureAwait(false);
        }

        return false;
    }

    private async Task<bool> IsTcpPortOpenAsync(CancellationToken cancellationToken)
    {
        try
        {
            using TcpClient client = new();
            await client.ConnectAsync(host, port, cancellationToken).ConfigureAwait(false);
            return client.Connected;
        }
        catch
        {
            return false;
        }
    }
}
