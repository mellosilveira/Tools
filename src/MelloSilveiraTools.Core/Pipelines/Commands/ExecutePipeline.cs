using MelloSilveiraTools.Core.Commands;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.Pipelines.Notifiers;
using MelloSilveiraTools.Core.Validators;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.Core.Pipelines.Commands;

/// <summary>
/// Base command that validates a pipeline request, pre-allocates its real-time progress channel,
/// dispatches execution asynchronously on a background worker, and immediately returns <c>202 Accepted</c>.
/// </summary>
/// <typeparam name="TRequest">Validated request payload type.</typeparam>
/// <typeparam name="TInput">Pipeline execution input deriving from <see cref="PipelineInputBase"/>.</typeparam>
/// <typeparam name="TOutput">Pipeline output payload type.</typeparam>
/// <param name="logger">Structured logger for background execution telemetry.</param>
/// <param name="progressNotifier">Pipeline progress notifier whose channel is pre-allocated prior to background scheduling.</param>
/// <param name="pipeline">Target pipeline instance executed in the background.</param>
/// <param name="validator">Request validator invoked prior to scheduling.</param>
public abstract class ExecutePipeline<TRequest, TInput, TOutput>(
    ILogger<ExecutePipeline<TRequest, TInput, TOutput>> logger,
    IPipelineProgressNotifier progressNotifier,
    IPipeline<TInput, TOutput> pipeline,
    IValidator<TRequest> validator)
    : CommandBaseWithData<TRequest, string>(validator)
    where TRequest : class
    where TInput : PipelineInputBase
{
    /// <inheritdoc/>
    protected override Task<Result<string>> ExecuteCommandAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string pipelineTypeName = pipeline.GetType().Name;
        TInput input = MapToInput(request);

        // Pre-allocate the channel before returning 202 Accepted so immediate /progress subscribers never observe 404.
        progressNotifier.EnsureChannel(input.Identifier);

        _ = Task.Run(async () =>
        {
            try
            {
                logger.LogInformation("Starting pipeline. Pipeline: {Pipeline}. Input: {@Input}.", pipelineTypeName, input);

                Result<TOutput> result = await pipeline.ProcessAsync(input).ConfigureAwait(false);

                if (result.Success)
                {
                    logger.LogInformation("Pipeline completed successfully. Pipeline: {Pipeline}. Input: {@Input}. Result: {@Result}", pipelineTypeName, input, result);
                }
                else
                {
                    logger.LogError("Pipeline failed. Pipeline: {Pipeline}. Input: {@Input}. Result: {@Result}", pipelineTypeName, input, result);
                    progressNotifier.TryComplete(input.Identifier);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Pipeline faulted. Pipeline: {Pipeline}. Request: {@Request}. Input: {@Input}.", pipelineTypeName, request, input);
                progressNotifier.TryComplete(input.Identifier, ex);
            }
        }, CancellationToken.None);

        return Task.FromResult(Result.CreateSuccessAccepted(input.Identifier));
    }

    protected abstract TInput MapToInput(TRequest request);
}
