using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;

namespace MelloSilveiraTools.Launcher.Infrastructure;

/// <summary>
/// Probes TCP port availability using native <see cref="IPGlobalProperties"/> and interactively resolves port conflicts
/// by letting the user stop the conflicting service and retry or choose an alternate port without aborting the launcher.
/// </summary>
/// <param name="logger">Logger for port conflict diagnostics.</param>
/// <param name="nonInteractive">When <see langword="true"/>, skips interactive prompts and returns failure on conflict.</param>
public sealed class PortConflictResolver(ILogger<PortConflictResolver> logger, bool nonInteractive)
{
    /// <summary>
    /// Ensures <paramref name="currentPort"/> is available (or accepted by <paramref name="canReusePortAsync"/>),
    /// otherwise prompts the user to either stop the service on that port and retry or enter a different port.
    /// </summary>
    /// <param name="serviceName">Human-readable service name (e.g., <c>PostgreSQL</c> or <c>WebApi.Host</c>).</param>
    /// <param name="host">Target host name.</param>
    /// <param name="currentPort">Initially configured TCP port.</param>
    /// <param name="canReusePortAsync">Optional callback returning <see langword="true"/> if an already-listening service on that port can be reused.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing resolution success, final port, and whether an existing service on the port is being reused.</returns>
    public async Task<(bool Success, int Port, bool ReusedExisting)> EnsurePortAvailableOrResolveAsync(
        string serviceName,
        string host,
        int currentPort,
        Func<int, Task<bool>>? canReusePortAsync = null,
        CancellationToken cancellationToken = default)
    {
        int port = currentPort;
        while (true)
        {
            if (canReusePortAsync is not null && await canReusePortAsync(port).ConfigureAwait(false))
                return (true, port, true);

            bool inUse = IsLocalPortInUse(host, port);
            if (!inUse)
                return (true, port, false);

            logger.LogWarning("Port {Port} required by {ServiceName} is already in use on {Host}.", port, serviceName, host);

            (bool resolved, int nextPort) = PromptPortResolution(serviceName, port);
            if (!resolved)
                return (false, port, false);

            port = nextPort;
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    /// <summary>
    /// Prompts the user after a runtime port-allocation failure (e.g. Docker <c>port is already allocated</c>)
    /// to either stop the conflicting service and retry on the same port or switch to a new port.
    /// </summary>
    /// <param name="serviceName">Human-readable service name.</param>
    /// <param name="currentPort">Port that failed to bind.</param>
    /// <returns>A tuple indicating whether to retry and the port to use.</returns>
    public (bool ShouldRetry, int Port) PromptPortResolution(string serviceName, int currentPort)
    {
        if (nonInteractive || Console.IsInputRedirected)
        {
            logger.LogError(
                "Port {Port} is already in use for {ServiceName}. Stop the service using port {Port} or specify a different port via CLI arguments.",
                currentPort,
                serviceName,
                currentPort);
            return (false, currentPort);
        }

        Console.WriteLine();
        Console.WriteLine($"  [Port Conflict] Port {currentPort} for {serviceName} is already in use.");
        Console.WriteLine("  You can stop the service currently using this port and press [R] to retry,");
        Console.WriteLine("  or enter a different port number (1-65535), or [Q] to cancel.");

        while (true)
        {
            Console.Write($"  Action ([R]etry on {currentPort} / new port / [Q]uit) [R]: ");
            string? input = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input) || input.Equals("R", StringComparison.OrdinalIgnoreCase))
                return (true, currentPort);

            if (input.Equals("Q", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogError("Launcher cancelled by user due to port {Port} conflict on {ServiceName}.", currentPort, serviceName);
                return (false, currentPort);
            }

            if (int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedPort)
                && parsedPort is > 0 and <= 65535)
            {
                logger.LogInformation("Switching {ServiceName} port from {OldPort} to {NewPort}.", serviceName, currentPort, parsedPort);
                return (true, parsedPort);
            }

            Console.WriteLine("  Invalid input. Press [R] to retry, enter a port between 1 and 65535, or [Q] to cancel.");
        }
    }

    /// <summary>
    /// Checks whether a local TCP port currently has an active listener using <see cref="IPGlobalProperties.GetActiveTcpListeners"/>.
    /// </summary>
    public static bool IsLocalPortInUse(string host, int port)
    {
        if (!string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(host, "127.0.0.1", StringComparison.Ordinal)
            && !string.Equals(host, "::1", StringComparison.Ordinal))
        {
            return false;
        }

        IPEndPoint[] activeListeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
        foreach (IPEndPoint endpoint in activeListeners)
        {
            if (endpoint.Port == port)
                return true;
        }

        return false;
    }
}
