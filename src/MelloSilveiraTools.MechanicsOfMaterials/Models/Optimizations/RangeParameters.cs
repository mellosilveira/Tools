namespace MelloSilveiraTools.MechanicsOfMaterials.Models.Optimizations;

/// <summary>
/// Contains the parameters to build a range of thiss.
/// </summary>
public record RangeParameters
{
    /// <summary>
    /// The initial point.
    /// </summary>
    public double InitialPoint { get; init; }

    /// <summary>
    /// The step used to iterate from initial point to final point.
    /// </summary>
    public double? Step { get; init; }

    /// <summary>
    /// The multiplicative factor used to iterate from initial point to final point.
    /// </summary>
    public double? MultiplicativeFactor { get; init; }

    /// <summary>
    /// The final point.
    /// </summary>
    public double? FinalPoint { get; init; }
}
