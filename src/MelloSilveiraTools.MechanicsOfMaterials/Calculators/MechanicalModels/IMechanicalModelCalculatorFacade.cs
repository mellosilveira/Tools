using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;

/// <summary>
/// Defines a unified facade for all mechanical model calculators.
/// Hides the underlying complexity, specific mathematical implementations, and generic constitutive parameters from the client.
/// </summary>
public interface IMechanicalModelCalculatorFacade
{
    /// <summary>
    /// Calculates the consolidated mechanical model's output at a specific point in time.
    /// </summary>
    /// <param name="time">The elapsed time for the calculation. Unit: s (second).</param>
    /// <returns>The mechanical model's output, encapsulating the calculated stress, strain, force, and displacement.</returns>
    MechanicalModelOutput Calculate(double time);
}