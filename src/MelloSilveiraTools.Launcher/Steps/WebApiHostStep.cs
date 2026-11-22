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
/// <param name="baseUrl">Base URL where the Web API host listens.</param>
/// <param name="openBrowser">Whether to open the default browser automatically when ready.</param>
public sealed class WebApiHostStep(
    ILogger<WebApiHostStep> logger,
    int currentStepNumber,
    int numberOfSteps,
    string repositoryRoot,
    string connectionString,
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

        if (await IsHealthyAsync(cancellationToken).ConfigureAwait(false))
        {
            logger.LogInformation("Web API & Blazor WebUI are already running at {BaseUrl}.", baseUrl);
            if (openBrowser)
                return TryOpenBrowser(logger, baseUrl);
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
            cancellationToken).ConfigureAwait(false);

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
            Arguments = $"\"{hostDll}\"",
            WorkingDirectory = publishDir,
            UseShellExecute = false
        };
        startInfo.Environment["Database__ConnectionString"] = connectionString;
        startInfo.Environment["ASPNETCORE_URLS"] = baseUrl;

        _hostProcess = Process.Start(startInfo);

        if (_hostProcess is null)
        {
            logger.LogError("Failed to start MelloSilveiraTools.WebApi.Host process.");
            return false;
        }

        int maxAttempts = 90;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            if (_hostProcess.HasExited)
            {
                logger.LogError("WebApi.Host exited unexpectedly with code {ExitCode}.", _hostProcess.ExitCode);
                _hostProcess.Dispose();
                _hostProcess = null;
                return false;
            }

            if (await IsHealthyAsync(cancellationToken).ConfigureAwait(false))
            {
                logger.LogInformation("Ready! Frontend: {BaseUrl} | Swagger: {BaseUrl}/swagger", baseUrl, baseUrl);
                if (openBrowser)
                    return TryOpenBrowser(logger, baseUrl);
            }

            TimeSpan delayTimeSpan = TimeSpan.FromSeconds(5);
            logger.LogInformation("({CurrentAttempt}/{MaxAttempts} Waiting {DelayTimeSpan} to check if WebApi.Host is health.", attempt, maxAttempts, delayTimeSpan);
            await Task.Delay(delayTimeSpan, cancellationToken).ConfigureAwait(false);
        }

        logger.LogError("Timed out waiting for WebApi.Host /health endpoint.");
        await TerminateHostProcessAsync().ConfigureAwait(false);
        return false;
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

    private async Task<bool> IsHealthyAsync(CancellationToken cancellationToken)
    {
        try
        {
            using HttpClient client = new() { Timeout = TimeSpan.FromSeconds(2) };
            using HttpResponseMessage response = await client.GetAsync($"{baseUrl}/health", cancellationToken).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public static bool TryOpenBrowser(ILogger logger, string url)
    {
        try
        {
            if (OperatingSystem.IsWindows())
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            else if (OperatingSystem.IsMacOS())
                Process.Start("open", url);
            else if (OperatingSystem.IsLinux())
                Process.Start("xdg-open", url);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to open browser for web application.");
            return false;
        }
    }
}
