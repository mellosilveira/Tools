using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.Core.Pipelines.Single;

/// <summary>
/// Fluent builder for the pipeline.
/// Exposes an internal append method that will be consumed by the extension methods.
/// </summary>
public interface IFluentPipelineBuilder<TInitialIn, TCurrentOut>
{
    IFluentPipelineBuilder<TInitialIn, TNextOut> AddStep<TNextOut>(string stepName, Func<TCurrentOut, CancellationToken, Task<TNextOut>> stepFunc);

    IFluentPipeline<TInitialIn, TCurrentOut> Build(ILogger? logger = null);
}
