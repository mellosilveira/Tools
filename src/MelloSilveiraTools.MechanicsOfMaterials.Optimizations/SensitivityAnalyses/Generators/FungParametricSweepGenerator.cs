using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Generators;

/// <summary>
/// Strongly typed OAT (One-at-a-Time) generator for Fung Quasi-Linear Viscoelastic constitutive parameters.
/// </summary>
public sealed class FungParametricSweepGenerator : ParametricSweepGeneratorBase<FungConstitutiveParameters>
{
    // TODO: VOLTAR A COMO ERA PARA TER APENAS UMA STRING DE NOME DE PROPRIEDADE
    
    /// <inheritdoc />
    protected override IEnumerable<(string[] PropertyAliases, Func<FungConstitutiveParameters, double, FungConstitutiveParameters> Mutate)> GetPropertyVariations(ParametricSweepInput input)
    {
        yield return ([nameof(FungConstitutiveParameters.ElasticStressConstant), "A"], (baseline, value) => baseline with { ElasticStressConstant = value });
        yield return ([nameof(FungConstitutiveParameters.ElasticPowerConstant), "B"], (baseline, value) => baseline with { ElasticPowerConstant = value });
        yield return ([nameof(FungConstitutiveParameters.ReducedRelaxationFunction.RelaxationStiffness), "c", "C"], (baseline, value) => baseline with { ReducedRelaxationFunction = baseline.ReducedRelaxationFunction! with { RelaxationStiffness = value } });
        yield return ([nameof(FungConstitutiveParameters.ReducedRelaxationFunction.FastRelaxationTime), "tau1", "Tau1", "t1"], (baseline, value) => baseline with { ReducedRelaxationFunction = baseline.ReducedRelaxationFunction! with { FastRelaxationTime = value } });
        yield return ([nameof(FungConstitutiveParameters.ReducedRelaxationFunction.SlowRelaxationTime), "tau2", "Tau2", "t2"], (baseline, value) => baseline with { ReducedRelaxationFunction = baseline.ReducedRelaxationFunction! with { SlowRelaxationTime = value } });
    }
}
