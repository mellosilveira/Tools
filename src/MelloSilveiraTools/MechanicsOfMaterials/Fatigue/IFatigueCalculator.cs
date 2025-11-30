using MelloSilveiraTools.MechanicsOfMaterials.Models.Fatigue;

namespace MelloSilveiraTools.MechanicsOfMaterials.Fatigue;

/// <summary>
/// It contains the Mechanical Fatigue constitutive equations.
/// </summary>
public interface IFatigueCalculator
{
    /// <summary>
    /// This method calculates the result for fatigue analysis.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    FatigueResult CalculateFatigueResult(FatigueInput input);

    /// <summary>
    /// This method calculates the modified fatigue stress.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    double CalculateModifiedFatigueStress(FatigueInput input);
}