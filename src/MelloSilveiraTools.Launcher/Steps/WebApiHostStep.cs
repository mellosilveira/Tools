using MelloSilveiraTools.Launcher.Infrastructure;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Globalization;

namespace MelloSilveiraTools.Launcher.Steps;

/// <summary>
/// Ensures <c>MelloSilveiraTools.WebApi.Host</c> (serving both the backend REST/NDJSON API and the Blazor WebAssembly frontend)
/// is published with ReadyToRun AOT enabled, running on both local HTTP and external HTTPS ports, healthy,
/// and optionally opened in the user's default browser.
/// </summary>
/// <param name="logger">Logger for execution diagnostics.</param>
/// <param name="currentStepNumber">Current 1-based step index.</param>
/// <param name="numberOfSteps">Total number of launcher steps.</param>
/// <param name="repositoryRoot">Root directory of the repository.</param>
/// <param name="connectionString">PostgreSQL connection string passed to the host process.</param>
/// <param name="secrets">Resolved runtime secrets passed to the host process.</param>
/// <param name="portConflictResolver">Interactive port conflict resolver when the configured HTTP or HTTPS port is already occupied.</param>
/// <param name="httpsCertificateStep">HTTPS certificate provisioning step placing <c>https-cert.pfx</c> alongside the host executable/DLLs.</param>
/// <param name="webApiPort">Initial local HTTP TCP port where the Web API and frontend host listens (default: <c>26324</c>).</param>
/// <param name="webApiHttpsPort">Initial external HTTPS TCP port where the Web API and frontend host listens (default: <c>24263</c>).</param>
/// <param name="openBrowserOverride">Explicit CLI flag override, or <see langword="null"/> to prompt interactively.</param>
/// <param name="nonInteractive">When <see langword="true"/>, skips prompting.</param>
/// <param name="timeProvider">Clock provider used for health polling delays.</param>
public sealed class WebApiHostStep(
    ILogger<WebApiHostStep> logger,
    int currentStepNumber,
    int numberOfSteps,
    string repositoryRoot,
    string connectionString,
    LauncherRuntimeSecrets secrets,
    PortConflictResolver portConflictResolver,
    HttpsCertificateStep httpsCertificateStep,
    int webApiPort = 26324,
    int webApiHttpsPort = 24263,
    bool? openBrowserOverride = null,
    bool nonInteractive = false,
    TimeProvider? timeProvider = null)
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private Process? _hostProcess;

    /// <summary>
    /// Checks if the backend and frontend host is already responding on <c>/api/health</c>; otherwise publishes with ReadyToRun AOT and launches it.
    /// </summary>
    public async Task<bool> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("[{CurrentStepNumber}/{NumberOfSteps}] Starting Backend API (WebApi.Host) & Frontend UI (Blazor WebUi)...", currentStepNumber, numberOfSteps);

        (bool httpAvailable, int activeHttpPort, bool alreadyHealthy) = await portConflictResolver.EnsurePortAvailableOrResolveAsync(
            "WebApi.Host / WebUi (HTTP)",
            "localhost",
            webApiPort,
            port => LauncherHostProbe.IsHealthyAsync($"http://localhost:{port}", cancellationToken),
            cancellationToken).ConfigureAwait(false);
        if (!httpAvailable)
            return false;

        string activeHttpUrl = $"http://localhost:{activeHttpPort}";
        if (alreadyHealthy)
        {
            logger.LogInformation("Backend API & Frontend Home are already running at {BaseUrl}.", activeHttpUrl);
            LauncherHostProbe.PromptAndOpenBrowserIfRequested(logger, activeHttpUrl, openBrowserOverride, nonInteractive);
            return true;
        }

        (bool httpsAvailable, int activeHttpsPort, _) = await portConflictResolver.EnsurePortAvailableOrResolveAsync(
            "WebApi.Host / WebUi (HTTPS)",
            "localhost",
            webApiHttpsPort,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!httpsAvailable)
            return false;

        string hostProject = Path.Combine(repositoryRoot, "src", "MelloSilveiraTools.WebApi.Host", "MelloSilveiraTools.WebApi.Host.csproj");
        if (!File.Exists(hostProject))
        {
            logger.LogError("Could not locate host project at '{HostProject}'.", hostProject);
            return false;
        }

        string publishDir = Path.Combine(repositoryRoot, "build", "publish", "WebApiHost");
        logger.LogInformation("Publishing MelloSilveiraTools.WebApi.Host + WebUi with ReadyToRun AOT...");
        (int publishExit, string publishOut, string publishErr) = await ProcessRunner.RunCommandAsync(
            "dotnet",
            $"publish \"{hostProject}\" -c Release -p:EnableReadyToRun=true -p:GeneratePackageOnBuild=false -o \"{publishDir}\"",
            repositoryRoot,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (publishExit != 0)
        {
            logger.LogError("ReadyToRun publish failed: {Error} {Output}", publishErr, publishOut);
            return false;
        }

        string hostDll = Path.Combine(publishDir, "MelloSilveiraTools.WebApi.Host.dll");
        if (!File.Exists(hostDll))
        {
            logger.LogError("Published WebApi.Host assembly not found at '{HostDll}'.", hostDll);
            return false;
        }

        (bool certOk, string? _, string? certPassword) = await httpsCertificateStep
            .PrepareCertificateInDirectoryAsync(publishDir, allowDockerSdkFallback: false, cancellationToken)
            .ConfigureAwait(false);
        if (!certOk)
            return false;

        while (true)
        {
            activeHttpUrl = $"http://localhost:{activeHttpPort}";
            string activeHttpsUrl = $"https://localhost:{activeHttpsPort}";
            string combinedUrls = $"{activeHttpUrl};{activeHttpsUrl}";
            logger.LogInformation("Launching Backend & Frontend host (HTTP: {HttpUrl} | HTTPS: {HttpsUrl})...", activeHttpUrl, activeHttpsUrl);

            ProcessStartInfo startInfo = new()
            {
                FileName = "dotnet",
                Arguments = $"\"{hostDll}\" --urls \"{combinedUrls}\"",
                WorkingDirectory = publishDir,
                UseShellExecute = false
            };
            startInfo.Environment["Database__ConnectionString"] = connectionString;
            startInfo.Environment["ASPNETCORE_URLS"] = combinedUrls;
            startInfo.Environment["Urls"] = combinedUrls;
            startInfo.Environment["Kestrel__Certificates__Default__Password"] = certPassword ?? string.Empty;
            secrets.ApplyToEnvironment(startInfo.Environment);

            _hostProcess = Process.Start(startInfo);
            if (_hostProcess is null)
            {
                logger.LogError("Failed to start MelloSilveiraTools.WebApi.Host process.");
                return false;
            }

            bool ready = await LauncherHostProbe.WaitForHealthyAsync(
                logger,
                activeHttpUrl,
                httpsBaseUrl: activeHttpsUrl,
                hasExitedEarly: CheckHostExitedEarly,
                timeProvider: _timeProvider,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (ready)
            {
                LauncherHostProbe.PromptAndOpenBrowserIfRequested(logger, activeHttpUrl, openBrowserOverride, nonInteractive);
                return true;
            }

            await TerminateHostProcessAsync().ConfigureAwait(false);

            bool httpInUse = PortConflictResolver.IsLocalPortInUse("localhost", activeHttpPort);
            bool httpsInUse = PortConflictResolver.IsLocalPortInUse("localhost", activeHttpsPort);
            if (!httpInUse && !httpsInUse)
                return false;

            if (httpsInUse && !httpInUse)
            {
                (bool shouldRetryHttps, int nextHttpsPort) = portConflictResolver.PromptPortResolution("WebApi.Host / WebUi (HTTPS)", activeHttpsPort);
                if (!shouldRetryHttps)
                    return false;

                activeHttpsPort = nextHttpsPort;
            }
            else
            {
                (bool shouldRetryHttp, int nextHttpPort) = portConflictResolver.PromptPortResolution("WebApi.Host / WebUi (HTTP)", activeHttpPort);
                if (!shouldRetryHttp)
                    return false;

                activeHttpPort = nextHttpPort;
            }
        }
    }

    /// <summary>
    /// Waits for the launched host process until cancellation is requested, ensuring graceful termination.
    /// </summary>
    public async Task WaitForShutdownAsync(CancellationToken cancellationToken)
    {
        if (_hostProcess is null)
            return;

        logger.LogInformation("Press Ctrl+C to stop MelloSilveiraTools...");
        try
        {
            await _hostProcess.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            await TerminateHostProcessAsync().ConfigureAwait(false);
        }
    }

    private bool CheckHostExitedEarly()
    {
        if (_hostProcess is null || !_hostProcess.HasExited)
            return false;

        logger.LogError("WebApi.Host exited unexpectedly with code {ExitCode}.", _hostProcess.ExitCode);
        return true;
    }

    private async Task TerminateHostProcessAsync()
    {
        if (_hostProcess is null)
            return;

        if (!_hostProcess.HasExited)
        {
            _hostProcess.Kill(entireProcessTree: true);
            await _hostProcess.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
        }

        _hostProcess.Dispose();
        _hostProcess = null;
    }
}
