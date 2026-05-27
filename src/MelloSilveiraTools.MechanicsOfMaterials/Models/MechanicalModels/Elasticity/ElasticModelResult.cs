using MelloSilveiraTools.MechanicsOfMaterials.Attributes;

namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Elasticity;

/// <summary>
/// Contains the output for a elastic model.
/// </summary>
public class ElasticModelOutput : MechanicalModelOutput
{
    /// <summary>
    /// Unit: N/m (Newton per meter).
    /// </summary>
    [MechanicalModelParameter]
    public double Stiffness { get; set; }
}
