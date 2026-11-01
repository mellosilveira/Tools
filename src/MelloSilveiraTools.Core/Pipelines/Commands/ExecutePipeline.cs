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
    protected override async Task<Result<string>> ExecuteCommandAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        string pipelineTypeName = pipeline.GetType().Name;
        TInput input = MapToInput(request);

        _ = Task.Run(async () =>
        {
            try
            {
                logger.LogInformation("Starting pipeline. Pipeline: {Pipeline}. Input: {@Input}.", pipelineTypeName, input);

                // TODO: TENTAR REPASSAR O CANCELLATION TOKEN PARA O PIPELINE
                Result<TOutput> result = await pipeline.ProcessAsync(input).ConfigureAwait(false);

                logger.LogInformation("Pipeline completed successfully. Pipeline: {Pipeline}. Input: {@Input}. Result: {@Result}", pipelineTypeName, request, input);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Pipeline faulted. Pipeline: {Pipeline}. Request: {@Request}. Input: {@Input}.", pipelineTypeName, request, input);
            }
        }, cancellationToken);

        return Result.CreateSuccessAccepted(input.Identifier);
    }

    protected abstract TInput MapToInput(TRequest request);
}
