namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;

/// <summary>
/// Defines how the initial loading phase (ramp time) is treated in viscoelastic analysis.
/// </summary>
public enum RampTimeConsideration
{
    /// <summary>
    /// Includes the ramp time in the analysis, computing viscoelastic time-dependent effects during the loading phase.
    /// </summary>
    ConsiderWithViscoelasticEffect = 1,

    /// <summary>
    /// Includes the ramp time but assumes purely elastic behavior during loading. 
    /// Viscoelastic effects (like relaxation or creep) begin only after the ramp phase is complete.
    /// </summary>
    ConsiderWithoutViscoelasticEffect = 2,

    /// <summary>
    /// Assumes instantaneous loading (zero ramp time). 
    /// The target strain or stress is applied immediately as a step function.
    /// </summary>
    Disregard = 3,
}