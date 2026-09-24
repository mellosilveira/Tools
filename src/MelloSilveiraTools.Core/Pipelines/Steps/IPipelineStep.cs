namespace MelloSilveiraTools.Core.Pipelines.Steps;

/// <summary>
/// Defines the core metadata contract common to all pipeline execution step topologies.
/// Encapsulates naming semantics required for distributed tracing, structured telemetry, and logging.
/// </summary>
public interface IPipelineStep
{
    /// <summary>
    /// Gets the semantic identifier for this specific execution step.
    /// Required by the pipeline orchestration engines for structured telemetry, 
    /// distributed tracing, and precise fault localization within the execution graph.
    /// </summary>
    string Name { get; }
}
