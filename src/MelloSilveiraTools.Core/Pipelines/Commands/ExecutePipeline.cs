using MelloSilveiraTools.Core.Application.Commands;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.Validators;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.Core.Pipelines.Commands;

public abstract class ExecutePipeline<TRequest, TInput, TOutput>(
    ILogger<ExecutePipeline<TRequest, TInput, TOutput>> logger,
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

        _ = Task.Run(async () =>
        {
            try
            {
                logger.LogInformation("Starting pipeline. Pipeline: {Pipeline}. Input: {@Input}.", pipelineTypeName, input);

                Result<TOutput> result = await pipeline.ProcessAsync(input).ConfigureAwait(false);

                if (result.Success)
                    logger.LogInformation("Pipeline completed successfully. Pipeline: {Pipeline}. Input: {@Input}. Result: {@Result}", pipelineTypeName, input, result);
                else
                    logger.LogError("Pipeline failed. Pipeline: {Pipeline}. Input: {@Input}. Result: {@Result}", pipelineTypeName, input, result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Pipeline faulted. Pipeline: {Pipeline}. Request: {@Request}. Input: {@Input}.", pipelineTypeName, request, input);
            }
        }, CancellationToken.None);

        return Task.FromResult(Result.CreateSuccessAccepted(input.Identifier));
    }

    protected abstract TInput MapToInput(TRequest request);
}
