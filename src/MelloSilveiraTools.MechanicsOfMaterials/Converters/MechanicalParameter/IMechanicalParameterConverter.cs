using MelloSilveiraTools.MechanicsOfMaterials.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Converters.MechanicalParameter;

/// <summary>
/// Converts mechanical parameters.
/// </summary>
public interface IMechanicalParameterConverter
{
    /// <summary>
    /// Calculates the displacement from the strain.
    /// </summary>
    /// <param name="specimenParameter">See reference at <see cref="SpecimenParameter"/>.</param>
    /// <param name="strain">Unit: dimensionless.</param>
    /// <returns>Unit: m (meter).</returns>
    double CalculateDisplacementFromStrain(SpecimenParameter specimenParameter, double strain);

    /// <summary>
    /// Calculates the displacement derivative from the strain.
    /// </summary>
    /// <param name="specimenParameter">See reference at <see cref="SpecimenParameter"/>.</param>
    /// <param name="strain">Unit: dimensionless.</param>
    /// <param name="strainDerivative"></param>
    /// <returns>Unit: m/s (meter per second).</returns>
    double CalculateDisplacementDerivativeFromStrain(SpecimenParameter specimenParameter, double strain, double strainDerivative);

    /// <summary>
    /// Calculates the strain from the displacement.
    /// </summary>
    /// <param name="specimenParameter">See reference at <see cref="SpecimenParameter"/>.</param>
    /// <param name="displacement">Unit: m (meter).</param>
    /// <returns>Unit: dimensionless.</returns>
    double CalculateStrainFromDisplacement(SpecimenParameter specimenParameter, double displacement);

    /// <summary>
    /// Calculates the strain derivative from the displacement.
    /// </summary>
    /// <param name="specimenParameter">See reference at <see cref="SpecimenParameter"/>.</param>
    /// <param name="displacement">Unit: m (meter).</param>
    /// <param name="displacementDerivative">Unit: m/s (meter per second).</param>
    /// <returns>Unit: /s (per second).</returns>
    double CalculateStrainDerivativeFromDisplacement(SpecimenParameter specimenParameter, double displacement, double displacementDerivative);

    /// <summary>
    /// Calculates the displacement from the strain considering the preload configuration as the base.
    /// </summary>
    /// <param name="specimenParameter">See reference at <see cref="SpecimenParameter"/>.</param>
    /// <param name="strain">Unit: dimensionless.</param>
    /// <returns>Unit: m (meter).</returns>
    double CalculatePreloadedDisplacementFromStrain(SpecimenParameter specimenParameter, double strain);

    /// <summary>
    /// Calculates the displacement derivative from the strain considering the preload configuration as the base.
    /// </summary>
    /// <param name="specimenParameter">See reference at <see cref="SpecimenParameter"/>.</param>
    /// <param name="strain">Unit: dimensionless.</param>
    /// <param name="strainDerivative"></param>
    /// <returns>Unit: m/s (meter per second).</returns>
    double CalculatePreloadedDisplacementDerivativeFromStrain(SpecimenParameter specimenParameter, double strain, double strainDerivative);

    /// <summary>
    /// Calculates the strain from the displacement considering the preload configuration as the base.
    /// </summary>
    /// <param name="specimenParameter">See reference at <see cref="SpecimenParameter"/>.</param>
    /// <param name="displacement">Unit: m (meter).</param>
    /// <returns>Unit: dimensionless.</returns>
    double CalculatePreloadedStrainFromDisplacement(SpecimenParameter specimenParameter, double displacement);

    /// <summary>
    /// Calculates the strain derivative from the displacement considering the preload configuration as the base.
    /// </summary>
    /// <param name="specimenParameter">See reference at <see cref="SpecimenParameter"/>.</param>
    /// <param name="displacement">Unit: m (meter).</param>
    /// <param name="displacementDerivative">Unit: m/s (meter per second).</param>
    /// <returns>Unit: /s (per second).</returns>
    double CalculatePreloadedStrainDerivativeFromDisplacement(SpecimenParameter specimenParameter, double displacement, double displacementDerivative);

    /// <summary>
    /// Calculates the force from the stress.
    /// </summary>
    /// <param name="specimenParameter">See reference at <see cref="SpecimenParameter"/>.</param>
    /// <param name="stress">Unit: MPa (Mega-Pascal).</param>
    /// <returns>Unit: N (Newton).</returns>
    double CalculateForceFromStress(SpecimenParameter specimenParameter, double stress);

    /// <summary>
    /// Calculates the stress from the force.
    /// </summary>
    /// <param name="specimenParameter">See reference at <see cref="SpecimenParameter"/>.</param>
    /// <param name="force">Unit: N (Newton).</param>
    /// <returns>Unit: MPa (Mega-Pascal).</returns>
    double CalculateStressFromForce(SpecimenParameter specimenParameter, double force);

    /// <summary>
    /// Calculates the stress derivative from the force.
    /// </summary>
    /// <param name="specimenParameter">See reference at <see cref="SpecimenParameter"/>.</param>
    /// <param name="force">Unit: N (Newton).</param>
    /// <param name="forceDerivative">Unit: N/s (Newton per second).</param>
    /// <returns>Unit: /s (per second).</returns>
    double CalculateStressDerivativeFromForce(SpecimenParameter specimenParameter, double force, double forceDerivative);
}
