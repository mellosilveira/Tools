using MelloSilveiraTools.MechanicsOfMaterials.Attributes;

namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.Schapery;

/// <summary>
/// Contains the results for Schapery's model.
/// </summary>
public sealed class SchaperyModelResult : ViscoelasticModelResult 
{
    /// <summary>
    /// Unit: /Mpa (per Mega-Pascal).
    /// </summary>
    [MechanicalModelParameter(ViscoelasticEffect.Creep)]
    public double TransientCreepCompliance { get; set; }

    /// <summary>
    /// Unit: MPa (Mega-Pascal).
    /// </summary>
    [MechanicalModelParameter(ViscoelasticEffect.Relaxation)]
    public double TransientRelaxationFunction { get; set; }
}
