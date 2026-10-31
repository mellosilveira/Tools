using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.Mathematics.NumericalMethods.Differentiations;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.SensitivityAnalysis.Steps;

/// <summary>
/// A domain-specific calculation step that evaluates the continuum stress asymptote
/// by tracking the stress relaxation rate (time derivative dσ/dt) until it approaches zero.
/// </summary>
/// <param name="calculator">Mechanical model calculator facade for stress calculations.</param>
/// <param name="differentiation">Numerical differentiation engine.</param>
/// <param name="tolerance">User-informed relative tolerance threshold for asymptotic convergence.</param>
public sealed class AsymptoteCalculationStep(
    IMechanicalModelCalculatorFacade calculator,
    IDifferentiation differentiation,
    double tolerance,
    int maxBracketIterations = 64,
    int binarySearchIterations = 20) : ISyncPipelineStep<GenericMechanicalModelInput, (double AsymptoteTime, double Stress)>
{
    /// <inheritdoc />
    public string Name => nameof(AsymptoteCalculationStep);

    /// <inheritdoc />
    public (double AsymptoteTime, double Stress) Execute(GenericMechanicalModelInput input)
    {
        double initialTime = input.RampTime ?? MathematicConstants.InitialTime;
        double timeStep = input.TimeStep;

        // If initial relaxation rate is already within tolerance, the material has reached the asymptote
        double initialDerivative = Math.Abs(CalculateStressDerivative(input, initialTime, timeStep));
        if (initialDerivative <= tolerance)
            return (initialTime, calculator.CalculateStress(input, initialTime));

        // Exponential bracket search for the transition where |dσ/dt| <= tolerance
        double lowTime = initialTime;
        double highTime = initialTime + Math.Max(1.0, timeStep);
        int bracketIterations = 0;

        while (bracketIterations++ < maxBracketIterations && Math.Abs(CalculateStressDerivative(input, highTime, timeStep)) > tolerance)
        {
            lowTime = highTime;
            highTime *= 2.0;
        }

        // Binary search refinement to locate the exact transition time
        for (int i = 0; i < binarySearchIterations; i++)
        {
            double midTime = (lowTime + highTime) * 0.5;
            double midDerivative = Math.Abs(CalculateStressDerivative(input, midTime, timeStep));

            if (midDerivative <= tolerance)
                highTime = midTime;
            else
                lowTime = midTime;
        }

        return (highTime, calculator.CalculateStress(input, highTime));
    }

    private double CalculateStressDerivative(GenericMechanicalModelInput input, double time, double timeStep)
        => differentiation.Calculate(t => calculator.CalculateStress(input, t), timeStep, time);

    /// <inheritdoc />
    public void Dispose() => GC.SuppressFinalize(this);
}
