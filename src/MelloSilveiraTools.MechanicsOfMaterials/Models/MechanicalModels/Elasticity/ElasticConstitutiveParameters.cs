namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Elasticity;

/// <summary>
/// Defines the parameters that govern the linear relationship between stress and strain in an elastic model.
/// </summary>
public record ElasticConstitutiveParameters : ConstitutiveParameters
{
    /// <summary>
    /// Represents the longitudinal stiffness (Young's Modulus).
    /// Unit: MPa.
    /// </summary>
    public double YoungModulus { get; init; }
}
