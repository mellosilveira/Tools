namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

/// <summary>
/// Contains the load-response relationships available for mechanical analysis.
/// </summary>
public enum LoadResponseRelationship : int
{
    /// <summary>
    /// Stress-strain relationship.
    /// </summary>
    StressStrain = 1,

    /// <summary>
    /// Force-displacement relationship.
    /// </summary>
    ForceDisplacement = 2,
}
