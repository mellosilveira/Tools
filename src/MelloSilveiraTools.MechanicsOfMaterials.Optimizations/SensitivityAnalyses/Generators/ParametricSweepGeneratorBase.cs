using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Models;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Generators;

/// <summary>
/// Base class for parametric sweep generators, providing common envelope creation and boundary condition iteration.
/// </summary>
/// <typeparam name="TConstitutiveParameters">The strictly typed constitutive parameters.</typeparam>
public abstract class ParametricSweepGeneratorBase<TConstitutiveParameters> : IParametricSweepGenerator
    where TConstitutiveParameters : ConstitutiveParameters
{
    /// <inheritdoc />
    public IEnumerable<SweepSimulationEnvelope> GenerateCombinations(ParametricSweepInput input)
    {
        TConstitutiveParameters baseline = (TConstitutiveParameters)input.Baseline;
        double baseStrain = input.StrainRange.InitialPoint;

        // 1. OAAT for Boundary Condition (Strain)
        foreach (double strain in input.StrainRange.GenerateValues())
        {
            yield return CreateEnvelope("Strain", strain, baseline, strain, input);
        }

        // 2. OAAT for Constitutive Parameters
        foreach (SweepSimulationEnvelope envelope in GeneratePropertyVariations(input, baseline, baseStrain))
        {
            yield return envelope;
        }
    }

    /// <summary>
    /// Generates property-specific OAAT variations, implemented by derived strongly-typed generators.
    /// </summary>
    protected abstract IEnumerable<SweepSimulationEnvelope> GeneratePropertyVariations(
        ParametricSweepInput input,
        TConstitutiveParameters baseline,
        double baseStrain);

    /// <summary>
    /// Creates a standardized envelope for the TPL Dataflow pipeline.
    /// </summary>
    protected SweepSimulationEnvelope CreateEnvelope(
        string variableName,
        double variableValue,
        TConstitutiveParameters mutatedParameters,
        double appliedStrain,
        ParametricSweepInput input)
    {
        MechanicalModelInput baseInput = new()
        {
            MechanicalModelName = input.MechanicalModelName,
            RampTimeConsideration = input.RampTimeConsideration,
            RampTime = input.RampTime,
            TimeStep = input.TimeStep,
            Strain = new MechanicalParameter(appliedStrain),
            AcceptedStrainRange = AcceptedRange.Default
        };

        GenericMechanicalModelInput simulationInput = new(baseInput, mutatedParameters);

        return new SweepSimulationEnvelope(
            Guid.NewGuid(),
            variableName,
            variableValue,
            simulationInput);
    }
}
