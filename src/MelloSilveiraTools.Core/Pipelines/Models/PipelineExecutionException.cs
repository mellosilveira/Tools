namespace MelloSilveiraTools.Core.Pipelines.Models;

/// <summary>
/// Represents a terminal fault encountered during the traversal of the pipeline execution graph.
/// Encapsulates the underlying runtime exception alongside the semantic identifier of the faulting step 
/// to facilitate granular telemetry and distributed tracing.
/// </summary>
/// <param name="stepName">The semantic identifier of the pipeline step where the unhandled exception originated.</param>
/// <param name="message">A contextual message detailing the execution failure and state mutation context.</param>
/// <param name="innerException">The original runtime exception emitted by the step's execution delegate.</param>
public class PipelineExecutionException(string stepName, string message, Exception innerException) : Exception(message, innerException)
{
    /// <summary>
    /// Gets the semantic identifier of the faulted step. 
    /// Enables precise fault localization within the pipeline's execution topology.
    /// </summary>
    public string StepName { get; } = stepName;
}