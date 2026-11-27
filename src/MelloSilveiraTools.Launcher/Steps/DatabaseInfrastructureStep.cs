using MelloSilveiraTools.Launcher.Infrastructure;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.Launcher.Steps;

/// <summary>
/// Ensures local PostgreSQL is running and accepting protocol queries before migrations execute.
/// </summary>
/// <param name="logger">Logger for execution diagnostics.</param>
/// <param name="currentStepNumber">Current 1-based step index.</param>
/// <param name="numberOfSteps">Total number of launcher steps.</param>
/// <param name="dependencyInstaller">Dependency installer providing PostgreSQL readiness probing.</param>
public sealed class DatabaseInfrastructureStep(
    ILogger<DatabaseInfrastructureStep> logger,
    int currentStepNumber,
    int numberOfSteps,
    DependencyInstaller dependencyInstaller)
{
    /// <summary>
    /// Validates that local PostgreSQL is running and accepting queries.
    /// </summary>
    public async Task<bool> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("[{CurrentStepNumber}/{NumberOfSteps}] Verifying local PostgreSQL database readiness...", currentStepNumber, numberOfSteps);
        bool ready = await dependencyInstaller.WaitForPostgresReadyAsync(cancellationToken).ConfigureAwait(false);
        if (!ready)
            logger.LogError("Local PostgreSQL did not respond in time.");

        return ready;
    }
}
