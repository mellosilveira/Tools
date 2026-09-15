using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;

/// <summary>
/// Defines a generic mechanical model calculator.
/// Establishes approaches for stress-strain and force-displacement relationships that approximate real-world behavior.
/// </summary>
/// <typeparam name="TConstitutiveParameters">The type of the constitutive parameters used as input for the mechanical model.</typeparam>
public interface IMechanicalModelCalculator<TConstitutiveParameters> where TConstitutiveParameters : ConstitutiveParameters
{
    /// <summary>
    /// Calculates the force, represented by F. 
    /// For more details, see the "Bibliographies" section in the "README.md" file.
    /// This method is primarily useful for load-sharing analysis.
    /// </summary>
    /// <param name="input">The mechanical model's input data.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <param name="displacement">
    /// Unit: m (meter).
    /// If not provided, this is calculated from the displacement parameters on the mechanical model's input.
    /// </param>
    /// <returns>Unit: N (Newton).</returns>
    double CalculateForce(MechanicalModelInput<TConstitutiveParameters> input, double time, double? displacement = null);

    /// <summary>
    /// Calculates the displacement, represented by δ. 
    /// For more details, see the "Bibliographies" section in the "README.md" file.
    /// This method is primarily useful for load-sharing analysis.
    /// </summary>
    /// <param name="input">The mechanical model's input data.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <param name="force">
    /// Unit: N (Newton).
    /// If not provided, this is calculated from the force parameters on the mechanical model's input.
    /// </param>
    /// <returns>Unit: m (meter).</returns>
    double CalculateDisplacement(MechanicalModelInput<TConstitutiveParameters> input, double time, double? force = null);

    /// <summary>
    /// Calculates the stress, represented by σ. 
    /// For more details, see the "Bibliographies" section in the "README.md" file.
    /// </summary>
    /// <param name="input">The mechanical model's input data.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <param name="strain">
    /// Unit: dimensionless. 
    /// If not provided, this is calculated from the strain parameters on the mechanical model's input.
    /// </param>
    /// <returns>Unit: MPa (Mega-Pascal).</returns>
    double CalculateStress(MechanicalModelInput<TConstitutiveParameters> input, double time, double? strain = null);

    /// <summary>
    /// Calculates the strain, represented by ε. 
    /// For more details, see the "Bibliographies" section in the "README.md" file.
    /// </summary>
    /// <param name="input">The mechanical model's input data.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <param name="stress">
    /// Unit: MPa (Mega-Pascal).
    /// If not provided, this is calculated from the stress parameters on the mechanical model's input.
    /// </param>
    /// <returns>Unit: dimensionless.</returns>
    double CalculateStrain(MechanicalModelInput<TConstitutiveParameters> input, double time, double? stress = null);
}