using MelloSilveiraTools.MechanicsOfMaterials.Attributes;

namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.Linear.Maxwell;

/// <summary>
/// Contains the results for Maxwell's model.
/// </summary>
public sealed class MaxwellModelResult : ViscoelasticModelResult 
{
    /// <summary>
    /// Unit: s (second).
    /// </summary>
    [MechanicalModelParameter(ViscoelasticEffect.Relaxation)]
    public double? RelaxationTime { get; set; }
}
