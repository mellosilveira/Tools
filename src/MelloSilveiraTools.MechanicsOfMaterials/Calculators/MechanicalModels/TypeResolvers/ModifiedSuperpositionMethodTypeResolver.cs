using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.NonLinear.ModifiedSuperpositionMethod;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.ModifiedSuperpositionMethod;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.TypeResolvers;

/// <inheritdoc cref="IMechanicalModelTypeResolver"/>
public class ModifiedSuperpositionMethodTypeResolver : IMechanicalModelTypeResolver
{
    /// <inheritdoc/>
    public string MechanicalModelName => nameof(MechanicalModel.ModifiedSuperpositionMethod);

    /// <inheritdoc/>
    public Type ConstitutiveParameters => typeof(ModifiedSuperpositionMethodConstitutiveParameters);

    /// <inheritdoc/>
    public Type Output => typeof(ModifiedSuperpositionMethodOutput);

    /// <inheritdoc/>
    public Type Calculator => typeof(IModifiedSuperpositionMethodCalculator);
}
