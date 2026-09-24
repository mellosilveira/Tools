using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.Fung;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.TypeResolvers;

/// <inheritdoc cref="IMechanicalModelTypeResolver"/>
public class FungModelTypeResolver : IMechanicalModelTypeResolver
{
    /// <inheritdoc/>
    public string MechanicalModelName => nameof(MechanicalModel.Fung);

    /// <inheritdoc/>
    public Type ConstitutiveParameters => typeof(FungConstitutiveParameters);

    /// <inheritdoc/>
    public Type Output => typeof(QuasiLinearModelOutput);

    /// <inheritdoc/>
    public Type Calculator => typeof(IFungModelCalculator);
}
