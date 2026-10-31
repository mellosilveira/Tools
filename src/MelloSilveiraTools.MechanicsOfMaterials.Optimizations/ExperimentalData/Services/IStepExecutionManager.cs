using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Services;

/// <summary>
/// Service responsible for recording step-level checkpoints (Brotli-compressed input and output payloads)
/// and coordinating rollback purges when retrying an analysis from a specific pipeline step.
/// </summary>
public interface IStepExecutionManager
{
    /// <summary>
    /// Records the registration of a pipeline step prior to its execution, transitioning state to <c>Pending</c>.
    /// </summary>
    /// <typeparam name="TInput">The strongly-typed input payload.</typeparam>
    /// <param name="analysisIdentifier">Unique business identifier correlating the analysis.</param>
    /// <param name="stepName">The logical name of the step.</param>
    /// <param name="stepOrder">The 1-based sequential order of the step.</param>
    /// <param name="input">The input payload instance to compress and store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecordStepPendingAsync<TInput>(string analysisIdentifier, string stepName, int stepOrder, TInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records the initiation of a pipeline step, persisting its compressed input payload and transitioning state to <c>Running</c>.
    /// </summary>
    /// <typeparam name="TInput">The strongly-typed input payload.</typeparam>
    /// <param name="analysisIdentifier">Unique business identifier correlating the analysis.</param>
    /// <param name="stepName">The logical name of the step.</param>
    /// <param name="stepOrder">The 1-based sequential order of the step.</param>
    /// <param name="input">The input payload instance to compress and store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecordStepStartedAsync<TInput>(string analysisIdentifier, string stepName, int stepOrder, TInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records the successful completion of a pipeline step, persisting its compressed output payload and transitioning state to <c>Completed</c>.
    /// </summary>
    /// <typeparam name="TOutput">The strongly-typed output payload.</typeparam>
    /// <param name="analysisIdentifier">Unique business identifier correlating the analysis.</param>
    /// <param name="stepName">The logical name of the step.</param>
    /// <param name="stepOrder">The 1-based sequential order of the step.</param>
    /// <param name="output">The output payload instance to compress and store.</param>
    /// <param name="processedItems">Total consolidated number of items processed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecordStepCompletedAsync<TOutput>(string analysisIdentifier, string stepName, int stepOrder, TOutput output, int processedItems, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records the failure of a pipeline step, capturing the error message and transitioning state to <c>Faulted</c>.
    /// </summary>
    /// <param name="analysisIdentifier">Unique business identifier correlating the analysis.</param>
    /// <param name="stepName">The logical name of the step.</param>
    /// <param name="stepOrder">The 1-based sequential order of the step.</param>
    /// <param name="errorMessage">The error or exception message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecordStepFaultedAsync(string analysisIdentifier, string stepName, int stepOrder, string errorMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Identifies and records the currently running or highest active step as <c>Faulted</c>.
    /// </summary>
    /// <param name="analysisIdentifier">Unique business identifier correlating the analysis.</param>
    /// <param name="errorMessage">The error or exception message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecordCurrentRunningStepFaultedAsync(string analysisIdentifier, string errorMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all recorded step checkpoints for a given analysis ordered by <c>StepOrder</c>.
    /// </summary>
    /// <param name="analysisIdentifier">Unique business identifier correlating the analysis.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of step execution entities.</returns>
    Task<IReadOnlyList<AnalysisStepExecutionEntity>> GetStepExecutionsAsync(string analysisIdentifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a clean compensation rollback from a given step forward, purging generated storage files,
    /// deleting child database entities, and resetting step execution checkpoints.
    /// </summary>
    /// <param name="analysisIdentifier">Unique business identifier correlating the analysis.</param>
    /// <param name="fromStepOrder">The 1-based step order from which rollback should occur.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RollbackFromStepAsync(string analysisIdentifier, int fromStepOrder, CancellationToken cancellationToken = default);
}
