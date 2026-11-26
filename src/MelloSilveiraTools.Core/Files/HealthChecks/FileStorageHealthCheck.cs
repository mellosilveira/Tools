using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Providers;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.Core.Files.HealthChecks;

/// <summary>
/// Native ASP.NET Core <see cref="IHealthCheck"/> probing every registered <see cref="IFileStorageService"/>
/// across static (keyed and unkeyed) and dynamic plugin containers via <see cref="ServiceLocator"/>.
/// Fails closed (<see cref="HealthStatus.Unhealthy"/>) if any registered storage provider fails its probe.
/// </summary>
/// <param name="logger">Logger recording storage health-check failures.</param>
/// <param name="serviceLocator">Service locator resolving all unique registered <see cref="IFileStorageService"/> instances.</param>
public sealed class FileStorageHealthCheck(
    ILogger<FileStorageHealthCheck> logger,
    ServiceLocator serviceLocator) : IHealthCheck
{
    /// <summary>
    /// Default registration name used when adding <see cref="FileStorageHealthCheck"/> to ASP.NET Core health checks.
    /// </summary>
    public const string DefaultName = "file-storage";

    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        List<IFileStorageService> storageServices = [.. serviceLocator.GetServices<IFileStorageService>()];
        if (storageServices.Count == 0)
            return HealthCheckResult.Unhealthy("No IFileStorageService instances are registered.");

        Dictionary<string, object> providerStatuses = new(storageServices.Count, StringComparer.OrdinalIgnoreCase);
        List<string> failedProviders = [];
        List<Exception> failures = [];

        foreach (IFileStorageService storageService in storageServices)
        {
            string providerKey = ResolveUniqueProviderKey(providerStatuses, storageService.Provider.ToString());
            try
            {
                if (await storageService.IsHealthAsync(cancellationToken).ConfigureAwait(false))
                {
                    providerStatuses[providerKey] = HealthStatus.Healthy.ToString();
                }
                else
                {
                    logger.LogError("Health check '{HealthCheckName}' reported unhealthy for file storage provider '{StorageProvider}'.", context.Registration.Name, providerKey);
                    providerStatuses[providerKey] = HealthStatus.Unhealthy.ToString();
                    failedProviders.Add(providerKey);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Health check '{HealthCheckName}' failed for file storage provider '{StorageProvider}'.", context.Registration.Name, providerKey);
                providerStatuses[providerKey] = HealthStatus.Unhealthy.ToString();
                failedProviders.Add(providerKey);
                failures.Add(ex);
            }
        }

        if (failedProviders.Count == 0)
            return HealthCheckResult.Healthy($"All {storageServices.Count} file storage provider(s) are healthy.", providerStatuses);

        string failedList = string.Join(", ", failedProviders);
        Exception? aggregatedException = failures.Count switch
        {
            0 => null,
            1 => failures[0],
            _ => new AggregateException($"Multiple file storage providers failed health checks ({failedList}).", failures)
        };

        return HealthCheckResult.Unhealthy($"File storage health check failed for provider(s): {failedList}.", aggregatedException, providerStatuses);
    }

    private static string ResolveUniqueProviderKey(Dictionary<string, object> statuses, string baseKey)
    {
        if (!statuses.ContainsKey(baseKey))
            return baseKey;

        int suffix = 2;
        while (statuses.ContainsKey($"{baseKey}#{suffix}"))
            suffix++;

        return $"{baseKey}#{suffix}";
    }
}
