using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace MelloSilveiraTools.Launcher.Infrastructure;

/// <summary>
/// Shared health-check polling and browser-launch helper used by both Local and Docker launcher steps.
/// </summary>
public static class LauncherHostProbe
{
    private static readonly TimeSpan HealthRequestTimeout = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Probes <c>{baseUrl}/api/health</c> once and returns <see langword="true"/> when a 2xx response is received.
    /// </summary>
    /// <param name="baseUrl">Base URL of <c>MelloSilveiraTools.WebApi.Host</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task<bool> IsHealthyAsync(string baseUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            using HttpClient client = new() { Timeout = HealthRequestTimeout };
            using HttpResponseMessage response = await client.GetAsync($"{baseUrl}/api/health", cancellationToken).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Polls <c>{baseUrl}/api/health</c> until the host becomes healthy, exits prematurely, or reaches <paramref name="maxAttempts"/>.
    /// Optionally opens the default browser once healthy.
    /// </summary>
    /// <param name="logger">Logger for startup polling diagnostics.</param>
    /// <param name="baseUrl">Base URL of <c>MelloSilveiraTools.WebApi.Host</c>.</param>
    /// <param name="openBrowser">When <see langword="true"/>, launches the default browser upon readiness.</param>
    /// <param name="hasExitedEarly">Optional callback returning <see langword="true"/> if the launched process terminated prematurely.</param>
    /// <param name="maxAttempts">Maximum polling attempts (5 seconds apart).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> if the host became healthy (and browser launch succeeded when requested); otherwise <see langword="false"/>.</returns>
    public static async Task<bool> WaitForHealthyAsync(
        ILogger logger,
        string baseUrl,
        bool openBrowser,
        Func<bool>? hasExitedEarly = null,
        int maxAttempts = 90,
        CancellationToken cancellationToken = default)
    {
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            if (hasExitedEarly?.Invoke() == true)
                return false;

            if (await IsHealthyAsync(baseUrl, cancellationToken).ConfigureAwait(false))
            {
                logger.LogInformation("Ready! Frontend: {BaseUrl} | Swagger: {BaseUrl}/swagger", baseUrl, baseUrl);
                return !openBrowser || TryOpenBrowser(logger, baseUrl);
            }

            logger.LogInformation("({CurrentAttempt}/{MaxAttempts}) Waiting {DelayTimeSpan} for WebApi.Host /api/health...", attempt, maxAttempts, RetryDelay);
            await Task.Delay(RetryDelay, cancellationToken).ConfigureAwait(false);
        }

        logger.LogError("Timed out waiting for WebApi.Host /api/health endpoint at {BaseUrl}.", baseUrl);
        return false;
    }

    /// <summary>
    /// Attempts to open <paramref name="url"/> in the operating system's default browser.
    /// </summary>
    /// <param name="logger">Logger for browser launch diagnostics.</param>
    /// <param name="url">Target URL to open.</param>
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
