namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models;

/// <summary>
/// Defines the execution strategy for batch pipeline operations.
/// </summary>
public enum PipelineExecutionMode
{
    /// <summary>
    /// Executes tasks sequentially one after another.
    /// </summary>
    Sequential = 0,

    /// <summary>
    /// Executes tasks concurrently in parallel with controlled degree of parallelism.
    /// </summary>
    Parallel = 1
}
