namespace MelloSilveiraTools.Core.Pipelines.Single;

/// <summary>
/// Custom exception to encapsulate execution errors within the pipeline.
/// </summary>
public class PipelineExecutionException(string stepName, string message, Exception innerException)
    : Exception(message, innerException)
{
    public string StepName { get; } = stepName;
}
