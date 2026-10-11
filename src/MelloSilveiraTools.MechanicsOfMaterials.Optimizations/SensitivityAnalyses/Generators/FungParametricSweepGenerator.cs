using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExtensionMethods;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.Range;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Generators;

/// <summary>
/// Strongly typed OAAT generator for Fung Quasi-Linear Viscoelastic constitutive parameters.
/// </summary>
public sealed class FungParametricSweepGenerator : ParametricSweepGeneratorBase<FungConstitutiveParameters>
{
    /// <inheritdoc />
    protected override IEnumerable<SweepSimulationEnvelope> GeneratePropertyVariations(ParametricSweepInput input, FungConstitutiveParameters baseline)
    {
        string elasticStressConstantName = nameof(baseline.ElasticStressConstant);
        if (input.ConstantVariables.TryGetValue(elasticStressConstantName, out RangeParameters? stressRange))
        {
            foreach (double value in stressRange.GenerateValues())
            {
                FungConstitutiveParameters mutated = baseline with { ElasticStressConstant = value };
                yield return CreateEnvelope(elasticStressConstantName, value, mutated, input);
            }
        }

        string elasticPowerConstantName = nameof(baseline.ElasticPowerConstant);
        if (input.ConstantVariables.TryGetValue(elasticPowerConstantName, out RangeParameters? powerRange))
        {
            foreach (double value in powerRange.GenerateValues())
            {
                FungConstitutiveParameters mutated = baseline with { ElasticPowerConstant = value };
                yield return CreateEnvelope(elasticPowerConstantName, value, mutated, input);
            }
        }

        string relaxationStiffnessName = nameof(baseline.ReducedRelaxationFunction.RelaxationStiffness);
        if (input.ConstantVariables.TryGetValue(relaxationStiffnessName, out RangeParameters? stiffRange))
        {
            foreach (double value in stiffRange.GenerateValues())
            {
                FungConstitutiveParameters mutated = baseline with { ReducedRelaxationFunction = baseline.ReducedRelaxationFunction! with { RelaxationStiffness = value } };
                yield return CreateEnvelope(relaxationStiffnessName, value, mutated, input);
            }
        }

        string fastRelaxationTimeName = nameof(baseline.ReducedRelaxationFunction.FastRelaxationTime);
        if (input.ConstantVariables.TryGetValue(fastRelaxationTimeName, out RangeParameters? fastRange))
        {
            foreach (double value in fastRange.GenerateValues())
            {
                FungConstitutiveParameters mutated = baseline with { ReducedRelaxationFunction = baseline.ReducedRelaxationFunction! with { FastRelaxationTime = value } };
                yield return CreateEnvelope(fastRelaxationTimeName, value, mutated, input);
            }
        }

        string slowRelaxationTimeName = nameof(baseline.ReducedRelaxationFunction.SlowRelaxationTime);
        if (input.ConstantVariables.TryGetValue(slowRelaxationTimeName, out RangeParameters? slowRange))
        {
            foreach (double value in slowRange.GenerateValues())
            {
                FungConstitutiveParameters mutated = baseline with { ReducedRelaxationFunction = baseline.ReducedRelaxationFunction! with { SlowRelaxationTime = value } };
                yield return CreateEnvelope(slowRelaxationTimeName, value, mutated, input);
            }
        }
    }
}
