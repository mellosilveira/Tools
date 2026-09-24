using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Elasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Elasticity;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.TypeResolvers;

/// <inheritdoc cref="IMechanicalModelTypeResolver"/>
public class ElasticModelTypeResolver : IMechanicalModelTypeResolver
{
    /// <inheritdoc/>
    public string MechanicalModelName => nameof(MechanicalModel.Elastic);

    /// <inheritdoc/>
    public Type ConstitutiveParameters => typeof(ElasticConstitutiveParameters);

    /// <inheritdoc/>
    public Type Output => typeof(ElasticModelOutput);

    /// <inheritdoc/>
    public Type Calculator => typeof(IElasticModelCalculator);
}
