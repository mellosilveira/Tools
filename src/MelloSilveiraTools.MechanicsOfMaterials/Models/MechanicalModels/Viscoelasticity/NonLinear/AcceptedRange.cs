namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear;

/// <summary>
/// Contains the range of values accepted for a variable.
/// </summary>
public sealed record AcceptedRange
{
    public AcceptedRange() { }

    public AcceptedRange(double initialPoint, double finalPoint)
    {
        InitialPoint = initialPoint;
        FinalPoint = finalPoint;
    }

    /// <summary>
    /// Unit depends on which variable the accepted range is applied.
    /// </summary>
    public double InitialPoint { get; init; }

    /// <summary>
    /// Unit depends on which variable the accepted range is applied.
    /// </summary>
    public double FinalPoint { get; init; }

    public static AcceptedRange Default = new() { InitialPoint = double.NegativeInfinity, FinalPoint = double.PositiveInfinity };
}
