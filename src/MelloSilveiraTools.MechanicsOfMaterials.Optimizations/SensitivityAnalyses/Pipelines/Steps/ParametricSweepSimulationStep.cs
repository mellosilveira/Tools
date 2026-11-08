using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.Mathematics.Extensions;
using MelloSilveiraTools.Mathematics.Models.NumericalMethods;
using MelloSilveiraTools.Mathematics.NumericalMethods.Differentiations;
using MelloSilveiraTools.Mathematics.NumericalMethods.RootFindingAlgorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.Range;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Pipelines.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Pipelines.Steps;

/// <summary>
/// Pipeline step that executes the forward numerical simulations across all parameter variations of a swept variable,
/// detects relaxation asymptotes, and computes stress deltas.
/// </summary>
/// <param name="calculator">Mechanical model calculator facade for stress calculations.</param>
/// <param name="differentiation">Numerical differentiation engine for asymptote detection.</param>
/// <param name="rootFinding">Root-finding algorithm engine for locating steady-state convergence.</param>
/// <param name="baseRootFindingInput">Base root-finding configuration parameters.</param>
/// <param name="initialTime">Initial simulation evaluation time ($t_0$).</param>
/// <param name="finalTime">Final simulation evaluation time ($t_f$).</param>
public sealed class ParametricSweepSimulationStep(
    IMechanicalModelCalculatorFacade calculator,
    IDifferentiation differentiation,
    IRootFinding rootFinding,
    RootFindingInput baseRootFindingInput,
    double initialTime,
    double finalTime)
    : IAsyncPipelineStep<(string VariableName, RangeParameters VariableRange, IEnumerable<ParametricSweepInput> Inputs), (string VariableName, RangeParameters VariableRange, IReadOnlyList<ParametricSweepOutput> Outputs)>
{
    /// <inheritdoc />
    public string Name => nameof(ParametricSweepSimulationStep);

    /// <inheritdoc />
    public Task<(string VariableName, RangeParameters VariableRange, IReadOnlyList<ParametricSweepOutput> Outputs)> ExecuteAsync(
        (string VariableName, RangeParameters VariableRange, IEnumerable<ParametricSweepInput> Inputs) input,
        CancellationToken cancellationToken = default)
    {
        List<ParametricSweepOutput> outputs = [];
        foreach (ParametricSweepInput sweepInput in input.Inputs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            double initialStress = calculator.CalculateStress(sweepInput.SimulationInput, initialTime);
            double finalStress = calculator.CalculateStress(sweepInput.SimulationInput, finalTime);

            RootFindingInput rootFindingInput = sweepInput.SimulationInput.RampTime is not null
                ? baseRootFindingInput with { InitialPoint = sweepInput.SimulationInput.RampTime.Value }
                : baseRootFindingInput;

            (double asymptoteTime, double error) = rootFinding.FindRoot(rootFindingInput, time =>
            {
                double derivative = differentiation.Calculate(t => calculator.CalculateStress(sweepInput.SimulationInput, t), sweepInput.SimulationInput.TimeStep, time);
                return Math.Abs(derivative);
            });

            double asymptoteStress = calculator.CalculateStress(sweepInput.SimulationInput, asymptoteTime);

            outputs.Add(new ParametricSweepOutput(
                sweepInput.VariableName,
                sweepInput.VariableValue,
                initialStress,
                finalStress,
                asymptoteTime,
                error,
                asymptoteStress,
                finalStress - initialStress,
                initialStress.PercentageDifference(finalStress)));
        }

        return Task.FromResult<(string VariableName, RangeParameters VariableRange, IReadOnlyList<ParametricSweepOutput> Outputs)>(
            (input.VariableName, input.VariableRange, outputs));
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
