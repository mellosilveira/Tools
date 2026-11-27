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
    /// </summary>
    /// <param name="logger">Logger for startup polling diagnostics.</param>
    /// <param name="baseUrl">Local HTTP base URL of <c>MelloSilveiraTools.WebApi.Host</c>.</param>
    /// <param name="httpsBaseUrl">Optional external HTTPS base URL of <c>MelloSilveiraTools.WebApi.Host</c>.</param>
    /// <param name="hasExitedEarly">Optional callback returning <see langword="true"/> if the launched process terminated prematurely.</param>
    /// <param name="maxAttempts">Maximum polling attempts (5 seconds apart).</param>
    /// <param name="timeProvider">Clock provider used for retry delays.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> if the host became healthy; otherwise <see langword="false"/>.</returns>
    public static async Task<bool> WaitForHealthyAsync(
        ILogger logger,
        string baseUrl,
        string? httpsBaseUrl = null,
        Func<bool>? hasExitedEarly = null,
        int maxAttempts = 90,
        TimeProvider? timeProvider = null,
        CancellationToken cancellationToken = default)
    {
        TimeProvider clock = timeProvider ?? TimeProvider.System;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            if (hasExitedEarly?.Invoke() == true)
                return false;

            if (await IsHealthyAsync(baseUrl, cancellationToken).ConfigureAwait(false))
            {
                if (!string.IsNullOrWhiteSpace(httpsBaseUrl))
                    logger.LogInformation("Ready! External HTTPS: {HttpsUrl} | Local HTTP: {HttpUrl} | Swagger: {HttpUrl}/swagger", httpsBaseUrl, baseUrl, baseUrl);
                else
                    logger.LogInformation("Ready! Frontend Home: {BaseUrl} | Backend Swagger API: {BaseUrl}/swagger", baseUrl, baseUrl);

                return true;
            }

            logger.LogInformation("({CurrentAttempt}/{MaxAttempts}) Waiting {DelayTimeSpan} for WebApi.Host /api/health...", attempt, maxAttempts, RetryDelay);
            await Task.Delay(RetryDelay, clock, cancellationToken).ConfigureAwait(false);
        }

        logger.LogError("Timed out waiting for WebApi.Host /api/health endpoint at {BaseUrl}.", baseUrl);
        return false;
    }

    /// <summary>
    /// Asks the user whether to open the default browser on the frontend Home page (unless overridden by CLI flags).
    /// </summary>
    /// <param name="logger">Logger for browser launch diagnostics.</param>
    /// <param name="baseUrl">Frontend Home URL.</param>
    /// <param name="openBrowserOverride">Explicit CLI flag override, or <see langword="null"/> to prompt interactively.</param>
    /// <param name="nonInteractive">When <see langword="true"/>, skips prompting.</param>
    public static void PromptAndOpenBrowserIfRequested(ILogger logger, string baseUrl, bool? openBrowserOverride, bool nonInteractive)
    {
        bool shouldOpen;
        if (openBrowserOverride.HasValue)
        {
            shouldOpen = openBrowserOverride.Value;
        }
        else if (nonInteractive || Console.IsInputRedirected)
        {
            shouldOpen = false;
        }
        else
        {
            Console.Write($"  Do you want to open your browser at the frontend Home page ({baseUrl})? [Y/n]: ");
            string? answer = Console.ReadLine()?.Trim();
            shouldOpen = answer is not null
                && (answer.Length == 0
                    || answer.Equals("S", StringComparison.OrdinalIgnoreCase)
                    || answer.Equals("Y", StringComparison.OrdinalIgnoreCase));
        }

        if (!shouldOpen)
        {
            logger.LogInformation("Browser launch skipped. Open {BaseUrl} manually to access the frontend Home page.", baseUrl);
            return;
        }

        TryOpenBrowser(logger, baseUrl);
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
