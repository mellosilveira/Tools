namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

// TODO: ALTERAR PARA LoadResponseRelationship.

/// <summary>
/// Contains the relationships available for mechanical analysis.
/// </summary>
public enum MechanicalRelationship
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
