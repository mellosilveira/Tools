using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.Schapery;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.NonLinear.Schapery;

/// <summary>
/// Schapery's non-linear viscoelastic model.
/// It uses the Boltzmann superposition method, with thermodynamics concepts, to determine a non-linear strain-stress relation. 
/// For soft tissue analysis, the influence of temperature is not considered since the temperature is constant or has slight
/// variation in a real body.
/// The main limitation of Schapery’s model is the dependence on the relaxation function value in the equilibrium condition (when 
/// the time tends to infinity). This is considered a limitation since the accuracy of the numerical model depends on finding the 
/// equilibrium state in the experimental tests. This may require periods in the range of hours, as observed in several researches. 
/// For more details, see on section "Bibliographies" on file "README.MD".
/// </summary>
public interface ISchaperyModelCalculator : IViscoelasticModelCalculator<SchaperyModelInput>
{
    /// <summary>
    /// Calculates the transient relaxation function.
    /// For more details, see on section "Bibliographies" on file "README.MD".
    /// </summary>
    /// <param name="input">The mechanical model's input.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <returns>Unit: MPa (Mega-Pascal).</returns>
    double CalculateTransientRelaxationFunction(SchaperyModelInput input, double time);

    /// <summary>
    /// Calculates the transient creep compliance.
    /// For more details, see on section "Bibliographies" on file "README.MD".
    /// </summary>
    /// <param name="input">The mechanical model's input.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <returns>Unit: /Mpa (per Mega-Pascal).</returns>
    double CalculateTransientCreepCompliance(SchaperyModelInput input, double time);

    /// <summary>
    /// Calculates the reduced time function to be used on stress calculation.
    /// For analysis with low stress level, as soft tissue analysis, this variable is considered equal to time.
    /// For more details, see on section "Bibliographies" on file "README.MD".
    /// </summary>
    /// <param name="input">The mechanical model's input.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <returns>Unit: s (second).</returns>
    double CalculateReducedTimeFunction(SchaperyModelInput input, double time);

    /// <summary>
    /// Calculates the retardation time function to be used on strain calculation.
    /// For analysis with low stress level, as soft tissue analysis, this variable is considered equal to time.
    /// </summary>
    /// <param name="input">The mechanical model's input.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <returns>Unit: s (second).</returns>
    double CalculateRetardationTimeFunction(SchaperyModelInput input, double time);

    /// <summary>
    /// Calculates the stress shift factor to be used when calculating the reduced time function for relaxation analysis.
    /// </summary>
    /// <param name="input">The mechanical model's input.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <returns>Unit: dimensionless.</returns>
    double CalculateStressShiftFactor(SchaperyModelInput input, double time);

    /// <summary>
    /// Calculates the temperature shift factor to be used when calculating the retardation time function for creep analysis.
    /// </summary>
    /// <param name="input">The mechanical model's input.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <returns>Unit: dimensionless.</returns>
    double CalculateTemperatureShiftFactor(SchaperyModelInput input, double time);
}