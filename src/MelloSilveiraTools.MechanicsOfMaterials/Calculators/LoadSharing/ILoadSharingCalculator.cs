using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.LoadSharing;

/// <summary>
/// Performs calculations for load sharing.
/// </summary>
public interface ILoadSharingCalculator
{
    /// <summary>
    /// Creates the specimen's displacement based on the system displacement and input for mechanical model.
    /// </summary>
    /// <param name="systemDisplacement">The displacement imposed to system.</param>
    /// <param name="specimenParameter">See reference at <see cref="SpecimenParameter"/>.</param>
    /// <returns>An instance of <see cref="MechanicalParameter"/> to represent the specimen's displacement.</returns>
    MechanicalParameter CreateSpecimenDisplacement(MechanicalParameter systemDisplacement, SpecimenParameter specimenParameter);

    /// <summary>
    /// Calculates the system's displacement based on the specimen displacement and input for mechanical model.
    /// </summary>
    /// <param name="specimenParameter">See reference at <see cref="SpecimenParameter"/>.</param>
    /// <param name="specimenDisplacement">Unit: m (meters).</param>
    /// <returns>Unit: m (meter).</returns>
    double CalculateSystemDisplacement(SpecimenParameter specimenParameter, double specimenDisplacement);

    /// <summary>
    /// Calculates the specimen's angle based on the system displacement and input for mechanical model.
    /// </summary>
    /// <param name="specimenParameter">See reference at <see cref="SpecimenParameter"/>.</param>
    /// <param name="systemDisplacement">The displacement imposed to system. Unit: m (meter).</param>
    /// <returns>Unit: rad (radians).</returns>
    Vector3D CalculateSpecimenAngle(SpecimenParameter specimenParameter, double systemDisplacement);

    /// <summary>
    /// Calculates the specimen's displacement based on the system displacement and input for mechanical model.
    /// </summary>
    /// <param name="specimenParameter">See reference at <see cref="SpecimenParameter"/>.</param>
    /// <param name="systemDisplacement">The displacement imposed to system. Unit: m (meter).</param>
    /// <returns>Unit: m (meter).</returns>
    double CalculateSpecimenDisplacement(SpecimenParameter specimenParameter, double systemDisplacement);

    /// <summary>
    /// Calculates the specimen's displacement based on the system load sharing parameters and input for mechanical model.
    /// </summary>
    /// <param name="systemDisplacement">The displacement imposed to system.</param>
    /// <param name="specimenParameter">See reference at <see cref="SpecimenParameter"/>.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <returns>Unit: m (meter).</returns>
    double CalculateSpecimenDisplacement(MechanicalParameter systemDisplacement, SpecimenParameter specimenParameter, double time);

    /// <summary>
    /// Calculates the specimen's displacement derivative based on the system load sharing parameters and input for mechanical model.
    /// </summary>
    /// <param name="specimenParameter">See reference at <see cref="SpecimenParameter"/>.</param>
    /// <param name="systemDisplacementDerivative">The displacement derivative imposed to system. Unit: m (meter).</param>
    /// <param name="systemDisplacement">The displacement imposed to system. Unit: m (meter).</param>
    /// <returns>Unit: m/s (meter per second).</returns>
    double CalculateSpecimenDisplacementDerivative(SpecimenParameter specimenParameter, double systemDisplacementDerivative, double systemDisplacement);

    /// <summary>
    /// Calculates the specimen's displacement derivative based on the system load sharing parameters and input for mechanical model.
    /// </summary>
    /// <param name="systemDisplacement">The displacement imposed to system.</param>
    /// <param name="specimenParameter">See reference at <see cref="SpecimenParameter"/>.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <returns>Unit: m/s (meter per second).</returns>
    double CalculateSpecimenDisplacementDerivative(MechanicalParameter systemDisplacement, SpecimenParameter specimenParameter, double time);

    /// <summary>
    /// Calculates the displacement and angle of specimen based on the system displacement and input for mechanical model.
    /// </summary>
    /// <param name="specimenParameter">See reference at <see cref="SpecimenParameter"/>.</param>
    /// <param name="systemDisplacement">The displacement imposed to system. Unit: m (meter).</param>
    /// <returns>Unit of angle: rad (radians). Unit of displacement: m (meter).</returns>
    (Vector3D Angle, double Displacement) CalculateSpecimenAngleAndDisplacement(SpecimenParameter specimenParameter, double systemDisplacement);

    /// <summary>
    /// Calculates the displacement and its derivative of specimen based on the system load sharing parameters and input for mechanical model.
    /// </summary>
    /// <param name="systemDisplacement">The displacement imposed to system.</param>
    /// <param name="specimenParameter">See reference at <see cref="SpecimenParameter"/>.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <returns>Unit of displacement: m (meter). Unit of displacement derivative: m/s (meter per second).</returns>
    (double Displacement, double DisplacementDerivative) CalculateSpecimenDisplacementAndDerivative(MechanicalParameter systemDisplacement, SpecimenParameter specimenParameter, double time);

    /// <summary>
    /// Calculates the specimen force according to system axis.
    /// </summary>
    /// <param name="angle">Specimen angle according to system axis.</param>
    /// <param name="force">Unit: N (Newton).</param>
    /// <returns>Unit: N (Newton).</returns>
    double CalculateSpecimenForceOnSystemAxis(Vector3D angle, double force);
}
