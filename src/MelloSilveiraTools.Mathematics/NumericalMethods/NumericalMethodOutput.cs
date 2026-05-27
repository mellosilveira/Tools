using MelloSilveiraTools.MechanicsOfMaterials.Models;
using System.Globalization;

namespace MelloSilveiraTools.Mathematics.NumericalMethods;

/// <summary>
/// Contains the finite element analysis output to a specific time.
/// </summary>
public class NumericalMethodOutput : TimebasedAnalysisOutput
{
    /// <summary>
    /// Basic constructor.
    /// </summary>
    public NumericalMethodOutput() { }

    /// <summary>
    /// Class constructor.
    /// </summary>
    /// <param name="numberOfBoundaryConditions"></param>
    public NumericalMethodOutput(uint numberOfBoundaryConditions)
    {
        Displacement = new double[numberOfBoundaryConditions];
        Velocity = new double[numberOfBoundaryConditions];
        Acceleration = new double[numberOfBoundaryConditions];
        EquivalentForce = new double[numberOfBoundaryConditions];
    }

    /// <summary>
    /// Unit: m (meter).
    /// </summary>
    public double[] Displacement { get; set; } = [];

    /// <summary>
    /// Unit: m/s (meter per second).
    /// </summary>
    public double[] Velocity { get; set; } = [];

    /// <summary>
    /// Unit: m/s² (meter per squared second).
    /// </summary>
    public double[] Acceleration { get; set; } = [];

    /// <summary>
    /// Unit: N (Newton).
    /// </summary>
    public double[] EquivalentForce { get; set; } = [];

    /// <inheritdoc/>
    public override string ToString()
    {
        static string Join(double[] values) =>
            string.Join(",", Array.ConvertAll(values, v => v.ToString(CultureInfo.InvariantCulture)));

        return string.Join(",",
            Time.ToString(CultureInfo.InvariantCulture),
            Join(Displacement),
            Join(Velocity),
            Join(Acceleration),
            Join(EquivalentForce));
    }
}
