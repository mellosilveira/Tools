using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.Core.Pipelines.Fluent;

/// <summary>
/// Orchestrates the sequential chaining of asynchronous pipeline steps.
/// Utilizes delegate wrapping (Type Erasure) to strip compile-time generic constraints, allowing 
/// heterogeneous return types to be stored in a unified internal execution collection.
/// </summary>
internal class FluentPipelineBuilder<TInitialIn, TCurrentOut>(List<(string Name, Func<object, CancellationToken, Task<object>> Func)> steps) : IFluentPipelineBuilder<TInitialIn, TCurrentOut>
{
    /// <summary>
    /// Initializes the root builder with an empty execution sequence.
    /// </summary>
    public FluentPipelineBuilder() : this([]) { }

    /// <inheritdoc/>
    public IFluentPipelineBuilder<TInitialIn, TNextOut> AddStep<TNextOut>(string stepName, Func<TCurrentOut, CancellationToken, Task<TNextOut>> stepFunc)
    {
        // Encapsulate the strongly-typed execution within a unified object-based delegate
        async Task<object> erasedFunc(object objInput, CancellationToken ct) => (await stepFunc((TCurrentOut)objInput, ct).ConfigureAwait(false))!;
        return new FluentPipelineBuilder<TInitialIn, TNextOut>([.. steps, (stepName, erasedFunc)]);
    }

    /// <inheritdoc/>
    public IFluentPipeline<TInitialIn, TCurrentOut> Build(ILogger? logger = null) => new PipelineEngine<TInitialIn, TCurrentOut>(logger, steps);
}

/// <summary>
/// The terminal execution engine responsible for iterating and awaiting the type-erased delegate chain.
/// Manages state transitions, cancellation propagation, and structured logging of payload mutations.
/// </summary>
/// <typeparam name="TInitialIn">The immutable starting input type validated at compile time.</typeparam>
/// <typeparam name="TFinalOut">The guaranteed final output type returned to the caller upon successful execution.</typeparam>
/// <param name="logger">Optional structured logger for capturing state mutations and fault payloads.</param>
/// <param name="steps">The chronologically ordered list of type-erased asynchronous delegates.</param>
file class PipelineEngine<TInitialIn, TFinalOut>(
    ILogger? logger,
    IReadOnlyList<(string Name, Func<object, CancellationToken, Task<object>> Func)> steps)
    : IFluentPipeline<TInitialIn, TFinalOut>
{
    public async Task<TFinalOut> ExecuteAsync(TInitialIn input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        logger?.LogInformation("Starting pipeline execution. Total steps: {StepCount}. Initial input type: {InputType}. Initial Data: {@InitialInput}", steps.Count, typeof(TInitialIn).Name, input);

        object currentData = input;

        foreach ((string? stepName, Func<object, CancellationToken, Task<object>>? stepFunc) in steps)
        {
            cancellationToken.ThrowIfCancellationRequested();

            logger?.LogInformation("Executing step: '{StepName}'. Input type: {CurrentDataType}. Input Data: {@StepInput}", stepName, currentData.GetType().Name, currentData);

            try
            {
                currentData = await stepFunc(currentData, cancellationToken).ConfigureAwait(false);

                logger?.LogInformation("Step '{StepName}' completed successfully. Output type: {OutputDataType}. Output Data: {@StepOutput}", stepName, currentData.GetType().Name, currentData);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger?.LogError(ex, "Pipeline execution failed at step '{StepName}'. Data state at failure: {@FailedDataState}", stepName, currentData);
                throw new PipelineExecutionException(stepName, $"Failed to execute step '{stepName}'.", ex);
            }
        }

        logger?.LogInformation("Pipeline execution finished successfully. Final output type: {FinalOutputType}. Final Data: {@FinalOutput}", typeof(TFinalOut).Name, currentData);
        return (TFinalOut)currentData;
    }
}
