using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.Core.Pipelines.Single;

/// <summary>
/// Static entry point to initialize the pipeline cleanly.
/// </summary>
public static class FluentPipelineFactory
{
    public static IFluentPipelineBuilder<T, T> Start<T>() => new PipelineBuilder<T, T>();
}

file class PipelineBuilder<TInitialIn, TCurrentOut>(
    List<(string Name, Func<object, CancellationToken, Task<object>> Func)> steps)
    : IFluentPipelineBuilder<TInitialIn, TCurrentOut>
{
    /// <summary>
    /// Initial constructor to bootstrap the pipeline.
    /// </summary>
    public PipelineBuilder() : this([]) { }

    public IFluentPipelineBuilder<TInitialIn, TNextOut> AddStep<TNextOut>(string stepName, Func<TCurrentOut, CancellationToken, Task<TNextOut>> stepFunc)
    {
        // Wrap the strongly-typed execution in a delegate that accepts and returns an object
        async Task<object> erasedFunc(object objInput, CancellationToken ct) => (await stepFunc((TCurrentOut)objInput, ct).ConfigureAwait(false))!;
        return new PipelineBuilder<TInitialIn, TNextOut>([.. steps, (stepName, erasedFunc)]);
    }

    public IFluentPipeline<TInitialIn, TCurrentOut> Build(ILogger? logger = null) => new PipelineEngine<TInitialIn, TCurrentOut>(logger, steps);
}

/// <summary>
/// The internal execution engine. 
/// Uses File-Scoped types (accessible only within this file) to protect the internal implementation details.
/// </summary>
/// <typeparam name="TInitialIn"></typeparam>
/// <typeparam name="TFinalOut"></typeparam>
/// <param name="logger"></param>
/// <param name="steps"></param>
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

        foreach (var (name, func) in steps)
        {
            cancellationToken.ThrowIfCancellationRequested();

            logger?.LogInformation("Executing step: '{StepName}'. Input type: {CurrentDataType}. Input Data: {@StepInput}", name, currentData.GetType().Name, currentData);

            try
            {
                currentData = await func(currentData, cancellationToken).ConfigureAwait(false);

                logger?.LogInformation("Step '{StepName}' completed successfully. Output type: {OutputDataType}. Output Data: {@StepOutput}", name, currentData.GetType().Name, currentData);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger?.LogError(ex, "Pipeline execution failed at step '{StepName}'. Data state at failure: {@FailedDataState}", name, currentData);
                throw new PipelineExecutionException(name, $"Failed to execute step '{name}'.", ex);
            }
        }

        logger?.LogInformation("Pipeline execution finished successfully. Final output type: {FinalOutputType}. Final Data: {@FinalOutput}", typeof(TFinalOut).Name, currentData);
        return (TFinalOut)currentData;
    }
}