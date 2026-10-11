using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.Range;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Models;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Generators;

/// <summary>
/// Strongly typed OAAT generator for Fung Quasi-Linear Viscoelastic constitutive parameters.
/// </summary>
public sealed class FungParametricSweepGenerator : ParametricSweepGeneratorBase<FungConstitutiveParameters>
{
    /// <inheritdoc />
    protected override IEnumerable<SweepSimulationEnvelope> GeneratePropertyVariations(
        ParametricSweepInput input,
        FungConstitutiveParameters baseline,
        double baseStrain)
    {
        // 1. ElasticStressConstant
        if (input.ConstantVariables.TryGetValue(nameof(baseline.ElasticStressConstant), out RangeParameters? stressRange))
        {
            foreach (double val in stressRange.GenerateValues())
            {
                FungConstitutiveParameters mutated = baseline with { ElasticStressConstant = val };
                yield return CreateEnvelope(nameof(baseline.ElasticStressConstant), val, mutated, baseStrain, input);
            }
        }

        // 2. ElasticPowerConstant
        if (input.ConstantVariables.TryGetValue(nameof(baseline.ElasticPowerConstant), out RangeParameters? powerRange))
        {
            foreach (double val in powerRange.GenerateValues())
            {
                FungConstitutiveParameters mutated = baseline with { ElasticPowerConstant = val };
                yield return CreateEnvelope(nameof(baseline.ElasticPowerConstant), val, mutated, baseStrain, input);
            }
        }

        // 3. ReducedRelaxationFunction parameters
        if (baseline.ReducedRelaxationFunction is not null)
        {
            // RelaxationStiffness
            if (input.ConstantVariables.TryGetValue(nameof(baseline.ReducedRelaxationFunction.RelaxationStiffness), out RangeParameters? stiffRange))
            {
                foreach (double val in stiffRange.GenerateValues())
                {
                    FungConstitutiveParameters mutated = baseline with 
                    { 
                        ReducedRelaxationFunction = baseline.ReducedRelaxationFunction with { RelaxationStiffness = val } 
                    };
                    yield return CreateEnvelope(nameof(baseline.ReducedRelaxationFunction.RelaxationStiffness), val, mutated, baseStrain, input);
                }
            }

            // FastRelaxationTime
            if (input.ConstantVariables.TryGetValue(nameof(baseline.ReducedRelaxationFunction.FastRelaxationTime), out RangeParameters? fastRange))
            {
                foreach (double val in fastRange.GenerateValues())
                {
                    FungConstitutiveParameters mutated = baseline with 
                    { 
                        ReducedRelaxationFunction = baseline.ReducedRelaxationFunction with { FastRelaxationTime = val } 
                    };
                    yield return CreateEnvelope(nameof(baseline.ReducedRelaxationFunction.FastRelaxationTime), val, mutated, baseStrain, input);
                }
            }

            // SlowRelaxationTime
            if (input.ConstantVariables.TryGetValue(nameof(baseline.ReducedRelaxationFunction.SlowRelaxationTime), out RangeParameters? slowRange))
            {
                foreach (double val in slowRange.GenerateValues())
                {
                    FungConstitutiveParameters mutated = baseline with 
                    { 
                        ReducedRelaxationFunction = baseline.ReducedRelaxationFunction with { SlowRelaxationTime = val } 
                    };
                    yield return CreateEnvelope(nameof(baseline.ReducedRelaxationFunction.SlowRelaxationTime), val, mutated, baseStrain, input);
                }
            }
        }
    }
}
