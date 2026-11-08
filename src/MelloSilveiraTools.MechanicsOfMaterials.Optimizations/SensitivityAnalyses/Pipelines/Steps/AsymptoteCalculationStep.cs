using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.Mathematics.NumericalMethods.Differentiations;
using MelloSilveiraTools.Mathematics.NumericalMethods.RootFindingAlgorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Pipelines.Steps;

/// <summary>
/// A domain-specific calculation step that evaluates the continuum stress asymptote by tracking the stress relaxation rate (time derivative dσ/dt) 
/// until it approaches zero, utilizing an <see cref="IRootFinding"/> engine to locate the convergence point.
/// </summary>
/// <param name="calculator">Mechanical model calculator facade for stress calculations.</param>
/// <param name="differentiation">Numerical differentiation engine.</param>
/// <param name="rootFinding">Root-finding algorithm engine.</param>
public sealed class AsymptoteCalculationStep(
    IMechanicalModelCalculatorFacade calculator,
    IDifferentiation differentiation,
    IRootFinding rootFinding,
    RootFindingInput baseRootFindingInput)
    : ISyncPipelineStep<GenericMechanicalModelInput, (double AsymptoteTime, double Error, double Stress)>
{
    /// <inheritdoc />
    public string Name => nameof(AsymptoteCalculationStep);

    /// <inheritdoc />
    public (double AsymptoteTime, double Error, double Stress) Execute(GenericMechanicalModelInput input)
    {
        RootFindingInput rootFindingInput = input.RampTime is not null ? baseRootFindingInput with { InitialPoint = input.RampTime.Value } : baseRootFindingInput;
        (double asymptoteTime, double error) = rootFinding.FindRoot(rootFindingInput, time =>
        {
            double derivative = differentiation.Calculate(t => calculator.CalculateStress(input, t), input.TimeStep, time);
            return Math.Abs(derivative);
        });

        return (asymptoteTime, error, calculator.CalculateStress(input, asymptoteTime));
    }

    /// <inheritdoc />
    public void Dispose() => GC.SuppressFinalize(this);
}
