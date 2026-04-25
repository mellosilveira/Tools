using MelloSilveiraTools.MechanicsOfMaterials.Models.Fatigue;

namespace MelloSilveiraTools.MechanicsOfMaterials.Fatigue;

/// <summary>
/// It contains the Mechanical Fatigue constitutive equations.
/// </summary>
public interface IFatigueCalculator
{
    /// <summary>
    /// Runs a complete high-cycle fatigue analysis and returns stress amplitude, mean stress,
    /// the Goodman-equivalent stress, the estimated fatigue life and the Modified Goodman safety factor.
    /// </summary>
    /// <param name="input">The fatigue analysis input (applied stresses, material data, profile and correction factors).</param>
    /// <returns>The <see cref="FatigueResult"/> containing every computed fatigue quantity.</returns>
    /// <example>
    /// <code>
    /// var input = new FatigueInput
    /// {
    ///     MinimumAppliedStress = -50,
    ///     MaximumAppliedStress = 200,
    ///     TensileStress = 700,
    ///     FatigueLimit = 350,
    ///     FatigueLimitFraction = 0.9,
    ///     IsRotativeSection = true,
    ///     Reliability = Reliability.NinetyNinePercent,
    ///     SurfaceFinish = SurfaceFinish.Machined,
    ///     LoadingType = LoadingType.Bending,
    ///     Temperature = 25,
    ///     Profile = new CircularProfile { /* ... */ },
    /// };
    /// FatigueResult result = fatigueCalculator.CalculateFatigueResult(input);
    /// </code>
    /// </example>
    FatigueResult CalculateFatigueResult(FatigueInput input);

    /// <summary>
    /// Calculates the modified (corrected) fatigue endurance limit Se by multiplying the uncorrected
    /// fatigue limit Se' by the applicable Marin factors (surface, size, loading, temperature and reliability).
    /// </summary>
    /// <param name="input">The fatigue analysis input providing the uncorrected fatigue limit and the parameters required to evaluate every Marin factor.</param>
    /// <returns>The modified fatigue endurance limit Se in MPa.</returns>
    double CalculateModifiedFatigueStress(FatigueInput input);
}