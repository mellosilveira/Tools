using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.Linear.Maxwell;

/// <summary>
/// Contains the input data for Maxwell's model.
/// </summary>
public sealed record MaxwellModelInput : MechanicalModelInput
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
