using System.Diagnostics;

namespace MelloSilveiraTools.Launcher.Infrastructure;

/// <summary>
/// Cross-platform helper for executing external CLI commands and probing TCP/HTTP readiness.
/// </summary>
public static class ProcessRunner
{
    /// <summary>
    /// Executes a command synchronously with captured stdout/stderr and returns its exit code and output.
    /// </summary>
    public static async Task<(int ExitCode, string StdOut, string StdErr)> RunCommandAsync(
        string fileName,
        string arguments,
        string? workingDirectory = null,
        IReadOnlyDictionary<string, string>? environmentVariables = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            if (environmentVariables is not null)
            {
                foreach (KeyValuePair<string, string> pair in environmentVariables)
                    startInfo.Environment[pair.Key] = pair.Value;
            }

            using Process? process = Process.Start(startInfo);
            if (process is null)
                return (-1, string.Empty, $"Failed to start '{fileName}'.");

            Task<string> outTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            Task<string> errTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            return (process.ExitCode, await outTask.ConfigureAwait(false), await errTask.ConfigureAwait(false));
        }
        catch (Exception ex)
        {
            return (-1, string.Empty, ex.Message);
        }
    }

    /// <summary>
    /// Executes an interactive command inheriting the user's terminal for package managers or installers.
    /// </summary>
    public static async Task<int> RunInteractiveAsync(string fileName, string arguments, CancellationToken cancellationToken = default)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false
        };

        using Process? process = Process.Start(startInfo);
        if (process is null)
            return -1;

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        return process.ExitCode;
    }
}
