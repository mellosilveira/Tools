using MelloSilveiraTools.MechanicsOfMaterials.Attributes;

namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Elasticity;

/// <summary>
/// Contains the results for a elastic model.
/// </summary>
public class ElasticModelResult : MechanicalModelResult
{
    /// <summary>
    /// Unit: N/m (Newton per meter).
    /// </summary>
    [MechanicalModelParameter]
    public double Stiffness { get; set; }
}
