namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;

/// <summary>
/// Defines the primary time-dependent mechanical behaviors observed in viscoelastic models.
/// </summary>
public enum ViscoelasticEffect
{
    /// <summary>
    /// Stress relaxation is the gradual decrease in stress over time when the model is held at a constant strain.
    /// </summary>
    Relaxation = 1,

    /// <summary>
    /// Creep is the gradual increase in strain over time when the model is subjected to a constant stress.
    /// </summary>
    Creep = 2
}