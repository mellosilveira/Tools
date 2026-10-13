using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Generators;

/// <summary>
/// Strongly typed OAT (One-at-a-Time) generator for Fung Quasi-Linear Viscoelastic constitutive parameters.
/// </summary>
public sealed class FungParametricSweepGenerator : ParametricSweepGeneratorBase<FungConstitutiveParameters>
{
    /// <inheritdoc />
    protected override IEnumerable<(string PropertyName, Func<FungConstitutiveParameters, double, FungConstitutiveParameters> Mutate)> GetPropertyVariations(ParametricSweepInput input)
    {
        yield return (nameof(FungConstitutiveParameters.ElasticStressConstant), (baseline, value) => baseline with { ElasticStressConstant = value });
        yield return (nameof(FungConstitutiveParameters.ElasticPowerConstant), (baseline, value) => baseline with { ElasticPowerConstant = value });
        yield return (nameof(FungConstitutiveParameters.ReducedRelaxationFunction.RelaxationStiffness), (baseline, value) => baseline with { ReducedRelaxationFunction = baseline.ReducedRelaxationFunction! with { RelaxationStiffness = value } });
        yield return (nameof(FungConstitutiveParameters.ReducedRelaxationFunction.FastRelaxationTime), (baseline, value) => baseline with { ReducedRelaxationFunction = baseline.ReducedRelaxationFunction! with { FastRelaxationTime = value } });
        yield return (nameof(FungConstitutiveParameters.ReducedRelaxationFunction.SlowRelaxationTime), (baseline, value) => baseline with { ReducedRelaxationFunction = baseline.ReducedRelaxationFunction! with { SlowRelaxationTime = value } });
    }
}
