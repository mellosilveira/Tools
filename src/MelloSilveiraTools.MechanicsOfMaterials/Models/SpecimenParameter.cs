using MelloSilveiraTools.Mathematics.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Models;

/// <summary>
/// Contains parameters to be used for specimens.
/// Specimen is understood as a part of animal, piece of a mineral or others used as an example of its species or type for scientific study or display.
/// </summary>
public sealed record SpecimenParameter
{
    public SpecimenParameter(
        bool considerLargeDisplacement, 
        bool considerAngleVariation, 
        double preLoadStrain, 
        double preLoadLength,
        double area,
        Vector3D initialAngle)
    {
        ConsiderLargeDisplacement = considerLargeDisplacement;
        ConsiderAngleVariation = considerAngleVariation;
        PreLoadStrain = preLoadStrain;
        PreLoadLength = preLoadLength;
        Area = area;
        InitialAngle = initialAngle;
        
        PreLoadFactor = 1 + preLoadStrain;
        InitialLength = preLoadLength / PreLoadFactor;
        PreLoadDisplacement = preLoadLength - InitialLength;
    }

    /// <summary>
    /// True, if should consider that the analysis assumes large displacements.
    /// False, otherwise.
    /// </summary>
    public bool ConsiderLargeDisplacement { get; init; }

    /// <summary>
    /// True, if should consider that the angle variates after a force or a displacement is imposed to system. 
    /// False, otherwise.
    /// </summary>
    public bool ConsiderAngleVariation { get; init; }

    /// <summary>
    /// Strain caused by preload.
    /// Unit: dimensionless.
    /// </summary>
    public double PreLoadStrain { get; init; }

    /// <summary>
    /// Unit: m (meter).
    /// </summary>
    public double PreLoadLength { get; init; }

    /// <summary>
    /// Multiplying factor for preload.
    /// Unit: dimensionless.
    /// </summary>
    public double PreLoadFactor { get; }

    /// <summary>
    /// Unit: m (meter).
    /// </summary>
    public double PreLoadDisplacement { get; }

    /// <summary>
    /// Unit: m (meter).
    /// </summary>
    public double InitialLength { get; }

    /// <summary>
    /// Cross-sectional area.
    /// Unit: m² (square meter).
    /// </summary>
    public double Area { get; init; }

    /// <summary>
    /// Initial angle of the material with the system.
    /// Unit: rad (radians).
    /// </summary>
    public Vector3D InitialAngle { get; init; }
}
