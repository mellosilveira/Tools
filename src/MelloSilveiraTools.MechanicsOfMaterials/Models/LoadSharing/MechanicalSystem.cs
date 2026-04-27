using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

namespace MelloSilveiraTools.MechanicsOfMaterials.Models.LoadSharing;

/// <summary>
/// Represents the mechanical system and its soft tissues.
/// </summary>
public class MechanicalSystem
{
    /// <summary>
    /// Represents the force behavior over the time.
    /// </summary>
    public double InitialForce { get; init; }

    /// <summary>
    /// Represents the displacement behavior over the time.
    /// </summary>
    public MechanicalParameter Displacement { get; init; }

    /// <summary>
    /// Mechanical model input for specimens on mechanical system.
    /// </summary>
    public MechanicalModelInput[] SpecimenInputs { get; init; }
}
