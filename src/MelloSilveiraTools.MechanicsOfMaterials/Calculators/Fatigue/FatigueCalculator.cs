using MelloSilveiraTools.MechanicsOfMaterials.Models.Fatigue;
using MelloSilveiraTools.MechanicsOfMaterials.Models.Materials;
using MelloSilveiraTools.MechanicsOfMaterials.Models.Profiles;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.Fatigue;

/// <summary>
/// It contains the Mechanical Fatigue constitutive equations.
/// </summary>
public class FatigueCalculator : IFatigueCalculator
{
    /// <summary>
    /// Performs a high-cycle fatigue analysis on the supplied input. The method computes the
    /// stress amplitude, mean stress, the Goodman-equivalent stress, the estimated fatigue life
    /// (number of cycles, capped at 1e6) and the Modified Goodman safety factor.
    /// </summary>
    /// <param name="input">The fatigue analysis input (applied stresses, material data, profile and correction factors).</param>
    /// <returns>The <see cref="FatigueResult"/> containing stress amplitude, mean stress, equivalent stress, expected life and safety factor.</returns>
    public FatigueResult CalculateFatigueResult(FatigueInput input)
    {
        double stressAmplitude = Math.Abs((input.MaximumAppliedStress - input.MinimumAppliedStress) / 2);
        double meanStress = (input.MaximumAppliedStress + input.MinimumAppliedStress) / 2;
        double equivalentStress = stressAmplitude / (1 - meanStress / input.TensileStress);
        double modifiedFatigueStress = CalculateModifiedFatigueStress(input);

        double a = Math.Pow(input.FatigueLimitFraction * input.TensileStress, 2) / modifiedFatigueStress;
        double b = -Math.Log10(input.FatigueLimitFraction * input.TensileStress / modifiedFatigueStress) / 3;
        double numberOfCycles = Math.Pow(equivalentStress / a, 1 / b);

        return new()
        {
            StressAmplitude = stressAmplitude,
            MeanStress = meanStress,
            EquivalentStress = equivalentStress,
            NumberOfCycles = numberOfCycles > 1e6 ? 1e6 : numberOfCycles,
            SafetyFactor = Math.Pow(stressAmplitude / modifiedFatigueStress + meanStress / input.TensileStress, -1)
        };
    }

    /// <summary>
    /// Calculates the modified (corrected) fatigue endurance limit Se by applying the Marin
    /// correction factors — surface, size, loading, temperature and reliability — to the
    /// uncorrected fatigue limit Se'.
    /// </summary>
    /// <param name="input">The fatigue analysis input providing the uncorrected fatigue limit and the parameters required to evaluate every Marin factor.</param>
    /// <returns>The modified fatigue endurance limit Se in MPa.</returns>
    public double CalculateModifiedFatigueStress(FatigueInput input)
    {
        return input.FatigueLimit
            * CalculateSurfaceFactor(input.TensileStress, input.SurfaceFinish)
            * CalculateSizeFactor(input.Profile, input.LoadingType, input.IsRotativeSection)
            * CalculateLoadingFactor(input.LoadingType)
            * CalculateTemperatureFactor(input.Temperature)
            * CalculateReliabilityFactor(input.Reliability);
    }

    /// <summary>
    /// Calculates the Marin surface factor k_a using the empirical correlation k_a = a · S_ut^b,
    /// where the (a, b) pair is selected from the surface finish (rectified, machined/cold-rolled,
    /// hot-rolled or as-forged/wrought). Reference: Shigley's Mechanical Engineering Design,
    /// Table 6-2.
    /// </summary>
    /// <param name="tensileStress">The ultimate tensile strength S_ut of the material, in MPa.</param>
    /// <param name="surfaceFinish">The manufacturing surface finish of the part.</param>
    /// <returns>The dimensionless surface factor k_a.</returns>
    private double CalculateSurfaceFactor(double tensileStress, SurfaceFinish surfaceFinish)
    {
        (double a, double b) = surfaceFinish switch
        {
            SurfaceFinish.Rectified => (1.58, -0.085),
            SurfaceFinish.Machined => (4.51, -0.265),
            SurfaceFinish.ColdRolled => (4.51, -0.265),
            SurfaceFinish.HotRolled => (57.7, -0.718),
            SurfaceFinish.Wrought => (272, -0.995),
            _ => throw new ArgumentOutOfRangeException(nameof(surfaceFinish))
        };

        return a * Math.Pow(tensileStress, b);
    }

