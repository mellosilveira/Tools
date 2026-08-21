namespace MelloSilveiraTools.Core.Pipelines.Single;

/// <summary>
/// Represents the fully constructed pipeline, ready for execution.
/// </summary>
public interface IFluentPipeline<TIn, TOut>
{
    Task<TOut> ExecuteAsync(TIn input, CancellationToken cancellationToken = default);
}

public static class PipelineExtensions
{
    extension<TInitial, TCurrentOut>(IFluentPipelineBuilder<TInitial, TCurrentOut> builder)
    {
        /// <summary>
        /// Appends a new asynchronous processing step to the pipeline.
        /// </summary>
        public IFluentPipelineBuilder<TInitial, TNextOut> AddStep<TNextOut>(IStep<TCurrentOut, TNextOut> step)
        {
            ArgumentNullException.ThrowIfNull(step);
            return builder.AddStep(step.GetType().Name, step.ExecuteAsync);
        }

        /// <summary>
        /// Maps or transforms the data from the previous step using a Lambda expression.
        /// </summary>
        public IFluentPipelineBuilder<TInitial, TNextOut> AddDataMapping<TNextOut>(Func<TCurrentOut, TNextOut> mapper)
        {
            ArgumentNullException.ThrowIfNull(mapper);
            return builder.AddStep("DataMapping", (input, _) => Task.FromResult(mapper(input)));
        }
    }
}
