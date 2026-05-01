namespace MelloSilveiraTools.Mathematics.Models.NumericalMethods;

/// <summary>
/// Contains the input data for integrations.
/// </summary>
public record IntegralInput
{
    /// <summary>
    /// The initial point.
    /// </summary>
    public double InitialPoint { get; init; }

    /// <summary>
    /// The final point.
    /// </summary>
    public double FinalPoint { get; init; }

    /// <summary>
    /// The step size used while iterating.
    /// </summary>
    public double Step { get; init; }
}
