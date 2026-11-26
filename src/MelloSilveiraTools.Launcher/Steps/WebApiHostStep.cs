using MelloSilveiraTools.Launcher.Infrastructure;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace MelloSilveiraTools.Launcher.Steps;

/// <summary>
/// Ensures <c>MelloSilveiraTools.WebApi.Host</c> (serving the API, Swagger, and Blazor WebAssembly UI)
/// is published with ReadyToRun AOT enabled, running, healthy, and optionally opened in the user's default browser.
/// </summary>
/// <param name="logger">Logger for execution diagnostics.</param>
/// <param name="currentStepNumber">Current 1-based step index.</param>
/// <param name="numberOfSteps">Total number of launcher steps.</param>
/// <param name="repositoryRoot">Root directory of the repository.</param>
/// <param name="connectionString">PostgreSQL connection string passed to the host process.</param>
/// <param name="secrets">Resolved runtime secrets passed to the host process.</param>
/// <param name="baseUrl">Base URL where the Web API host listens.</param>
/// <param name="openBrowser">Whether to open the default browser automatically when ready.</param>
public sealed class WebApiHostStep(
    ILogger<WebApiHostStep> logger,
    int currentStepNumber,
    int numberOfSteps,
    string repositoryRoot,
    string connectionString,
    LauncherRuntimeSecrets secrets,
    string baseUrl = "http://localhost:24263",
    bool openBrowser = true)
{
    private Process? _hostProcess;

    /// <summary>
    /// Checks if the Web API is already responding on <c>/health</c>; otherwise publishes with ReadyToRun AOT and launches it.
    /// </summary>
    public async Task<bool> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("[{CurrentStepNumber}/{NumberOfSteps}] Checking Web API & Frontend Host (ReadyToRun AOT)...", currentStepNumber, numberOfSteps);

        if (await LauncherHostProbe.IsHealthyAsync(baseUrl, cancellationToken).ConfigureAwait(false))
        {
            logger.LogInformation("Web API & Blazor WebUI are already running at {BaseUrl}.", baseUrl);
            return !openBrowser || LauncherHostProbe.TryOpenBrowser(logger, baseUrl);
        }

        string hostProject = Path.Combine(repositoryRoot, "src", "MelloSilveiraTools.WebApi.Host", "MelloSilveiraTools.WebApi.Host.csproj");
        if (!File.Exists(hostProject))
        {
            logger.LogError("Could not locate host project at '{HostProject}'.", hostProject);
            return false;
        }

        string publishDir = Path.Combine(repositoryRoot, "build", "publish", "WebApiHost");
        logger.LogInformation("Publishing MelloSilveiraTools.WebApi.Host with ReadyToRun AOT (PublishReadyToRun=true)...");
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

        logger.LogInformation("Launching MelloSilveiraTools.WebApi.Host ({BaseUrl})...", baseUrl);
        ProcessStartInfo startInfo = new()
        {
            FileName = "dotnet",
            Arguments = $"\"{hostDll}\" --urls \"{baseUrl}\"",
            WorkingDirectory = publishDir,
            UseShellExecute = false
        };
        startInfo.Environment["Database__ConnectionString"] = connectionString;
        startInfo.Environment["ASPNETCORE_URLS"] = baseUrl;
        startInfo.Environment["Urls"] = baseUrl;
        secrets.ApplyToEnvironment(startInfo.Environment);

        _hostProcess = Process.Start(startInfo);
        if (_hostProcess is null)
        {
            logger.LogError("Failed to start MelloSilveiraTools.WebApi.Host process.");
            return false;
        }

        bool ready = await LauncherHostProbe.WaitForHealthyAsync(
            logger,
            baseUrl,
            openBrowser,
            hasExitedEarly: CheckHostExitedEarly,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!ready)
            await TerminateHostProcessAsync().ConfigureAwait(false);

        return ready;
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
