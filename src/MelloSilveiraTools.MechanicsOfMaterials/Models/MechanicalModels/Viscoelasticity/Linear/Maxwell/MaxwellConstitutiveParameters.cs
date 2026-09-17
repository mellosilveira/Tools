namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.Linear.Maxwell;

/// <summary>
/// Defines the constitutive parameters for the Maxwell viscoelastic model.
/// </summary>
public sealed record MaxwellConstitutiveParameters : ConstitutiveParameters
{
    /// <summary>
    /// Unit: MPa.s (Mega-Pascal-second).
    /// </summary>
    public double Viscosity { get; init; }

    /// <summary>
    /// Unit: MPa (Mega-Pascal).
    /// </summary>
    public double Stiffness { get; init; }
}
