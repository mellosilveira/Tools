using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;

/// <summary>
/// Facade for <see cref="IMechanicalModelCalculator{TInput}"/>
/// </summary>
public interface IMechanicalModelCalculatorFacade : IMechanicalModelCalculator<MechanicalModelInput>
{
    /// <summary>
    /// Calculates the mechanical model's result.
    /// </summary>
    /// <param name="time">Unit: s (second).</param>
    /// <returns>The mechanical model's result.</returns>
    MechanicalModelResult CalculateResult(double time);
}