    /// <summary>
    /// Calculates the Marin size factor k_b. Returns 1 for purely axial loading. For bending or
    /// torsion the factor depends on the equivalent diameter d_e of the cross-section: for circular
    /// sections d_e = D when the section rotates about its axis or 0.37·D otherwise; for
    /// rectangular sections d_e = h when rotating or 0.808·√(b·h) otherwise. The piecewise
    /// correlation comes from Shigley's Mechanical Engineering Design (k_b = (d_e/7.62)^-0.107 for
    /// 2.79 ≤ d_e ≤ 51 mm and k_b = 1.51·d_e^-0.157 for 51 &lt; d_e ≤ 254 mm).
    /// </summary>
    /// <param name="profile">The cross-section profile (circular or rectangular). Linear dimensions are expected in millimeters.</param>
    /// <param name="loadingType">The loading type applied to the part.</param>
    /// <param name="isRotativeSection">True when the section rotates relative to the load direction (e.g. a rotating shaft); false otherwise.</param>
    /// <returns>The dimensionless size factor k_b.</returns>
    private double CalculateSizeFactor(Profile profile, LoadingType loadingType, bool isRotativeSection)
    {
        if (loadingType == LoadingType.Axial)
        {
            return 1;
        }

        double equivalentDiameter = profile switch 
        {
            CircularProfile circularProfile => isRotativeSection ? circularProfile.Diameter : 0.37 * circularProfile.Diameter,
            RectangularProfile rectangularProfile => isRotativeSection ? rectangularProfile.Height : 0.808 * Math.Sqrt(rectangularProfile.Width * rectangularProfile.Height),
            _ => 0
        };

        if (2.79 <= equivalentDiameter * 1000 && equivalentDiameter <= 51)
        {
            return Math.Pow(equivalentDiameter / 7.62, -0.107);
        }
        
        if (51 < equivalentDiameter * 1000 && equivalentDiameter <= 254)
        {
            return 1.51 * Math.Pow(equivalentDiameter, -0.157);
        }

        throw new ArgumentOutOfRangeException(nameof(profile));
    }

    /// <summary>
    /// Calculates the Marin loading factor k_c using the standard discrete values from Shigley's
    /// Mechanical Engineering Design: 1 for bending, 0.85 for axial loads and 0.59 for torsion.
    /// </summary>
    /// <param name="loadingType">The loading type applied to the part.</param>
    /// <returns>The dimensionless loading factor k_c.</returns>
    private double CalculateLoadingFactor(LoadingType loadingType)
    {
        return loadingType switch
        {
            LoadingType.Bending => 1,
            LoadingType.Axial => 0.85,
            LoadingType.Torsion => 0.59,
            _ => throw new ArgumentOutOfRangeException(nameof(loadingType))
        };
    }

    /// <summary>
    /// Calculates the Marin temperature factor k_d. The current implementation always returns 1
    /// because the operating temperature window for Baja SAE structural analyses does not vary
    /// enough to meaningfully reduce the endurance limit. Reference: Shigley's Mechanical
    /// Engineering Design.
    /// </summary>
    /// <param name="temperature">The operating temperature of the part, in degrees Celsius. Currently unused.</param>
    /// <returns>The dimensionless temperature factor k_d (always 1 in this implementation).</returns>
    private double CalculateTemperatureFactor(double temperature)
    {
        // It always retuns 1 because for Baja SAE analysis, this property does not affect and
        // does not variate significantly.
        return 1;
    }

    /// <summary>
    /// Calculates the Marin reliability factor k_e using the standard discrete values from
    /// Shigley's Mechanical Engineering Design (Table 6-5): 1.000 (50%), 0.897 (90%), 0.868 (95%),
    /// 0.814 (99%), 0.753 (99.9%).
    /// </summary>
    /// <param name="reliability">The desired reliability level for the fatigue analysis.</param>
    /// <returns>The dimensionless reliability factor k_e.</returns>
    private double CalculateReliabilityFactor(Reliability reliability)
    {
        return reliability switch
        {
            Reliability.Fifty => 1,
            Reliability.Ninety => 0.897,
            Reliability.NinetyFive => 0.868,
            Reliability.NinetyNine => 0.814,
            Reliability.NinetyNinePointNine => 0.753,
            _ => throw new ArgumentOutOfRangeException(nameof(reliability))
        };
    }
}
