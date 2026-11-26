using MelloSilveiraTools.Launcher.Infrastructure;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Globalization;

namespace MelloSilveiraTools.Launcher.Steps;

/// <summary>
/// Builds and starts all self-contained Docker Compose services (<c>postgres</c>, <c>migrator</c>, and <c>webapi</c>)
/// and waits for the Web API host to become healthy.
/// </summary>
/// <param name="logger">Logger for execution diagnostics.</param>
/// <param name="repositoryRoot">Root directory containing <c>docker-compose.yml</c>.</param>
/// <param name="options">Resolved launcher infrastructure options.</param>
/// <param name="secrets">Resolved runtime secrets passed to Docker Compose interpolation.</param>
public sealed class DockerComposeStep(
    ILogger<DockerComposeStep> logger,
    string repositoryRoot,
    LauncherInfrastructureOptions options,
    LauncherRuntimeSecrets secrets)
{
    /// <summary>
    /// Ensures Docker is available, builds/starts the Compose stack, and probes <c>/health</c>.
    /// </summary>
    public async Task<bool> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("[1/2] Checking Docker availability for self-contained container stack...");

        if (!await DatabaseInfrastructureStep.EnsureDockerDaemonRunningAsync(logger, cancellationToken).ConfigureAwait(false))
        {
            logger.LogError("Docker daemon is not running or Docker is not installed. Start Docker and re-run with --docker.");
            return false;
        }

        logger.LogInformation("[2/2] Building and starting self-contained Docker Compose stack (postgres + migrator + webapi)...");
        ProcessStartInfo upInfo = CreateComposeStartInfo("compose up -d --build");

        using Process? upProcess = Process.Start(upInfo);
        if (upProcess is null)
        {
            logger.LogError("Failed to start 'docker compose up -d --build'.");
            return false;
        }

        await upProcess.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        if (upProcess.ExitCode != 0)
        {
            logger.LogError("'docker compose up -d --build' failed with exit code {ExitCode}.", upProcess.ExitCode);
            return false;
        }

        return await LauncherHostProbe.WaitForHealthyAsync(
            logger,
            options.BuildWebApiBaseUrl(),
            options.OpenBrowser,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Waits until cancellation is requested (<c>Ctrl+C</c>) and stops the Docker Compose stack.
    /// </summary>
    public async Task WaitForShutdownAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Press Ctrl+C to stop the Docker Compose stack...");
        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }

        logger.LogInformation("Stopping Docker Compose stack...");
        ProcessStartInfo downInfo = CreateComposeStartInfo("compose down");
        using Process? downProcess = Process.Start(downInfo);
        if (downProcess is not null)
            await downProcess.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
    }

    private ProcessStartInfo CreateComposeStartInfo(string arguments)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = "docker",
            Arguments = arguments,
            WorkingDirectory = repositoryRoot,
            UseShellExecute = false
        };

        (_, int postgresPort) = options.ExtractHostAndPort();
        startInfo.Environment["TOOLS_POSTGRES_PORT"] = postgresPort.ToString(CultureInfo.InvariantCulture);
        startInfo.Environment["TOOLS_WEBAPI_PORT"] = options.WebApiPort.ToString(CultureInfo.InvariantCulture);
        startInfo.Environment["TOOLS_MIGRATION_CONTEXT"] = options.MigrationContext.ToString();
        startInfo.Environment["TOOLS_MIGRATION_TIMEOUT"] = options.MigrationTimeoutSeconds.ToString(CultureInfo.InvariantCulture);
        startInfo.Environment["TOOLS_DOCKER_MIGRATOR_CONNECTION_STRING"] = options.BuildDockerConnectionString();
        startInfo.Environment["TOOLS_DOCKER_APP_CONNECTION_STRING"] = options.BuildApplicationConnectionString(forDocker: true);
        secrets.ApplyToEnvironment(startInfo.Environment, uppercaseKeys: true);
        return startInfo;
    }
}
