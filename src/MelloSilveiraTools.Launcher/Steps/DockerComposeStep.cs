using MelloSilveiraTools.Launcher.Infrastructure;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Globalization;

namespace MelloSilveiraTools.Launcher.Steps;

/// <summary>
/// Orchestrates the Docker Compose stack sequentially:
/// reuses an existing Docker PostgreSQL container when present (or starts Compose <c>postgres</c> with interactive port-conflict resolution),
/// executes and validates <c>migrator</c>, removes the <c>migrator</c> container (<c>docker compose rm -f migrator</c>),
/// and starts the backend/frontend host (<c>webapi</c>).
/// </summary>
/// <param name="logger">Logger for execution diagnostics.</param>
/// <param name="repositoryRoot">Root directory containing <c>docker-compose.yml</c>.</param>
/// <param name="options">Resolved launcher infrastructure options.</param>
/// <param name="secrets">Resolved runtime secrets passed to Docker Compose interpolation.</param>
/// <param name="dependencyInstaller">Dependency installer providing Docker PostgreSQL detection and readiness checks.</param>
/// <param name="portConflictResolver">Interactive port conflict resolver for PostgreSQL and Web API ports.</param>
/// <param name="httpsCertificateStep">HTTPS certificate provisioning step allowing custom or local certificate setup.</param>
/// <param name="timeProvider">Clock provider used for health polling delays.</param>
public sealed class DockerComposeStep(
    ILogger<DockerComposeStep> logger,
    string repositoryRoot,
    LauncherInfrastructureOptions options,
    LauncherRuntimeSecrets secrets,
    DependencyInstaller dependencyInstaller,
    PortConflictResolver portConflictResolver,
    HttpsCertificateStep httpsCertificateStep,
    TimeProvider timeProvider)
{
    private LauncherInfrastructureOptions _options = options;
    private string? _hostCertificateFile;
    private string? _containerCertificatePassword;

    /// <summary>
    /// Ensures PostgreSQL is ready (reusing any existing Docker PostgreSQL container without pulling a new image),
    /// runs and tears down the migrator container after table validation, and starts the Web API &amp; Frontend container.
    /// </summary>
    public async Task<bool> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("[1/3] Checking existing Docker PostgreSQL containers or starting PostgreSQL via Docker Compose...");
        (bool postgresReady, bool useExternalHostGateway) = await EnsureDockerPostgresReadyAsync(cancellationToken).ConfigureAwait(false);
        if (!postgresReady)
            return false;

        logger.LogInformation("[2/3] Running database migrator container, validating tables, and tearing down migrator...");
        int migratorExit = await RunComposeInteractiveAsync("compose up --build --no-deps --exit-code-from migrator migrator", useExternalHostGateway, cancellationToken).ConfigureAwait(false);
        if (migratorExit != 0)
        {
            logger.LogError("Docker migrator failed with exit code {ExitCode}.", migratorExit);
            return false;
        }

        bool tablesValid = await DatabaseMigrationStep.ValidateTablesCreatedAsync(
            logger,
            _options.ConnectionString,
            _options.MigrationContext,
            cancellationToken).ConfigureAwait(false);
        if (!tablesValid)
            return false;

        int rmExit = await RunComposeInteractiveAsync("compose rm -f migrator", useExternalHostGateway, cancellationToken).ConfigureAwait(false);
        if (rmExit != 0)
            logger.LogWarning("'docker compose rm -f migrator' exited with code {ExitCode}.", rmExit);
        else
            logger.LogInformation("Temporary 'migrator' container removed.");

        logger.LogInformation("[3/3] Preparing HTTPS certificate and starting Web API & Frontend container (webapi)...");
        string certsDir = Path.Combine(repositoryRoot, "build", "certs");
        (bool certOk, string? preparedCertPath, string? preparedCertPassword) = await httpsCertificateStep
            .PrepareCertificateInDirectoryAsync(certsDir, allowDockerSdkFallback: true, cancellationToken)
            .ConfigureAwait(false);
        if (!certOk)
            return false;

        if (!string.IsNullOrWhiteSpace(preparedCertPath))
        {
            _hostCertificateFile = preparedCertPath;
            _containerCertificatePassword = preparedCertPassword ?? string.Empty;
        }

        if (!await EnsureDockerWebApiReadyAsync(useExternalHostGateway, cancellationToken).ConfigureAwait(false))
            return false;

        string baseUrl = _options.BuildWebApiBaseUrl();
        string httpsUrl = _options.BuildWebApiHttpsUrl();
        bool healthy = await LauncherHostProbe.WaitForHealthyAsync(
            logger,
            baseUrl,
            httpsBaseUrl: httpsUrl,
            timeProvider: timeProvider,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!healthy)
            return false;

        LauncherHostProbe.PromptAndOpenBrowserIfRequested(logger, baseUrl, _options.OpenBrowser, _options.NonInteractive);
        return true;
    }

    /// <summary>
    /// Waits until cancellation is requested (<c>Ctrl+C</c>) and stops the Docker Compose stack.
    /// </summary>
    public async Task WaitForShutdownAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Press Ctrl+C to stop the Docker Compose stack...");
        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, timeProvider, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }

        logger.LogInformation("Stopping Docker Compose stack...");
        ProcessStartInfo downInfo = CreateComposeStartInfo("compose down", useHostGateway: false);
        using Process? downProcess = Process.Start(downInfo);
        if (downProcess is not null)
            await downProcess.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
    }

    private async Task<(bool Success, bool UseExternalHostGateway)> EnsureDockerPostgresReadyAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            (string pgHost, int pgPort) = _options.ExtractHostAndPort();
            DockerPostgresContainerInfo? existing = await dependencyInstaller.DetectDockerPostgresAsync(pgPort, cancellationToken).ConfigureAwait(false);
            if (existing.HasValue)
            {
                DockerPostgresContainerInfo container = existing.Value;
                bool wasRunning = container.IsRunning;
                if (!wasRunning)
                {
                    logger.LogInformation(
                        "Found stopped Docker PostgreSQL container '{ContainerName}' ({Image}). Starting existing container without pulling a new version...",
                        container.Name,
                        container.Image);
                    (int startExit, _, string startErr) = await ProcessRunner.RunCommandAsync("docker", $"start {container.Name}", cancellationToken: cancellationToken).ConfigureAwait(false);
                    if (startExit != 0)
                        logger.LogWarning("Could not start existing container '{ContainerName}': {Error}", container.Name, startErr);
                    else
                        container = await dependencyInstaller.DetectDockerPostgresAsync(pgPort, cancellationToken).ConfigureAwait(false) ?? container;
                }
                else
                {
                    logger.LogInformation(
                        "Found running Docker PostgreSQL container '{ContainerName}' ({Image}). Reusing existing container without pulling a new version.",
                        container.Name,
                        container.Image);
                }

                if (container.HostPort.HasValue && container.HostPort.Value != pgPort)
                {
                    logger.LogInformation("Adjusting target PostgreSQL port from {OldPort} to existing container port {NewPort}.", pgPort, container.HostPort.Value);
                    _options = _options.WithPostgresPort(container.HostPort.Value);
                }

                bool external = !string.Equals(container.Name, "tools_postgres", StringComparison.OrdinalIgnoreCase);
                bool containerReady = wasRunning
                    ? await DependencyInstaller.CanQueryPostgresAsync(_options.ConnectionString, cancellationToken).ConfigureAwait(false)
                    : await dependencyInstaller.WaitForPostgresReadyAsync(_options.ConnectionString, cancellationToken).ConfigureAwait(false);
                if (containerReady)
                    return (true, external);

                logger.LogWarning(
                    "Existing Docker PostgreSQL container '{ContainerName}' on port {Port} did not accept the configured credentials or is still unavailable.",
                    container.Name,
                    _options.ExtractHostAndPort().Port);

                (bool shouldRetryExisting, int chosenPortExisting) = portConflictResolver.PromptPortResolution("PostgreSQL", _options.ExtractHostAndPort().Port);
                if (!shouldRetryExisting)
                    return (false, external);

                _options = _options.WithPostgresPort(chosenPortExisting);
                continue;
            }

            (bool portAvailable, int resolvedPort, bool reusedPostgres) = await portConflictResolver.EnsurePortAvailableOrResolveAsync(
                "PostgreSQL",
                pgHost,
                pgPort,
                port => DependencyInstaller.CanQueryPostgresAsync(_options.WithPostgresPort(port).ConnectionString, cancellationToken),
                cancellationToken).ConfigureAwait(false);

            if (!portAvailable)
                return (false, false);

            if (resolvedPort != pgPort)
                _options = _options.WithPostgresPort(resolvedPort);

            if (reusedPostgres)
            {
                logger.LogInformation("PostgreSQL is already accepting connections on {Host}:{Port}. Reusing existing instance.", pgHost, resolvedPort);
                return (true, true);
            }

            (int composeExit, string stdOut, string stdErr) = await RunComposeCapturedAsync("compose up -d --wait postgres", useHostGateway: false, cancellationToken).ConfigureAwait(false);
            if (composeExit == 0)
            {
                bool ready = await dependencyInstaller.WaitForPostgresReadyAsync(_options.ConnectionString, cancellationToken).ConfigureAwait(false);
                if (ready)
                    return (true, false);

                logger.LogError("PostgreSQL container started, but did not accept protocol queries.");
                return (false, false);
            }

            string combinedError = $"{stdErr}{Environment.NewLine}{stdOut}";
            bool isPortConflict = combinedError.Contains("port is already allocated", StringComparison.OrdinalIgnoreCase)
                || combinedError.Contains("address already in use", StringComparison.OrdinalIgnoreCase)
                || PortConflictResolver.IsLocalPortInUse(pgHost, resolvedPort);

            if (!isPortConflict)
            {
                logger.LogError("'docker compose up -d --wait postgres' failed: {StdErr} {StdOut}", stdErr, stdOut);
                return (false, false);
            }

            logger.LogWarning("Docker could not bind PostgreSQL port {Port} because it is already in use.", resolvedPort);
            (bool shouldRetry, int nextPort) = portConflictResolver.PromptPortResolution("PostgreSQL", resolvedPort);
            if (!shouldRetry)
                return (false, false);

            _options = _options.WithPostgresPort(nextPort);
        }
    }

    private async Task<bool> EnsureDockerWebApiReadyAsync(bool useExternalHostGateway, CancellationToken cancellationToken)
    {
        while (true)
        {
            (bool httpAvailable, int resolvedHttpPort, bool alreadyHealthy) = await portConflictResolver.EnsurePortAvailableOrResolveAsync(
                "WebApi.Host / WebUi (HTTP)",
                "localhost",
                _options.WebApiPort,
                port => LauncherHostProbe.IsHealthyAsync($"http://localhost:{port}", cancellationToken),
                cancellationToken).ConfigureAwait(false);

            if (!httpAvailable)
                return false;

            if (resolvedHttpPort != _options.WebApiPort)
                _options = _options.WithWebApiPort(resolvedHttpPort);

            if (alreadyHealthy)
            {
                logger.LogInformation("WebApi.Host / WebUi is already running and healthy at {BaseUrl}.", _options.BuildWebApiBaseUrl());
                return true;
            }

            (bool httpsAvailable, int resolvedHttpsPort, _) = await portConflictResolver.EnsurePortAvailableOrResolveAsync(
                "WebApi.Host / WebUi (HTTPS)",
                "localhost",
                _options.WebApiHttpsPort,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (!httpsAvailable)
                return false;

            if (resolvedHttpsPort != _options.WebApiHttpsPort)
                _options = _options.WithWebApiHttpsPort(resolvedHttpsPort);

            (int webApiExit, string stdOut, string stdErr) = await RunComposeCapturedAsync("compose up -d --no-deps --build webapi", useExternalHostGateway, cancellationToken).ConfigureAwait(false);
            if (webApiExit == 0)
                return true;

            string combinedError = $"{stdErr}{Environment.NewLine}{stdOut}";
            bool httpInUse = PortConflictResolver.IsLocalPortInUse("localhost", resolvedHttpPort);
            bool httpsInUse = PortConflictResolver.IsLocalPortInUse("localhost", resolvedHttpsPort);
            bool isPortConflict = combinedError.Contains("port is already allocated", StringComparison.OrdinalIgnoreCase)
                || combinedError.Contains("address already in use", StringComparison.OrdinalIgnoreCase)
                || httpInUse
                || httpsInUse;

            if (!isPortConflict)
            {
                logger.LogError("'docker compose up -d --no-deps --build webapi' failed: {StdErr} {StdOut}", stdErr, stdOut);
                return false;
            }

            if (httpsInUse && !httpInUse)
            {
                logger.LogWarning("Docker could not bind WebApi.Host HTTPS port {Port} because it is already in use.", resolvedHttpsPort);
                (bool shouldRetryHttps, int nextHttpsPort) = portConflictResolver.PromptPortResolution("WebApi.Host / WebUi (HTTPS)", resolvedHttpsPort);
                if (!shouldRetryHttps)
                    return false;

                _options = _options.WithWebApiHttpsPort(nextHttpsPort);
            }
            else
            {
                logger.LogWarning("Docker could not bind WebApi.Host HTTP port {Port} because it is already in use.", resolvedHttpPort);
                (bool shouldRetryHttp, int nextHttpPort) = portConflictResolver.PromptPortResolution("WebApi.Host / WebUi (HTTP)", resolvedHttpPort);
                if (!shouldRetryHttp)
                    return false;

                _options = _options.WithWebApiPort(nextHttpPort);
            }
        }
    }

    private async Task<int> RunComposeInteractiveAsync(string arguments, bool useHostGateway, CancellationToken cancellationToken)
    {
        ProcessStartInfo startInfo = CreateComposeStartInfo(arguments, useHostGateway);
        using Process? process = Process.Start(startInfo);
        if (process is null)
            return -1;

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        return process.ExitCode;
    }

    private async Task<(int ExitCode, string StdOut, string StdErr)> RunComposeCapturedAsync(string arguments, bool useHostGateway, CancellationToken cancellationToken)
    {
        Dictionary<string, string> env = BuildComposeEnvironment(useHostGateway);
        return await ProcessRunner.RunCommandAsync("docker", arguments, repositoryRoot, env, cancellationToken).ConfigureAwait(false);
    }

    private ProcessStartInfo CreateComposeStartInfo(string arguments, bool useHostGateway)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = "docker",
            Arguments = arguments,
            WorkingDirectory = repositoryRoot,
            UseShellExecute = false
        };

        foreach (KeyValuePair<string, string> pair in BuildComposeEnvironment(useHostGateway))
            startInfo.Environment[pair.Key] = pair.Value;

        return startInfo;
    }

    private Dictionary<string, string> BuildComposeEnvironment(bool useHostGateway)
    {
        (_, int postgresPort) = _options.ExtractHostAndPort();
        Dictionary<string, string> env = new()
        {
            ["TOOLS_POSTGRES_PORT"] = postgresPort.ToString(CultureInfo.InvariantCulture),
            ["TOOLS_WEBAPI_HTTP_PORT"] = _options.WebApiPort.ToString(CultureInfo.InvariantCulture),
            ["TOOLS_WEBAPI_HTTPS_PORT"] = _options.WebApiHttpsPort.ToString(CultureInfo.InvariantCulture),
            ["TOOLS_DOCKER_URLS"] = $"http://+:{_options.WebApiPort.ToString(CultureInfo.InvariantCulture)};https://+:{_options.WebApiHttpsPort.ToString(CultureInfo.InvariantCulture)}",
            ["TOOLS_HTTPS_CERT_FILE"] = _hostCertificateFile ?? Path.Combine(repositoryRoot, "build", "certs", HttpsCertificateStep.RootCertificateFileName),
            ["TOOLS_DOCKER_CERT_PASSWORD"] = _containerCertificatePassword ?? string.Empty,
            ["TOOLS_MIGRATION_CONTEXT"] = _options.MigrationContext.ToString(),
            ["TOOLS_MIGRATION_TIMEOUT"] = _options.MigrationTimeoutSeconds.ToString(CultureInfo.InvariantCulture),
            ["TOOLS_DOCKER_MIGRATOR_CONNECTION_STRING"] = _options.BuildDockerConnectionString(useHostGateway),
            ["TOOLS_DOCKER_APP_CONNECTION_STRING"] = _options.BuildApplicationConnectionString(forDocker: true, useHostGateway: useHostGateway)
        };
        secrets.ApplyToEnvironment(env!, uppercaseKeys: true);
        return env;
    }
}
