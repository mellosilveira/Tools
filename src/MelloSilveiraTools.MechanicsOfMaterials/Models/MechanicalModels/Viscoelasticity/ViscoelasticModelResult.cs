using MelloSilveiraTools.MechanicsOfMaterials.Attributes;

namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;

/// <summary>
/// Contains the results for a generic viscoelastic model.
/// </summary>
public class ViscoelasticModelResult : MechanicalModelResult
{
    /// <summary>
    /// Unit: /MPa (per Mega-Pascal) or m/N (meter per Newton).
    /// </summary>
    [MechanicalModelParameter(ViscoelasticEffect.Creep)]
    public double? CreepCompliance { get; set; }

    /// <summary>
    /// Unit: MPa (Mega-Pascal) or N/m (Newton per meter).
    /// </summary>
    [MechanicalModelParameter(ViscoelasticEffect.Relaxation)]
    public double? RelaxationFunction { get; set; }
}
