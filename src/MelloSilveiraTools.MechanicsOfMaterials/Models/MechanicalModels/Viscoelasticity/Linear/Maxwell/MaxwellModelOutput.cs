using MelloSilveiraTools.MechanicsOfMaterials.Attributes;

namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.Linear.Maxwell;

/// <summary>
/// Contains the output for Maxwell's model.
/// </summary>
public sealed record MaxwellModelOutput : ViscoelasticModelOutput
{
    /// <summary>
    /// Unit: s (second).
    /// </summary>
    [MechanicalModelParameter(ViscoelasticEffect.Relaxation)]
    public double? RelaxationTime { get; set; }
}
