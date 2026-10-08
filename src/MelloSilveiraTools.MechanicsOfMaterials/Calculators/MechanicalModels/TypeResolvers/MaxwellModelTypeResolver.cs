using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.Linear.Maxwell;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.Linear.Maxwell;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.TypeResolvers;

/// <inheritdoc cref="IMechanicalModelTypeResolver"/>
public class MaxwellModelTypeResolver : IMechanicalModelTypeResolver
{
    /// <inheritdoc/>
    public string MechanicalModelName => nameof(MechanicalModel.Maxwell);

    /// <inheritdoc/>
    public Type ConstitutiveParameters => typeof(MaxwellConstitutiveParameters);

    /// <inheritdoc/>
    public Type Output => typeof(MaxwellModelOutput);

    /// <inheritdoc/>
    public Type Calculator => typeof(IMaxwellModelCalculator);
}
