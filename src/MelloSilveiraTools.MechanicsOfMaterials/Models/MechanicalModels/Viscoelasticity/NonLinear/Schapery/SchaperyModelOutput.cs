using MelloSilveiraTools.MechanicsOfMaterials.Attributes;

namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.Schapery;

/// <summary>
/// Contains the output for Schapery's model.
/// </summary>
public sealed class SchaperyModelOutput : ViscoelasticModelOutput 
{
    /// <summary>
    /// Unit: /Mpa (per Mega-Pascal).
    /// </summary>
    [MechanicalModelParameter(ViscoelasticEffect.Creep)]
    public double? TransientCreepCompliance { get; set; }

    /// <summary>
    /// Unit: MPa (Mega-Pascal).
    /// </summary>
    [MechanicalModelParameter(ViscoelasticEffect.Relaxation)]
    public double? TransientRelaxationFunction { get; set; }
}
