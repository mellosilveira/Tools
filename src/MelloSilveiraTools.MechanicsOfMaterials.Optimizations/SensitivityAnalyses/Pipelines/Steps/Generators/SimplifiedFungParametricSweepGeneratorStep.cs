using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Pipelines.Steps.Generators;

/// <summary>
/// Strongly typed OAT (One-at-a-Time) generator pipeline step for Simplified Fung Quasi-Linear Viscoelastic constitutive parameters.
/// </summary>
public sealed class SimplifiedFungParametricSweepGeneratorStep : ParametricSweepGeneratorStepBase<SimplifiedFungConstitutiveParameters>
{
    /// <inheritdoc />
    protected override IEnumerable<(string VariableName, Func<SimplifiedFungConstitutiveParameters, double, SimplifiedFungConstitutiveParameters> Mutate)> GetVariableVariations()
    {
        yield return (nameof(SimplifiedFungConstitutiveParameters.ElasticStressConstant), (baseline, value) => baseline with { ElasticStressConstant = value });
        yield return (nameof(SimplifiedFungConstitutiveParameters.ElasticPowerConstant), (baseline, value) => baseline with { ElasticPowerConstant = value });
    }

    /// <inheritdoc />
    protected override IEnumerable<(string VariableName, Func<SimplifiedFungConstitutiveParameters, int, double, SimplifiedFungConstitutiveParameters> Mutate)> GetArrayVariableVariations()
    {
        yield return (
            nameof(SimplifiedFungConstitutiveParameters.ReducedRelaxationFunction),
            static (baseline, index, value) => baseline with { ReducedRelaxationFunction = GetPronySeriesVariation(baseline.ReducedRelaxationFunction!, index, value) }
        );
    }
}
