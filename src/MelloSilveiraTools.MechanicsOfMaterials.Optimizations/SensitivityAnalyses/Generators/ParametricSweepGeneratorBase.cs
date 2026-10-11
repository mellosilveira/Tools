using MelloSilveiraTools.MechanicsOfMaterials.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExtensionMethods;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Generators;

/// <summary>
/// Base class for parametric sweep generators, providing common envelope creation and boundary condition iteration.
/// </summary>
/// <typeparam name="TConstitutiveParameters">The strictly typed constitutive parameters.</typeparam>
public abstract class ParametricSweepGeneratorBase<TConstitutiveParameters> : IParametricSweepGenerator
    where TConstitutiveParameters : ConstitutiveParameters
{
    private const string StrainVariableName = "Strain";

    /// <inheritdoc />
    public IEnumerable<SweepSimulationEnvelope> GenerateCombinations(ParametricSweepInput input)
    {
        TConstitutiveParameters baseline = (TConstitutiveParameters)input.Baseline;

        // 1. OAAT for Boundary Condition (Strain)
        foreach (double strain in input.StrainRange.GenerateValues())
        {
            yield return CreateEnvelope(StrainVariableName, strain, baseline, input);
        }

        // 2. OAAT for Constitutive Parameters
        foreach (SweepSimulationEnvelope envelope in GeneratePropertyVariations(input, baseline))
        {
            yield return envelope;
        }
    }

    /// <summary>
    /// Generates property-specific OAAT variations, implemented by derived strongly-typed generators.
    /// </summary>
    protected abstract IEnumerable<SweepSimulationEnvelope> GeneratePropertyVariations(ParametricSweepInput input, TConstitutiveParameters baseline);

    /// <summary>
    /// Creates a standardized envelope for the TPL Dataflow pipeline.
    /// </summary>
    protected SweepSimulationEnvelope CreateEnvelope(string variableName, double variableValue, TConstitutiveParameters constitutiveParameters, ParametricSweepInput input)
    {
        MechanicalModelInput baseInput = new()
        {
            MechanicalModelName = input.MechanicalModelName,
            RampTimeConsideration = input.RampTimeConsideration,
            RampTime = input.RampTime,
            TimeStep = input.TimeStep,
            AcceptedStrainRange = AcceptedRange.Default,
            Strain = new MechanicalParameter(variableName == StrainVariableName ? variableValue : input.BaseStrain),
            Stress = new MechanicalParameter(0),
            // TODO: In this current version, the parametric sweep analysis only works for stress-strain and relaxation.
            LoadResponseRelationship = LoadResponseRelationship.StressStrain,
            ViscoelasticEffect = ViscoelasticEffect.Relaxation,
        };

        GenericMechanicalModelInput simulationInput = new(baseInput, constitutiveParameters);
        return new SweepSimulationEnvelope(Guid.NewGuid(), variableName, variableValue, simulationInput);
    }
}
