namespace MelloSilveiraTools.Mathematics.Models;

/// <summary>
/// Contains the initial and final boundary values of a calculation, along with their relative variation.
/// </summary>
public sealed record BoundaryValues
{
    /// <summary>
    /// Initializes a new instance of <see cref="BoundaryValues"/>.
    /// </summary>
    public BoundaryValues(double initialValue, double finalValue)
    {
        InitialValue = initialValue;
        FinalValue = finalValue;
        Variation = (finalValue - initialValue) / initialValue;
    }

    /// <summary>
    /// The value at the initial boundary of the calculation.
    /// </summary>
    public double InitialValue { get; }

    /// <summary>
    /// The value at the final boundary of the calculation.
    /// </summary>
    public double FinalValue { get; }

    /// <summary>
    /// The relative variation between the final and initial boundary values.
    /// </summary>
    public double Variation { get; }
}
