using MelloSilveiraTools.Launcher.Infrastructure;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.Launcher.Steps;

/// <summary>
/// Ensures an HTTPS PKCS#12 (<c>.pfx</c>) certificate file is placed alongside the application binaries:
/// accepts a user-provided custom certificate path (via CLI or interactive prompt) or generates a local
/// development certificate using the native <c>dotnet dev-certs https</c> CLI tool only when no custom certificate is supplied.
/// </summary>
/// <param name="logger">Logger for certificate provisioning diagnostics.</param>
/// <param name="options">Resolved launcher infrastructure options.</param>
public sealed class HttpsCertificateStep(
    ILogger<HttpsCertificateStep> logger,
    LauncherInfrastructureOptions options)
{
    /// <summary>
    /// Standard file name expected by <c>MelloSilveiraTools.WebApi.Host</c> in its application root directory.
    /// </summary>
    public const string RootCertificateFileName = "https-cert.pfx";

    private const string LocalDevCertPassword = "mellosilveiratools";

    private string? _resolvedCustomPath = options.HttpsCertificatePath;
    private string? _resolvedCustomPassword = options.HttpsCertificatePassword;
    private bool _prompted;

    /// <summary>
    /// Prompts the user (once, when interactive and not passed via CLI) whether they want to use a custom <c>.pfx</c> certificate.
    /// </summary>
    public void ResolveCertificateSelection()
    {
        if (_prompted)
            return;

        _prompted = true;
        if (!string.IsNullOrWhiteSpace(_resolvedCustomPath) || options.NonInteractive || Console.IsInputRedirected)
            return;

        Console.Write("  Do you want to use your own custom HTTPS certificate (.pfx)? [y/N]: ");
        string? useCustomAnswer = Console.ReadLine()?.Trim();
        bool wantsCustom = useCustomAnswer is not null
            && (useCustomAnswer.Equals("Y", StringComparison.OrdinalIgnoreCase)
                || useCustomAnswer.Equals("S", StringComparison.OrdinalIgnoreCase));

        if (!wantsCustom)
            return;

        while (true)
        {
            Console.Write("  Enter path to your custom HTTPS certificate (.pfx) (or press Enter to create a local certificate instead): ");
            string? rawPath = Console.ReadLine()?.Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(rawPath))
                return;

            string fullPath = Path.GetFullPath(rawPath);
            if (!File.Exists(fullPath))
            {
                Console.WriteLine($"  File not found: '{fullPath}'. Try again or press Enter to generate a local certificate.");
                continue;
            }

            _resolvedCustomPath = fullPath;
            if (string.IsNullOrEmpty(_resolvedCustomPassword))
            {
                Console.Write("  Enter certificate password (press Enter if none): ");
                _resolvedCustomPassword = Console.ReadLine();
            }

            return;
        }
    }

    /// <summary>
    /// Copies the custom certificate (or generates a local certificate via <c>dotnet dev-certs https</c> only when
    /// no custom certificate was supplied) into <paramref name="targetDirectory"/> as <c>https-cert.pfx</c>.
    /// </summary>
    /// <param name="targetDirectory">Application root or mounted certificate directory.</param>
    /// <param name="allowDockerSdkFallback">When <see langword="true"/>, falls back to running <c>dotnet dev-certs</c> inside <c>mcr.microsoft.com/dotnet/sdk:10.0</c> if <c>dotnet</c> is not installed on the host.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<(bool Success, string? CertificatePath, string? CertificatePassword)> PrepareCertificateInDirectoryAsync(
        string targetDirectory,
        bool allowDockerSdkFallback,
        CancellationToken cancellationToken = default)
    {
        ResolveCertificateSelection();
        Directory.CreateDirectory(targetDirectory);
        string destinationPath = Path.Combine(targetDirectory, RootCertificateFileName);

        if (!string.IsNullOrWhiteSpace(_resolvedCustomPath))
        {
            string sourcePath = Path.GetFullPath(_resolvedCustomPath);
            if (!File.Exists(sourcePath))
            {
                logger.LogError("Custom HTTPS certificate file not found at '{SourcePath}'.", sourcePath);
                return (false, null, null);
            }

            File.Copy(sourcePath, destinationPath, overwrite: true);
            logger.LogInformation("Custom HTTPS certificate copied to '{DestinationPath}'.", destinationPath);
            return (true, destinationPath, _resolvedCustomPassword);
        }

        logger.LogInformation("No custom HTTPS certificate supplied. Generating local HTTPS certificate via 'dotnet dev-certs https'...");
        (int dotnetCheckExit, _, _) = await ProcessRunner.RunCommandAsync("dotnet", "--version", cancellationToken: cancellationToken).ConfigureAwait(false);
        if (dotnetCheckExit == 0)
        {
            (int exportExit, _, string exportErr) = await ProcessRunner.RunCommandAsync(
                "dotnet",
                $"dev-certs https -ep \"{destinationPath}\" -p \"{LocalDevCertPassword}\"",
                targetDirectory,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (exportExit == 0 && File.Exists(destinationPath))
            {
                (int checkTrustExit, _, _) = await ProcessRunner.RunCommandAsync("dotnet", "dev-certs https --check --trust", cancellationToken: cancellationToken).ConfigureAwait(false);
                if (checkTrustExit != 0 && !options.NonInteractive && !Console.IsInputRedirected)
                {
                    logger.LogInformation("Trusting local ASP.NET Core HTTPS development certificate...");
                    await ProcessRunner.RunCommandAsync("dotnet", "dev-certs https --trust", cancellationToken: cancellationToken).ConfigureAwait(false);
                }

                logger.LogInformation("Local HTTPS certificate placed in '{DestinationPath}'.", destinationPath);
                return (true, destinationPath, LocalDevCertPassword);
            }

            if (!allowDockerSdkFallback)
            {
                logger.LogError("Failed to generate local HTTPS certificate via 'dotnet dev-certs https': {Error}", exportErr);
                return (false, null, null);
            }
        }

        if (!allowDockerSdkFallback)
        {
            logger.LogError("The .NET SDK is required to generate a local HTTPS certificate when no custom .pfx is provided.");
            return (false, null, null);
        }

        logger.LogInformation("Generating local HTTPS certificate using Docker SDK container (no .NET SDK required on host)...");
        string fullTargetDir = Path.GetFullPath(targetDirectory);
        (int dockerCertExit, _, string dockerCertErr) = await ProcessRunner.RunCommandAsync(
            "docker",
            $"run --rm -v \"{fullTargetDir}:/certs\" mcr.microsoft.com/dotnet/sdk:10.0 dotnet dev-certs https -ep /certs/{RootCertificateFileName} -p \"{LocalDevCertPassword}\"",
            targetDirectory,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (dockerCertExit != 0 || !File.Exists(destinationPath))
        {
            logger.LogError("Failed to generate local HTTPS certificate via Docker SDK container: {Error}", dockerCertErr);
            return (false, null, null);
        }

        logger.LogInformation("Local HTTPS certificate placed in '{DestinationPath}'.", destinationPath);
        return (true, destinationPath, LocalDevCertPassword);
    }
}
