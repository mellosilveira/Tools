using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.TypeResolvers;

/// <summary>
/// Type resolver for mechanical models.
/// </summary>
public interface IMechanicalModelTypeResolver
{
    /// <summary>
    /// Gets the name of the mechanical model.
    /// </summary>
    string MechanicalModelName { get; }

    /// <summary>
    /// Gets the type of constitutive parameters for the mechanical model.
    /// This type must inherit from <see cref="ConstitutiveParameters"/>.
    /// </summary>
    Type ConstitutiveParameters { get; }

    /// <summary>
    /// Gets the type of projection output for the mechanical model.
    /// This type must inherit from <see cref="MechanicalModelOutput"/>.
    /// </summary>
    Type Output { get; }

    /// <summary>
    /// Gets the type of calculator for the mechanical model.
    /// </summary>
    Type Calculator { get; }
}
