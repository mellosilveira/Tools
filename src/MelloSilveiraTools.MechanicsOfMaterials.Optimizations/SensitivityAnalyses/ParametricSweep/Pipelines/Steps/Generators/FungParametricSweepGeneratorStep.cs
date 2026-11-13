using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Pipelines.Steps.Generators;

/// <summary>
/// Strongly typed OAT (One-at-a-Time) generator pipeline step for Fung Quasi-Linear Viscoelastic constitutive parameters.
/// </summary>
public sealed class FungParametricSweepGeneratorStep : ParametricSweepGeneratorStepBase<FungConstitutiveParameters>
{
    /// <inheritdoc />
    protected override IEnumerable<(string VariableName, Func<FungConstitutiveParameters, double, FungConstitutiveParameters> Mutate)> GetVariableVariations()
    {
        yield return (nameof(FungConstitutiveParameters.ElasticStressConstant), (baseline, value) => baseline with { ElasticStressConstant = value });
        yield return (nameof(FungConstitutiveParameters.ElasticPowerConstant), (baseline, value) => baseline with { ElasticPowerConstant = value });
        yield return (nameof(FungConstitutiveParameters.ReducedRelaxationFunction.RelaxationStiffness), (baseline, value) => baseline with { ReducedRelaxationFunction = baseline.ReducedRelaxationFunction! with { RelaxationStiffness = value } });
        yield return (nameof(FungConstitutiveParameters.ReducedRelaxationFunction.FastRelaxationTime), (baseline, value) => baseline with { ReducedRelaxationFunction = baseline.ReducedRelaxationFunction! with { FastRelaxationTime = value } });
        yield return (nameof(FungConstitutiveParameters.ReducedRelaxationFunction.SlowRelaxationTime), (baseline, value) => baseline with { ReducedRelaxationFunction = baseline.ReducedRelaxationFunction! with { SlowRelaxationTime = value } });
    }

    /// <inheritdoc />
    protected override IEnumerable<(string VariableName, Func<FungConstitutiveParameters, int, double, FungConstitutiveParameters> Mutate)> GetArrayVariableVariations() => [];
}
