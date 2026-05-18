using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

namespace SoftTissue.UseCases.Facade.MechanicalModels;

/// <summary>
/// Facade for <see cref="IMechanicalModelCalculator{TInput}"/>
/// </summary>
public interface IMechanicalModelCalculatorFacade
{
    /// <summary>
    /// Calculates the mechanical model's result.
    /// </summary>
    /// <param name="time">Unit: s (second).</param>
    /// <returns>The mechanical model's result.</returns>
    MechanicalModelResult CalculateResult(double time);

    /// <inheritdoc cref="IMechanicalModelCalculator{TInput}.CalculateDisplacement"/>
    double CalculateDisplacement(MechanicalModelInput input, double time, double force);

    /// <inheritdoc cref="IMechanicalModelCalculator{TInput}.CalculateForce"/>
    double CalculateForce(MechanicalModelInput input, double time, double displacement);

    /// <inheritdoc cref="IMechanicalModelCalculator{TInput}.CalculateStress"/>
    double CalculateStress(MechanicalModelInput input, double time, double strain);
}
