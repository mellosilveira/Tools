using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.NonLinear.Schapery;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.Schapery;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.TypeResolvers;

/// <inheritdoc cref="IMechanicalModelTypeResolver"/>
public class SchaperyModelTypeResolver : IMechanicalModelTypeResolver
{
    /// <inheritdoc/>
    public string MechanicalModelName => nameof(MechanicalModel.Schapery);

    /// <inheritdoc/>
    public Type ConstitutiveParameters => typeof(SchaperyConstitutiveParameters);

    /// <inheritdoc/>
    public Type Output => typeof(SchaperyModelOutput);

    /// <inheritdoc/>
    public Type Calculator => typeof(ISchaperyModelCalculator);
}
