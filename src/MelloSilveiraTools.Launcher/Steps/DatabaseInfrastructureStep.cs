using MelloSilveiraTools.Launcher.Infrastructure;
using System.Diagnostics;
using System.Net.Sockets;

namespace MelloSilveiraTools.Launcher.Steps;

/// <summary>
/// Validates and bootstraps the database infrastructure following the exact precedence:
/// Docker (start daemon + ensure containers) -&gt; local PostgreSQL (start + validate) -&gt; interactive Docker installation prompt.
/// </summary>
public sealed class DatabaseInfrastructureStep(string repositoryRoot, string host = "localhost", int port = 5432)
{
    /// <summary>
    /// Ensures PostgreSQL is running and accepting connections on the configured host and port.
    /// </summary>
    public async Task<bool> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("[1/3] Checking container & database infrastructure...");

        if (await IsDockerInstalledAsync(cancellationToken).ConfigureAwait(false))
        {
            Console.WriteLine("  -> Docker detected.");
            if (!await EnsureDockerDaemonRunningAsync(cancellationToken).ConfigureAwait(false))
            {
                Console.WriteLine("  [ERROR] Docker is installed, but the Docker daemon could not be started.");
                return false;
            }

            return await EnsureDockerComposeDependenciesAsync(cancellationToken).ConfigureAwait(false);
        }

        Console.WriteLine("  -> Docker is not installed. Checking for local PostgreSQL installation...");
        if (await IsLocalPostgresInstalledAsync(cancellationToken).ConfigureAwait(false))
        {
            Console.WriteLine("  -> Local PostgreSQL detected.");
            return await EnsureLocalPostgresRunningAsync(cancellationToken).ConfigureAwait(false);
        }

        Console.WriteLine("  [WARNING] Neither Docker nor local PostgreSQL was found on this machine.");
        Console.Write("  Do you allow downloading and installing Docker now? [Y/n]: ");
        string? answer = Console.ReadLine()?.Trim();

        bool allowInstall = answer is not null
            && ((answer.Length == 0 && !Console.IsInputRedirected)
                || answer.Equals("S", StringComparison.OrdinalIgnoreCase)
                || answer.Equals("Y", StringComparison.OrdinalIgnoreCase));

        if (allowInstall)
        {
            bool installed = await TryInstallDockerAsync(cancellationToken).ConfigureAwait(false);
            if (!installed)
            {
                Console.WriteLine("  [ERROR] Docker installation did not complete automatically. Please finish installing Docker and re-run the launcher.");
                return false;
            }

            if (!await EnsureDockerDaemonRunningAsync(cancellationToken).ConfigureAwait(false))
            {
                Console.WriteLine("  [INFO] Docker was installed, but may require starting Docker Desktop or rebooting/re-logging in before continuing.");
                return false;
            }

            return await EnsureDockerComposeDependenciesAsync(cancellationToken).ConfigureAwait(false);
        }

        Console.WriteLine("  [REQUIRED] Docker (or a local PostgreSQL instance) is mandatory to run MelloSilveiraTools locally. Exiting.");
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

    private static async Task<bool> EnsureDockerDaemonRunningAsync(CancellationToken cancellationToken)
    {
        if (await IsDockerDaemonRunningAsync(cancellationToken).ConfigureAwait(false))
        {
            Console.WriteLine("  -> Docker daemon is already running.");
            return true;
        }

        Console.WriteLine("  -> Starting Docker daemon...");
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
                Console.WriteLine($"  [WARNING] Could not launch Docker Desktop automatically: {err}");
        }
        else if (OperatingSystem.IsLinux())
        {
            (int exitCode, _, string err) = await ProcessRunner.RunCommandAsync("systemctl", "start docker", cancellationToken: cancellationToken).ConfigureAwait(false);
            if (exitCode != 0)
                Console.WriteLine($"  [WARNING] Could not start docker service via systemctl: {err}");
        }

        for (int attempt = 0; attempt < 30; attempt++)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
            if (await IsDockerDaemonRunningAsync(cancellationToken).ConfigureAwait(false))
            {
                Console.WriteLine("  -> Docker daemon is ready.");
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
            Console.WriteLine("  -> Container 'tools_postgres' is already running.");
            return await WaitForTcpPortAsync(cancellationToken).ConfigureAwait(false);
        }

        Console.WriteLine("  -> Starting PostgreSQL container via docker compose...");
        (int composeExit, string stdOut, string stdErr) = await ProcessRunner.RunCommandAsync(
            "docker",
            "compose up -d postgres",
            repositoryRoot,
            cancellationToken).ConfigureAwait(false);

        if (composeExit != 0)
        {
            Console.WriteLine($"  [ERROR] 'docker compose up -d postgres' failed: {stdErr} {stdOut}");
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
            Console.WriteLine($"  -> Local PostgreSQL is already listening on {host}:{port}.");
            return true;
        }

        Console.WriteLine("  -> Attempting to start local PostgreSQL service...");
        (int startExit, _, string startErr) = OperatingSystem.IsWindows()
            ? await ProcessRunner.RunCommandAsync("powershell", "-Command \"Get-Service postgresql* | Start-Service\"", cancellationToken: cancellationToken).ConfigureAwait(false)
            : OperatingSystem.IsLinux()
                ? await ProcessRunner.RunCommandAsync("systemctl", "start postgresql", cancellationToken: cancellationToken).ConfigureAwait(false)
                : await ProcessRunner.RunCommandAsync("brew", "services start postgresql", cancellationToken: cancellationToken).ConfigureAwait(false);

        if (startExit != 0)
            Console.WriteLine($"  [WARNING] Failed to start local PostgreSQL service automatically: {startErr}");

        bool open = await WaitForTcpPortAsync(cancellationToken).ConfigureAwait(false);
        if (!open)
            Console.WriteLine($"  [ERROR] Could not connect to local PostgreSQL on {host}:{port}.");

        return open;
    }

    private static async Task<bool> TryInstallDockerAsync(CancellationToken cancellationToken)
    {
        if (OperatingSystem.IsWindows())
        {
            Console.WriteLine("  -> Installing Docker Desktop via winget...");
            int code = await ProcessRunner.RunInteractiveAsync("winget", "install --id Docker.DockerDesktop -e --accept-source-agreements --accept-package-agreements", cancellationToken).ConfigureAwait(false);
            return code == 0;
        }

        if (OperatingSystem.IsMacOS())
        {
            Console.WriteLine("  -> Installing Docker Desktop via Homebrew...");
            int code = await ProcessRunner.RunInteractiveAsync("brew", "install --cask docker", cancellationToken).ConfigureAwait(false);
            return code == 0;
        }

        if (OperatingSystem.IsLinux())
        {
            Console.WriteLine("  -> Installing Docker Engine via official get.docker.com script...");
            int code = await ProcessRunner.RunInteractiveAsync("sh", "-c \"curl -fsSL https://get.docker.com | sh\"", cancellationToken).ConfigureAwait(false);
            return code == 0;
        }

        return false;
    }

    private async Task<bool> WaitForTcpPortAsync(CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            if (await IsTcpPortOpenAsync(cancellationToken).ConfigureAwait(false))
            {
                Console.WriteLine($"  -> PostgreSQL is accepting connections at {host}:{port}.");
                return true;
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
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
