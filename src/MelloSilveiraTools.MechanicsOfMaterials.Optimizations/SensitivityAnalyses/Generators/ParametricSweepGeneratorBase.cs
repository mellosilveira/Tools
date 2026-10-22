using MelloSilveiraTools.MechanicsOfMaterials.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExtensionMethods;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.Range;
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
        if (input.StrainRange is not null)
        {
            foreach (double strain in input.StrainRange.GenerateValues())
            {
                yield return CreateEnvelope(StrainVariableName, strain, baseline, input);
            }
        }

        // 2. OAAT for Constitutive Parameters
        if (input.ConstantVariables is not null)
        {
            foreach ((string propertyName, Func<TConstitutiveParameters, double, TConstitutiveParameters> mutate) in GetPropertyVariations(input))
            {
                if (input.ConstantVariables.TryGetValue(propertyName, out RangeParameters? range))
                {
                    foreach (double value in range.GenerateValues())
                    {
                        yield return CreateEnvelope(propertyName, value, mutate(baseline, value), input);
                    }
                }
            }
        }

        // 3. OAAT for Array Constitutive Parameters
        if (input.ArrayVariables is not null)
        {
            foreach ((string propertyName, int index, Func<TConstitutiveParameters, double, TConstitutiveParameters> mutate) in GetArrayPropertyVariations(input))
            {
                if (input.ArrayVariables.TryGetValue(propertyName, out Dictionary<int, RangeParameters>? indexDict) &&
                    indexDict.TryGetValue(index, out RangeParameters? range))
                {
                    string variableKey = $"{propertyName}[{index}]";
                    foreach (double value in range.GenerateValues())
                    {
                        yield return CreateEnvelope(variableKey, value, mutate(baseline, value), input);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Generates property-specific OAAT variations, implemented by derived strongly-typed generators.
    /// </summary>
    protected abstract IEnumerable<(string PropertyName, Func<TConstitutiveParameters, double, TConstitutiveParameters> Mutate)> GetPropertyVariations(ParametricSweepInput input);

    /// <summary>
    /// Generates array property-specific OAAT variations, optionally implemented by derived generators.
    /// </summary>
    protected virtual IEnumerable<(string PropertyName, int Index, Func<TConstitutiveParameters, double, TConstitutiveParameters> Mutate)> GetArrayPropertyVariations(ParametricSweepInput input) => [];

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
            // TODO: In this current version, the parametric sweep analysis only works for stress-strain and relaxation.
            LoadResponseRelationship = LoadResponseRelationship.StressStrain,
            ViscoelasticEffect = ViscoelasticEffect.Relaxation,
        };

        GenericMechanicalModelInput simulationInput = new(baseInput, constitutiveParameters);
        return new SweepSimulationEnvelope(variableName, variableValue, simulationInput);
    }
}
