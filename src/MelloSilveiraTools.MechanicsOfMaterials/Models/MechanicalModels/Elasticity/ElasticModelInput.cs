namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Elasticity;

/// <summary>
/// Contains the input data for a elastic model.
/// </summary>
public record ElasticModelInput : MechanicalModelInput
{
    /// <summary>
    /// Unit: MPa (Mega-Pascal).
    /// </summary>
    public double ElasticModulus { get; init; }
}
