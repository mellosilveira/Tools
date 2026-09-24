namespace MelloSilveiraTools.Mathematics.Models.NumericalMethods;

/// <summary>
/// Contains the input data for integrations.
/// </summary>
/// <param name="InitialPoint"></param>
/// <param name="FinalPoint"></param>
/// <param name="Step"></param>
public record IntegralInput(double InitialPoint, double FinalPoint, double Step)
{
    public IntegralInput(double finalPoint, double step) : this(MathematicConstants.InitialTime, finalPoint, step) { }
}
