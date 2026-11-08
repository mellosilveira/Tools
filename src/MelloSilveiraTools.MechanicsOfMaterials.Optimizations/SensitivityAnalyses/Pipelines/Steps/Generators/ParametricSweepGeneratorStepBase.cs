using MelloSilveiraTools.Mathematics.Expressions;
using MelloSilveiraTools.MechanicsOfMaterials.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExtensionMethods;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.Range;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Pipelines.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Pipelines.Steps.Generators;

/// <summary>
/// Base class for parametric sweep generators, providing common envelope creation and boundary condition iteration as a pipeline step.
/// </summary>
/// <typeparam name="TConstitutiveParameters">The strictly typed constitutive parameters.</typeparam>
public abstract class ParametricSweepGeneratorStepBase<TConstitutiveParameters> : IParametricSweepGeneratorStep
    where TConstitutiveParameters : ConstitutiveParameters
{
    private const string StrainVariableName = "Strain";

    /// <inheritdoc />
    public string Name => GetType().Name;

    /// <inheritdoc />
    public IEnumerable<(string VariableName, RangeParameters VariableRange, IEnumerable<ParametricSweepInput> Inputs)> Execute(ParametricSweepPipelineInput input, CancellationToken cancellationToken = default)
    {
        TConstitutiveParameters baseline = (TConstitutiveParameters)input.Baseline;

        // 1. OAAT for Boundary Condition (Strain)
        if (input.StrainRange is not null)
        {
            yield return (StrainVariableName, input.StrainRange, CreateInputs(StrainVariableName, input.StrainRange, _ => baseline, input));
        }

        // 2. OAAT for Constitutive Parameters
        if (input.ConstantVariables is not null)
        {
            foreach ((string variableName, Func<TConstitutiveParameters, double, TConstitutiveParameters> mutate) in GetVariableVariations())
            {
                if (input.ConstantVariables.TryGetValue(variableName, out RangeParameters? range))
                    yield return (variableName, range, CreateInputs(variableName, range, value => mutate(baseline, value), input));
            }
        }

        // 3. OAAT for Array Constitutive Parameters
        if (input.ArrayVariables is not null)
        {
            foreach ((string variableName, Func<TConstitutiveParameters, int, double, TConstitutiveParameters> mutate) in GetArrayVariableVariations())
            {
                if (input.ArrayVariables.TryGetValue(variableName, out Dictionary<int, RangeParameters>? indexDict) && indexDict is not null)
                {
                    foreach ((int index, RangeParameters range) in indexDict)
                    {
                        string variableKey = $"{variableName}[{index}]";
                        yield return (variableKey, range, CreateInputs(variableKey, range, value => mutate(baseline, index, value), input));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Generates variable-specific OAAT variations, implemented by derived strongly-typed generators.
    /// </summary>
    protected abstract IEnumerable<(string VariableName, Func<TConstitutiveParameters, double, TConstitutiveParameters> Mutate)> GetVariableVariations();

    /// <summary>
    /// Generates array variable-specific OAAT variations, optionally implemented by derived generators.
    /// </summary>
    protected abstract IEnumerable<(string VariableName, Func<TConstitutiveParameters, int, double, TConstitutiveParameters> Mutate)> GetArrayVariableVariations();

    /// <summary>
    /// Creates a standardized envelope for the TPL Dataflow pipeline.
    /// </summary>
    protected IEnumerable<ParametricSweepInput> CreateInputs(string variableName, RangeParameters variableRange, Func<double, TConstitutiveParameters> mutate, ParametricSweepPipelineInput input)
    {
        foreach (double value in variableRange.GenerateValues())
        {
            MechanicalModelInput baseInput = new()
            {
                MechanicalModelName = input.MechanicalModelName,
                RampTimeConsideration = input.RampTimeConsideration,
                RampTime = input.RampTime,
                TimeStep = input.TimeStep,
                AcceptedStrainRange = AcceptedRange.Default,
                Strain = new MechanicalParameter(variableName == StrainVariableName ? value : input.BaseStrain),
                // TODO: In this current version, the parametric sweep analysis only works for stress-strain and relaxation.
                LoadResponseRelationship = LoadResponseRelationship.StressStrain,
                ViscoelasticEffect = ViscoelasticEffect.Relaxation,
            };

            GenericMechanicalModelInput simulationInput = new(baseInput, mutate(value));
            yield return new ParametricSweepInput(variableName, value, simulationInput);
        }
    }

    /// <summary>
    /// Produces a mutated <see cref="PronySeries"/> instance with the coefficient at the specified index replaced.
    /// </summary>
    /// <param name="baseline">The baseline Prony series.</param>
    /// <param name="index">Zero-based index across the combined independent and iterator coefficients array.</param>
    /// <param name="value">The swept coefficient value.</param>
    /// <returns>A new <see cref="PronySeries"/> instance reflecting the mutated coefficient.</returns>
    protected static PronySeries GetPronySeriesVariation(PronySeries baseline, int index, double value)
    {
        double[] coefficients = [baseline.IndependentParameter, .. baseline.IteratorCoefficients];
        coefficients[index] = value;
        return new PronySeries(coefficients[0], coefficients[1..], baseline.InitialVariableValue, baseline.FinalVariableValue);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
