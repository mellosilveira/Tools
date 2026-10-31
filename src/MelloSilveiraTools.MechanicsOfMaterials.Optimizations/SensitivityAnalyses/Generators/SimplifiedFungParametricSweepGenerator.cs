using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Generators;

/// <summary>
/// Strongly typed OAT (One-at-a-Time) generator for Simplified Fung Quasi-Linear Viscoelastic constitutive parameters.
/// </summary>
public sealed class SimplifiedFungParametricSweepGenerator : ParametricSweepGeneratorBase<SimplifiedFungConstitutiveParameters>
{
    /// <inheritdoc />
    protected override IEnumerable<(string PropertyName, Func<SimplifiedFungConstitutiveParameters, double, SimplifiedFungConstitutiveParameters> Mutate)> GetPropertyVariations(ParametricSweepInput input)
    {
        yield return (nameof(SimplifiedFungConstitutiveParameters.ElasticStressConstant), (baseline, value) => baseline with { ElasticStressConstant = value });
        yield return (nameof(SimplifiedFungConstitutiveParameters.ElasticPowerConstant), (baseline, value) => baseline with { ElasticPowerConstant = value });
    }
}
