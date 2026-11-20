using System.Diagnostics;

namespace MelloSilveiraTools.Launcher.Steps;

/// <summary>
/// Ensures <c>MelloSilveiraTools.WebApi.Host</c> (serving the API, Swagger, and Blazor WebAssembly UI)
/// is running, healthy, and opened in the user's default browser.
/// </summary>
public sealed class WebApiHostStep(string repositoryRoot, string connectionString, string baseUrl = "http://localhost:5080")
{
    private Process? _hostProcess;

    /// <summary>
    /// Checks if the Web API is already responding on <c>/health</c>; otherwise launches it and opens the browser.
    /// </summary>
    public async Task<bool> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("[3/3] Checking Web API & Frontend Host...");

        if (await IsHealthyAsync(cancellationToken).ConfigureAwait(false))
        {
            Console.WriteLine($"  -> Web API & Blazor WebUI are already running at {baseUrl}.");
            OpenBrowser(baseUrl);
            return true;
        }

        string hostProject = Path.Combine(repositoryRoot, "src", "MelloSilveiraTools.WebApi.Host", "MelloSilveiraTools.WebApi.Host.csproj");
        if (!File.Exists(hostProject))
        {
            Console.WriteLine($"  [ERROR] Could not locate host project at '{hostProject}'.");
            return false;
        }

        Console.WriteLine($"  -> Launching MelloSilveiraTools.WebApi.Host ({baseUrl})...");
        ProcessStartInfo startInfo = new()
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{hostProject}\" --no-launch-profile",
            WorkingDirectory = repositoryRoot,
            UseShellExecute = false
        };
        startInfo.Environment["Database__ConnectionString"] = connectionString;

        _hostProcess = Process.Start(startInfo);

        if (_hostProcess is null)
        {
            Console.WriteLine("  [ERROR] Failed to start MelloSilveiraTools.WebApi.Host process.");
            return false;
        }

        for (int attempt = 0; attempt < 90; attempt++)
        {
            if (_hostProcess.HasExited)
            {
                Console.WriteLine($"  [ERROR] WebApi.Host exited unexpectedly with code {_hostProcess.ExitCode}.");
                _hostProcess.Dispose();
                _hostProcess = null;
                return false;
            }

            if (await IsHealthyAsync(cancellationToken).ConfigureAwait(false))
            {
                Console.WriteLine($"  -> Ready! Frontend: {baseUrl} | Swagger: {baseUrl}/swagger");
                OpenBrowser(baseUrl);
                return true;
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
        }

        Console.WriteLine("  [ERROR] Timed out waiting for WebApi.Host /health endpoint.");
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

        Console.WriteLine("Press Ctrl+C to stop MelloSilveiraTools...");
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
            using HttpResponseMessage response = await client.GetAsync($"{baseUrl.TrimEnd('/')}/health", cancellationToken).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static void OpenBrowser(string url)
    {
        try
        {
            if (OperatingSystem.IsWindows())
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            else if (OperatingSystem.IsMacOS())
                Process.Start("open", url);
            else if (OperatingSystem.IsLinux())
                Process.Start("xdg-open", url);
        }
        catch
        {
        }
    }
}
