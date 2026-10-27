using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.Schapery;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Generators;

/// <summary>
/// Strongly typed OAT (One-at-a-Time) generator for Schapery non-linear viscoelastic constitutive parameters.
/// </summary>
public sealed class SchaperyParametricSweepGenerator : ParametricSweepGeneratorBase<SchaperyConstitutiveParameters>
{
    /// <inheritdoc />
    protected override IEnumerable<(string PropertyName, Func<SchaperyConstitutiveParameters, double, SchaperyConstitutiveParameters> Mutate)> GetPropertyVariations(ParametricSweepInput input)
    {
        yield return (nameof(SchaperyConstitutiveParameters.G0), (baseline, value) => baseline with { G0 = value });
        yield return (nameof(SchaperyConstitutiveParameters.G1), (baseline, value) => baseline with { G1 = value });
        yield return (nameof(SchaperyConstitutiveParameters.G2), (baseline, value) => baseline with { G2 = value });
        yield return (nameof(SchaperyConstitutiveParameters.J0), (baseline, value) => baseline with { J0 = value });
        yield return (nameof(SchaperyConstitutiveParameters.Ge), (baseline, value) => baseline with { Ge = value });

        yield return ("g1", (baseline, value) =>
        {
            if (baseline.TransientRelaxationFunction is null)
                return baseline;

            double[] newCoefficients = [value, baseline.TransientRelaxationFunction.Coefficients[1]];
            return baseline with
            {
                TransientRelaxationFunction = new PowerLaw(newCoefficients, baseline.TransientRelaxationFunction.InitialVariableValue, baseline.TransientRelaxationFunction.FinalVariableValue)
            };
        });

        yield return ("n", (baseline, value) =>
        {
            if (baseline.TransientRelaxationFunction is null)
                return baseline;

            double[] newCoefficients = [baseline.TransientRelaxationFunction.Coefficients[0], value];
            return baseline with
            {
                TransientRelaxationFunction = new PowerLaw(newCoefficients, baseline.TransientRelaxationFunction.InitialVariableValue, baseline.TransientRelaxationFunction.FinalVariableValue)
            };
        });
    }
}
