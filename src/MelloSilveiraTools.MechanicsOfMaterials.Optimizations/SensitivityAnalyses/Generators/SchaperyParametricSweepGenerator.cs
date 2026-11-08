using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.Schapery;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Generators;

/// <summary>
/// Strongly typed OAT (One-at-a-Time) generator for Schapery non-linear viscoelastic constitutive parameters.
/// </summary>
public sealed class SchaperyParametricSweepGenerator : ParametricSweepGeneratorBase<SchaperyConstitutiveParameters>
{
    /// <inheritdoc />
    protected override IEnumerable<(string VariableName, Func<SchaperyConstitutiveParameters, double, SchaperyConstitutiveParameters> Mutate)> GetVariableVariations()
    {
        yield return (nameof(SchaperyConstitutiveParameters.G0), (baseline, value) => baseline with { G0 = value });
        yield return (nameof(SchaperyConstitutiveParameters.G1), (baseline, value) => baseline with { G1 = value });
        yield return (nameof(SchaperyConstitutiveParameters.G2), (baseline, value) => baseline with { G2 = value });
        yield return (nameof(SchaperyConstitutiveParameters.J0), (baseline, value) => baseline with { J0 = value });
        yield return (nameof(SchaperyConstitutiveParameters.Ge), (baseline, value) => baseline with { Ge = value });
        yield return (nameof(SchaperyConstitutiveParameters.He), (baseline, value) => baseline with { He = new ConstantFunction(value) });
        yield return (nameof(SchaperyConstitutiveParameters.H1), (baseline, value) => baseline with { H1 = new ConstantFunction(value) });
        yield return (nameof(SchaperyConstitutiveParameters.H2), (baseline, value) => baseline with { H2 = new ConstantFunction(value) });
        yield return ($"{nameof(SchaperyConstitutiveParameters.TransientRelaxationFunction)}.C", static (baseline, value) => GetTransientRelaxationFunction(baseline, [value, baseline.TransientRelaxationFunction!.Coefficients[1]]));
        yield return ($"{nameof(SchaperyConstitutiveParameters.TransientRelaxationFunction)}.n", static (baseline, value) => GetTransientRelaxationFunction(baseline, [baseline.TransientRelaxationFunction!.Coefficients[0], value]));

        static SchaperyConstitutiveParameters GetTransientRelaxationFunction(SchaperyConstitutiveParameters baseline, double[] coefficients)
        {
            PowerLaw transientRelaxationFunction = baseline.TransientRelaxationFunction!;
            return baseline with
            {
                TransientRelaxationFunction = new PowerLaw(coefficients, transientRelaxationFunction.InitialVariableValue, transientRelaxationFunction.FinalVariableValue)
            };
        }
    }

    /// <inheritdoc/>
    protected override IEnumerable<(string VariableName, Func<SchaperyConstitutiveParameters, int, double, SchaperyConstitutiveParameters> Mutate)> GetArrayVariableVariations()
    {
        yield return (
            nameof(SchaperyConstitutiveParameters.TransientCreepCompliance),
            static (baseline, index, value) => baseline with { TransientCreepCompliance = GetPronySeriesVariation(baseline.TransientCreepCompliance!, index, value) }
        );
    }
}
