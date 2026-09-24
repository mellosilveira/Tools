using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.SimplifiedFung;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.TypeResolvers;

/// <inheritdoc cref="IMechanicalModelTypeResolver"/>
public class SimplifiedFungModelTypeResolver : IMechanicalModelTypeResolver
{
    /// <inheritdoc/>
    public string MechanicalModelName => nameof(MechanicalModel.SimplifiedFung);

    /// <inheritdoc/>
    public Type ConstitutiveParameters => typeof(SimplifiedFungConstitutiveParameters);

    /// <inheritdoc/>
    public Type Output => typeof(QuasiLinearModelOutput);

    /// <inheritdoc/>
    public Type Calculator => typeof(ISimplifiedFungModelCalculator);
}
