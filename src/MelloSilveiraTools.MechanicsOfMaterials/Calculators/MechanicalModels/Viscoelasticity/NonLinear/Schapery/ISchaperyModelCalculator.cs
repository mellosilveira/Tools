using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.Schapery;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.NonLinear.Schapery;

/// <summary>
/// Defines a calculator for Schapery's non-linear viscoelastic model.
/// </summary>
/// <remarks>
/// It uses the Boltzmann superposition principle combined with thermodynamic concepts to determine a non-linear stress-strain relationship.
/// 
/// For soft tissue analysis, the influence of temperature is typically disregarded, as the temperature remains constant or has slight variations in a real biological body.
/// 
/// The main limitation of Schapery’s model is its dependence on the relaxation function value in the equilibrium condition (when time approaches infinity). 
/// This requires finding the equilibrium state in experimental tests, which may take hours, as observed in several biomechanical researches.
/// For more details, see the "Bibliographies" section in the "README.md" file.
/// </remarks>
public interface ISchaperyModelCalculator : IViscoelasticModelCalculator<SchaperyConstitutiveParameters>
{
    /// <summary>
    /// Calculates the transient relaxation function.
    /// For more details, see the "Bibliographies" section in the "README.md" file.
    /// </summary>
    /// <param name="input">The mechanical model's input data.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <returns>Unit: MPa (Mega-Pascal).</returns>
    double CalculateTransientRelaxationFunction(MechanicalModelInput<SchaperyConstitutiveParameters> input, double time);

    /// <summary>
    /// Calculates the transient creep compliance.
    /// For more details, see the "Bibliographies" section in the "README.md" file.
    /// </summary>
    /// <param name="input">The mechanical model's input data.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <returns>Unit: /MPa (per Mega-Pascal).</returns>
    double CalculateTransientCreepCompliance(MechanicalModelInput<SchaperyConstitutiveParameters> input, double time);

    /// <summary>
    /// Calculates the reduced time function used in stress calculation (relaxation).
    /// For analyses with low stress levels (such as soft tissue analysis), this variable is often considered equal to the real time.
    /// For more details, see the "Bibliographies" section in the "README.md" file.
    /// </summary>
    /// <param name="input">The mechanical model's input data.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <returns>Unit: s (second).</returns>
    double CalculateReducedTimeFunction(MechanicalModelInput<SchaperyConstitutiveParameters> input, double time);

    /// <summary>
    /// Calculates the retardation time function used in strain calculation (creep).
    /// For analyses with low stress levels (such as soft tissue analysis), this variable is often considered equal to the real time.
    /// </summary>
    /// <param name="input">The mechanical model's input data.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <returns>Unit: s (second).</returns>
    double CalculateRetardationTimeFunction(MechanicalModelInput<SchaperyConstitutiveParameters> input, double time);

    /// <summary>
    /// Calculates the stress shift factor used when computing the reduced time function for relaxation analysis.
    /// </summary>
    /// <param name="input">The mechanical model's input data.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <returns>Unit: dimensionless.</returns>
    double CalculateStressShiftFactor(MechanicalModelInput<SchaperyConstitutiveParameters> input, double time);

    /// <summary>
    /// Calculates the temperature shift factor used when computing the retardation time function for creep analysis.
    /// </summary>
    /// <param name="input">The mechanical model's input data.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <returns>Unit: dimensionless.</returns>
    double CalculateTemperatureShiftFactor(MechanicalModelInput<SchaperyConstitutiveParameters> input, double time);
}